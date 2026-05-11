using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts;
using RimMind.Contracts.Result;
using RimMind.Kernel.Bus;
using RimMind.Contracts.Client;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Sensor;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Tools;
using RimMind.Core.Runtime;
using RimMind.Kernel.Logging;
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
            var schema = SchemaRegistry.AgentDecision;
            var snapshot = RimMindRuntime.Instance.ContextEngine.BuildSnapshot(ctxRequest);
            if (snapshot == null) return;
            var toolDefs = RimMindAPI.Tools.GetAllDefinitions();
            var tools = toolDefs.Select(d => new StructuredTool
            {
                Name = d.Id,
                Description = d.Description,
                Parameters = d.ParametersSchema
            }).ToList();
            _lastMessages = new List<ChatMessage>(snapshot.Messages);
            _lastSchema = schema; _lastTools = tools;
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
            if (response.State != AIRequestState.Completed) return;
            if (string.IsNullOrEmpty(response.Content)) return;
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
                RimMindErrors.Warn($"[RimMind-Core] HandleThinkResponse parse failed for NPC-{_pawn?.thingIDNumber}: {ex.Message}");
            }
        }

        public void ForceThink() => _lastThinkTick = -(RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000);

        public void ExposeData()
        {
            Scribe_Values.Look(ref _lastThinkTick, "lastThinkTick", -(RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000));
            Scribe_Values.Look(ref _lastSchema, "lastSchema", null);
        }
    }
}
