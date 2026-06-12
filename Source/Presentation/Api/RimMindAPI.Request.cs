using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Queue;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Presentation.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimMind.Application.Api
{
    public static partial class RimMindAPI
    {
        public static class Request
        {
            public static void PauseQueue() => RimMindRuntime.Instance.Queue?.PauseQueue();
            public static void ResumeQueue() => RimMindRuntime.Instance.Queue?.ResumeQueue();
            public static int ActiveRequestCount => RimMindRuntime.Instance.Queue?.ActiveRequestCount ?? 0;
            public static IReadOnlyList<TrackedRequest> GetActiveRequests() => RimMindRuntime.Instance.Queue?.GetActiveRequests() ?? new List<TrackedRequest>();
            public static IReadOnlyList<TrackedRequest> GetAllQueuedRequests() => RimMindRuntime.Instance.Queue?.GetAllQueuedRequests() ?? new List<TrackedRequest>();
            public static int TotalQueuedCount => RimMindRuntime.Instance.Queue?.TotalQueuedCount ?? 0;

            public static void ClearModCooldown(string modId) => RimMindRuntime.Instance.Queue?.ClearCooldown(modId);

            /// <summary>Unified async request entry (callback style)</summary>
            public static void Send(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> onComplete)
            {
                Send(envelope, (result, _) => onComplete(result));
            }

            /// <summary>Unified async request entry (callback with context style)</summary>
            public static void Send(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>, LlmRequestContext> onComplete)
            {
                if (RimMindRuntime.Instance.IsShutdown)
                {
                    onComplete?.Invoke(Result<LlmResponse, RimMindError>.Err(RimMindErrors.PipelineShortCircuited("shutdown")), null!);
                    return;
                }

                var client = Bus.GetClient();
                if (client == null)
                {
                    onComplete?.Invoke(Result<LlmResponse, RimMindError>.Err(RimMindErrors.ClientNotConfigured("No AI client available")), null!);
                    return;
                }

                var ctx = new LlmRequestContext { Envelope = envelope, Client = client };
                var traceLog = RimMindServiceLocator.TryGet<IAIRequestTraceLog>();
                var elapsed = Stopwatch.StartNew();
                traceLog?.StartRequest(
                    envelope.RequestId,
                    GetTraceSource(envelope),
                    RimMindServiceLocator.TryGet<IAIModelSettings>()?.ModelName ?? string.Empty,
                    BuildTracePrompt(envelope, "system"),
                    BuildTracePrompt(envelope, "user"),
                    BuildTracePrompt(envelope, "assistant"));

                RimMindRuntime.Instance.UnifiedPipeline.ExecuteAsync(ctx).ContinueWith(task =>
                {
                    elapsed.Stop();
                    if (task.IsFaulted)
                    {
                        var message = task.Exception?.GetBaseException().Message ?? "Pipeline execution failed.";
                        traceLog?.FailRequest(envelope.RequestId, message);
                        onComplete?.Invoke(Result<LlmResponse, RimMindError>.Err(RimMindErrors.Internal(message)), ctx);
                        return;
                    }

                    var result = ctx.Result ?? Result<LlmResponse, RimMindError>.Err(RimMindErrors.Internal("Pipeline produced no result."));
                    if (result.IsOk)
                    {
                        var response = result.Value;
                        traceLog?.CompleteRequest(envelope.RequestId, response.Content, response.TokensUsed, (int)elapsed.ElapsedMilliseconds);
                    }
                    else
                    {
                        traceLog?.FailRequest(envelope.RequestId, result.Error.Message);
                    }

                    onComplete?.Invoke(result, ctx);
                }, TaskContinuationOptions.ExecuteSynchronously);
            }

            /// <summary>Unified async request entry (Task style)</summary>
            public static Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
            {
                var tcs = new TaskCompletionSource<Result<LlmResponse, RimMindError>>();
                Send(envelope, result => tcs.SetResult(result));
                return tcs.Task;
            }

            private static string GetTraceSource(LlmRequestEnvelope envelope)
            {
                if (!string.IsNullOrWhiteSpace(envelope.NpcId)) return $"npc:{envelope.NpcId}";
                if (!string.IsNullOrWhiteSpace(envelope.ModId)) return envelope.ModId;
                if (!string.IsNullOrWhiteSpace(envelope.ScenarioId)) return envelope.ScenarioId;
                return "unknown";
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
