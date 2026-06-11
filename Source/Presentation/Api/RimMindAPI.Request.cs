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
                RimMindRuntime.Instance.UnifiedPipeline.ExecuteAsync(ctx).ContinueWith(_ =>
                {
                    onComplete?.Invoke(ctx.Result ?? Result<LlmResponse, RimMindError>.Err(RimMindErrors.Internal("Pipeline produced no result.")), ctx);
                }, TaskContinuationOptions.ExecuteSynchronously);
            }

            /// <summary>Unified async request entry (Task style)</summary>
            public static Task<Result<LlmResponse, RimMindError>> SendAsync(LlmRequestEnvelope envelope)
            {
                var tcs = new TaskCompletionSource<Result<LlmResponse, RimMindError>>();
                Send(envelope, result => tcs.SetResult(result));
                return tcs.Task;
            }
        }
    }
}
