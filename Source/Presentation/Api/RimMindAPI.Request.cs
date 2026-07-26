using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Async;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Queue;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Runtime.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace RimMind.Presentation.Api
{
    public static partial class RimMindAPI
    {
        public static class Request
        {
            private static readonly RuntimeServiceRef<IAIRequestQueue> Queues =
                RuntimeServiceRef<IAIRequestQueue>.Optional();

            public static void PauseQueue() => Queues.ValueOrDefault?.PauseQueue();
            public static void ResumeQueue() => Queues.ValueOrDefault?.ResumeQueue();
            public static int ActiveRequestCount => Queues.ValueOrDefault?.ActiveRequestCount ?? 0;
            public static IReadOnlyList<TrackedRequest> GetActiveRequests() => Queues.ValueOrDefault?.GetActiveRequests() ?? new List<TrackedRequest>();
            public static IReadOnlyList<TrackedRequest> GetAllQueuedRequests() => Queues.ValueOrDefault?.GetAllQueuedRequests() ?? new List<TrackedRequest>();
            public static int TotalQueuedCount => Queues.ValueOrDefault?.TotalQueuedCount ?? 0;

            public static void ClearModCooldown(string modId) => Queues.ValueOrDefault?.ClearCooldown(modId);

            /// <summary>Remaining cooldown ticks for a mod (0 when ready or when queue is unavailable).</summary>
            public static int GetModCooldownTicksLeft(string modId)
                => Queues.ValueOrDefault?.GetCooldownTicksLeft(modId) ?? 0;

            /// <summary>Unified async request entry (callback style)</summary>
            public static void Send(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> onComplete)
            {
                Send(envelope, (result, _) => onComplete(result));
            }

            /// <summary>Unified async request entry (callback with context style)</summary>
            public static void Send(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>, LlmRequestContext> onComplete)
            {
                var scope = RuntimeServiceHub.Shared.Capture();
                if (scope.Snapshot.State != RuntimeLifecycleState.Running)
                {
                    onComplete?.Invoke(Result<LlmResponse, RimMindError>.Err(RimMindErrors.PipelineShortCircuited("shutdown")), null!);
                    return;
                }

                var queue = scope.GetOptional<IAIRequestQueue>();
                var clientManager = scope.GetOptional<IClientManager>();
                var pipeline = scope.GetOptional<IPipeline<LlmRequestContext>>();
                if (queue == null || clientManager == null || pipeline == null)
                {
                    onComplete?.Invoke(Result<LlmResponse, RimMindError>.Err(
                        RimMindErrors.PipelineShortCircuited("runtime services unavailable")), null!);
                    return;
                }

                var client = clientManager.GetClient();
                if (client == null)
                {
                    onComplete?.Invoke(Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientNotConfigured("No AI client available")), null!);
                    return;
                }

                var traceLog = scope.GetOptional<IAIRequestTraceLog>();
                var modelSettings = scope.GetOptional<IAIModelSettings>();
                var elapsed = Stopwatch.StartNew();
                traceLog?.StartRequest(
                    envelope.RequestId,
                    GetTraceSource(envelope),
                    modelSettings?.ModelName ?? string.Empty,
                    BuildTracePrompt(envelope, "system"),
                    BuildTracePrompt(envelope, "user"),
                    BuildTracePrompt(envelope, "assistant"));

                var executor = new QueuedPipelineRequestExecutor(pipeline, client, envelope);
                queue.Enqueue(envelope, result =>
                {
                    if (!RuntimeServiceHub.Shared.IsCurrent(scope.Token))
                    {
                        RuntimeServiceHub.Shared.RecordStaleCompletion();
                        return;
                    }

                    elapsed.Stop();
                    if (result.IsOk)
                    {
                        var response = result.Value;
                        traceLog?.CompleteRequest(envelope.RequestId, response.Content, response.TokensUsed, (int)elapsed.ElapsedMilliseconds);
                    }
                    else
                    {
                        traceLog?.FailRequest(envelope.RequestId, result.Error.Message, (int)elapsed.ElapsedMilliseconds);
                    }

                    onComplete?.Invoke(result, executor.Context!);
                }, ct => executor.ExecuteAsync(envelope, ct), client.IsLocalEndpoint);
            }

            /// <summary>Unified async request entry (Task style)</summary>
            public static Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
            {
                var scope = RuntimeServiceHub.Shared.Capture();
                var completionFence = scope.GetOptional<ICompletionFence>();
                var tcs = new TaskCompletionSource<Result<LlmResponse, RimMindError>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                void CompleteCancelled() => tcs.TrySetResult(
                    Result<LlmResponse, RimMindError>.Err(RimMindErrors.Cancelled()));

                if (!RequestCancellationRegistrations.TryCreate(
                        completionFence?.CancellationToken ?? CancellationToken.None,
                        envelope.Ct,
                        CompleteCancelled,
                        out RequestCancellationRegistrations? cancellationRegistrations,
                        out Exception? registrationFailure))
                {
                    RimMindError error = registrationFailure is ObjectDisposedException
                        ? RimMindErrors.Cancelled()
                        : RimMindErrors.Internal(
                            "Failed to register request cancellation.",
                            registrationFailure);
                    return Task.FromResult(Result<LlmResponse, RimMindError>.Err(error));
                }

                try
                {
                    if (!tcs.Task.IsCompleted)
                        Send(envelope, result => tcs.TrySetResult(result));
                }
                catch
                {
                    cancellationRegistrations.Dispose();
                    throw;
                }

                _ = tcs.Task.ContinueWith(
                    _ => cancellationRegistrations.Dispose(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                return tcs.Task;
            }

            private static string GetTraceSource(LlmRequestEnvelope envelope)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(envelope.ModId)) parts.Add($"mod:{envelope.ModId}");
                if (!string.IsNullOrWhiteSpace(envelope.ScenarioId)) parts.Add($"scenario:{envelope.ScenarioId}");
                if (!string.IsNullOrWhiteSpace(envelope.NpcId)) parts.Add($"npc:{envelope.NpcId}");
                return parts.Count > 0 ? string.Join(" | ", parts) : "unknown";
            }

            private static string BuildTracePrompt(LlmRequestEnvelope envelope, string role)
            {
                if (envelope.Messages == null || envelope.Messages.Count == 0)
                    return string.Empty;

                var sb = new StringBuilder();
                foreach (var message in envelope.Messages)
                {
                    if (!string.Equals(message.Role, role, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.IsNullOrWhiteSpace(message.Content))
                        continue;

                    if (sb.Length > 0)
                        sb.AppendLine().AppendLine();

                    if (!string.IsNullOrWhiteSpace(message.LayerTag))
                        sb.Append('[').Append(message.LayerTag).Append("] ");

                    sb.Append(message.Content);
                }

                return sb.ToString();
            }
        }
    }
}
