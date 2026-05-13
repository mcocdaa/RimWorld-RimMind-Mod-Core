using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Queue;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation;
using RimMind.Presentation.Pipeline.AI;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Verse;

namespace RimMind.Presentation
{
    public static partial class RimMindAPI
    {
        public static class Request
        {
            public static void RequestImmediate(AIRequest request, Action<Result<AIResponse, RimMindError>> onComplete)
            {
                var client = Bus.GetClient();
                if (client == null)
                {
                    onComplete?.Invoke(Result<AIResponse, RimMindError>.Ok(AIResponse.Ok(request.RequestId, "", 0)));
                    return;
                }
                RimMindRuntime.Instance.Queue.EnqueueImmediate(request, response => onComplete?.Invoke(Result<AIResponse, RimMindError>.Ok(response)), client);
            }

            public static void RequestStructuredAsync(AIRequest request, string? jsonSchema, Action<Result<AIResponse, RimMindError>> onComplete, List<StructuredTool>? tools = null)
            {
                var s = RimMindCoreMod.Settings;
                if (s == null || !s.IsConfigured())
                {
                    onComplete?.Invoke(Result<AIResponse, RimMindError>.Ok(AIResponse.Ok(request.RequestId, "", 0)));
                    return;
                }

                request.UseJsonMode = true;
                if (!string.IsNullOrEmpty(jsonSchema))
                    request.JsonSchema = jsonSchema;
                if (tools != null && tools.Count > 0)
                    request.Tools = tools;

                var client = Bus.GetClient();
                if (client == null)
                {
                    onComplete?.Invoke(Result<AIResponse, RimMindError>.Ok(AIResponse.Ok(request.RequestId, "", 0)));
                    return;
                }

                RimMindRuntime.Instance.Queue.Enqueue(request, response => onComplete?.Invoke(Result<AIResponse, RimMindError>.Ok(response)), client);
            }

            public static void RequestStructured(ContextRequest request, string schema,
                Action<Result<AIResponse, RimMindError>> onComplete, List<StructuredTool>? tools = null)
            {
                if (RimMindRuntime.Instance.IsShutdown)
                {
                    onComplete?.Invoke(Result<AIResponse, RimMindError>.Ok(AIResponse.Ok($"Structured_{request.NpcId}", "", 0)));
                    return;
                }

                var snapshot = RimMindRuntime.Instance.ContextEngine.BuildSnapshot(request);
                var aiRequest = new AIRequest
                {
                    SystemPrompt = string.Empty,
                    Messages = new List<ChatMessage>(snapshot.Messages),
                    MaxTokens = snapshot.MaxTokens,
                    Temperature = snapshot.Temperature,
                    RequestId = $"Structured_{request.NpcId}",
                    ModId = request.Scenario.ToString(),
                    ExpireAtTicks = Find.TickManager.TicksGame + (RimMindCoreMod.Settings?.requestExpireTicks ?? 30000),
                    UseJsonMode = true,
                    Priority = AIRequestPriority.Normal,
                };
                if (!string.IsNullOrEmpty(schema)) aiRequest.JsonSchema = schema;
                if (tools != null && tools.Count > 0) aiRequest.Tools = tools;

                var ctx = new AIRequestContext { Request = aiRequest, Client = Bus.GetClient(), Snapshot = snapshot };
                aiRequest.TraceId = ctx.TraceId;
                RimMindRuntime.Instance.AIRequestPipeline.ExecuteAsync(ctx).ContinueWith(_ =>
                {
                    onComplete?.Invoke(ctx.Result ?? Result<AIResponse, RimMindError>.Err(RimMindErrors.Internal("Pipeline produced no result.")));
                }, TaskContinuationOptions.ExecuteSynchronously);
            }

            public static void PauseQueue() => RimMindRuntime.Instance.Queue?.PauseQueue();
            public static void ResumeQueue() => RimMindRuntime.Instance.Queue?.ResumeQueue();
            public static int ActiveRequestCount => RimMindRuntime.Instance.Queue?.ActiveRequestCount ?? 0;
            public static IReadOnlyList<TrackedRequest> GetActiveRequests() => RimMindRuntime.Instance.Queue?.GetActiveRequests() ?? new List<TrackedRequest>();
            public static IReadOnlyList<TrackedRequest> GetAllQueuedRequests() => RimMindRuntime.Instance.Queue?.GetAllQueuedRequests() ?? new List<TrackedRequest>();
            public static int TotalQueuedCount => RimMindRuntime.Instance.Queue?.TotalQueuedCount ?? 0;

            public static void ClearModCooldown(string modId) => RimMindRuntime.Instance.Queue?.ClearCooldown(modId);
        }
    }
}
