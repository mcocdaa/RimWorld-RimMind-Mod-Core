using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Kernel.Bus;
using RimMind.Core.Client;
using RimMind.Kernel.Context;
using RimMind.Core.Internal;
using RimMind.Core.Runtime;
using RimMind.Core.Sensor;
using RimMind.Core.Settings;
using RimMind.Kernel.Json;
using RimMind.Kernel.Prompt;
using Verse;

namespace RimMind.Core.Agent
{
    public class PawnThinker
    {
        private readonly Pawn _pawn;
        private readonly IEventBus _eventBus;
        private readonly AgentGoalStack _goalStack;
        private readonly PawnActor _actor;
        private readonly PawnRecorder _recorder;
        private List<ChatMessage>? _lastMessages;
        private List<StructuredTool>? _lastTools;
        private List<StructuredTool>? _lastSensorTools;
        private string? _lastSchema;
        private int _toolCallDepth;
        private int _lastThinkTick = -(RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000);

        public int LastThinkTick => _lastThinkTick;

        public PawnThinker(Pawn pawn, IEventBus eventBus, AgentGoalStack goalStack, PawnActor actor, PawnRecorder recorder)
        {
            _pawn = pawn; _eventBus = eventBus; _goalStack = goalStack; _actor = actor; _recorder = recorder;
        }

        public void Think(IReadOnlyList<PerceptionBufferEntry> perceptions)
        {
            if (_goalStack.ActiveCount == 0) { GenerateGoalsIfNeeded(perceptions); if (_goalStack.ActiveCount == 0) return; }
            var topGoal = _goalStack.ActiveGoals[0];
            var npcId = $"NPC-{_pawn.thingIDNumber}";
            var ctxRequest = new ContextRequest
            {
                NpcId = npcId, Scenario = ScenarioIds.Decision,
                Budget = RimMindCoreMod.Settings?.Context?.ContextBudget ?? 0.6f,
                CurrentQuery = topGoal.Description,
                MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
            };
            var schema = Context.SchemaRegistry.AgentDecision;
            var snapshot = RimMindRuntime.Instance.ContextEngine.BuildSnapshot(ctxRequest);
            if (snapshot == null) return;
            var bridge = RimMindRuntime.Instance.GetAgentActionBridge();
            var tools = bridge?.GetAvailableTools(_pawn);
            if (tools != null && tools.Count > 0) tools = _recorder.StrategyOptimizer.GetWeightedTools(tools);
            _lastMessages = new List<ChatMessage>(snapshot.Messages);
            _lastSchema = schema; _toolCallDepth = 0; _lastTools = tools;
            var sensorMgr = RimMindServiceLocator.Get<ISensorManager>();
            if (sensorMgr != null) _lastSensorTools = sensorMgr.BuildAgentTools(_pawn);
            var aiRequest = new AIRequest
            {
                SystemPrompt = null!, Messages = new List<ChatMessage>(snapshot.Messages),
                MaxTokens = snapshot.MaxTokens, Temperature = snapshot.Temperature,
                RequestId = $"Structured_{npcId}", ModId = ctxRequest.Scenario,
                ExpireAtTicks = Find.TickManager.TicksGame + (RimMindCoreMod.Settings?.requestExpireTicks ?? 30000),
                UseJsonMode = true, Priority = AIRequestPriority.Normal,
            };
            RimMindRuntime.Instance.RequestStructuredAsync(aiRequest, schema, response =>
            {
                _lastThinkTick = Find.TickManager.TicksGame;
                HandleThinkResponse(response, topGoal);
            }, _lastTools);
        }

        private void GenerateGoalsIfNeeded(IReadOnlyList<PerceptionBufferEntry> perceptions)
        {
            if (_goalStack.TotalCount > 0 || _pawn == null) return;
            foreach (var g in GoalGenerator.GenerateFromIdentity(_pawn)) _goalStack.TryAdd(g, _pawn.thingIDNumber);
            foreach (var g in GoalGenerator.GenerateFromState(_pawn)) _goalStack.TryAdd(g, _pawn.thingIDNumber);
            if (perceptions.Count > 0)
            {
                var latest = perceptions[perceptions.Count - 1];
                foreach (var g in GoalGenerator.GenerateFromEvent(latest.PerceptionType, latest.Content))
                    _goalStack.TryAdd(g, _pawn.thingIDNumber);
            }
        }

        private void HandleThinkResponse(AIResponse response, AgentGoal goal)
        {
            if (!response.Success) return;
            if (string.IsNullOrEmpty(response.Content) && string.IsNullOrEmpty(response.ToolCallsJson)) return;
            if (!string.IsNullOrEmpty(response.ToolCallsJson)) { HandleToolCalls(response.ToolCallsJson!, goal); return; }
            var content = response.Content ?? "";
            var json = JsonTagExtractor.ExtractRaw(content, "Action") ?? content;
            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                string? action = obj["action"]?.ToString();
                string? reason = obj["reason"]?.ToString();
                if (!string.IsNullOrEmpty(action)) _actor.Execute(action!, null, reason ?? "");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Core] HandleThinkResponse parse failed for NPC-{_pawn?.thingIDNumber}: {ex.Message}");
            }
        }

        private void HandleToolCalls(string toolCallsJson, AgentGoal goal)
        {
            List<StructuredToolCall>? toolCalls;
            try { toolCalls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<StructuredToolCall>>(toolCallsJson); }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Core] PawnAgent ToolCalls parse failed for NPC-{_pawn?.thingIDNumber}: {ex.Message}");
                return;
            }
            if (toolCalls == null || toolCalls.Count == 0) return;
            var decisionResults = new List<(StructuredToolCall tc, string result)>();
            foreach (var tc in toolCalls)
            {
                if (string.IsNullOrEmpty(tc.Name)) continue;
                var (targetName, reason) = ParseToolCallArgs(tc);
                _actor.Execute(tc.Name, targetName, reason);
                decisionResults.Add((tc, $"Decision recorded: {tc.Name}"));
            }
            if (_toolCallDepth < (RimMindCoreMod.Settings?.maxToolCallDepth ?? 3) && decisionResults.Count > 0)
                RequestToolFeedback(toolCallsJson, decisionResults);
        }

        private (string? targetName, string reason) ParseToolCallArgs(StructuredToolCall tc)
        {
            string? targetName = null; string reason = tc.Name;
            if (string.IsNullOrEmpty(tc.Arguments)) return (targetName, reason);
            try
            {
                var args = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tc.Arguments);
                if (args != null)
                {
                    if (args.TryGetValue("target", out var t)) targetName = t;
                    if (args.TryGetValue("reason", out var r)) reason = r;
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Core] PawnAgent tool call args parse failed for NPC-{_pawn?.thingIDNumber}: {ex.Message}");
            }
            return (targetName, reason);
        }

        private void RequestToolFeedback(string toolCallsJson, List<(StructuredToolCall tc, string result)> results)
        {
            _toolCallDepth++;
            List<StructuredToolCall>? toolCalls;
            try { toolCalls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<StructuredToolCall>>(toolCallsJson); }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Core] PawnAgent ToolCalls re-parse failed for NPC-{_pawn?.thingIDNumber}: {ex.Message}");
                return;
            }
            var messages = new List<ChatMessage>(_lastMessages ?? new List<ChatMessage>());
            messages.Add(new ChatMessage
            {
                Role = "assistant", Content = "",
                ToolCalls = toolCalls?.Select(tc => new ChatToolCall { Id = tc.Id, Name = tc.Name, Arguments = tc.Arguments }).ToList() ?? new List<ChatToolCall>()
            });
            foreach (var (tc, result) in results)
                messages.Add(new ChatMessage { Role = "tool", Content = result, ToolCallId = tc.Id });
            _lastMessages = messages;
            var npcId = $"NPC-{_pawn.thingIDNumber}";
            var followUpRequest = new AIRequest
            {
                Messages = messages, MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
                RequestId = $"Structured_{npcId}_fb{_toolCallDepth}", ModId = "Decision",
                ExpireAtTicks = Find.TickManager.TicksGame + (RimMindCoreMod.Settings?.requestExpireTicks ?? 30000),
                UseJsonMode = true, Priority = AIRequestPriority.Normal,
            };
            var topGoal = _goalStack.ActiveCount > 0 ? _goalStack.ActiveGoals[0] : null;
            RimMindRuntime.Instance.RequestStructuredAsync(followUpRequest, _lastSchema, response =>
            {
                _lastThinkTick = Find.TickManager.TicksGame;
                if (topGoal != null) HandleThinkResponse(response, topGoal);
            }, _lastTools);
        }

        public void ForceThink() => _lastThinkTick = -(RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000);

        public void ExposeData()
        {
            Scribe_Values.Look(ref _lastThinkTick, "lastThinkTick", -(RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000));
            Scribe_Values.Look(ref _toolCallDepth, "toolCallDepth", 0);
            Scribe_Values.Look(ref _lastSchema, "lastSchema", null);
        }
    }
}
