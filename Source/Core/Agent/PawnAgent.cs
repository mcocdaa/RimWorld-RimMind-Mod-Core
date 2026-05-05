using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Core.AgentBus;
using RimMind.Core.Client;
using RimMind.Core.Context;
using RimMind.Core.Internal;
using RimMind.Core.Npc;
using RimMind.Core.Settings;
using Verse;

namespace RimMind.Core.Agent
{
    public class PawnAgent : IExposable
    {
        public Pawn Pawn { get; private set; }
        public AgentState State { get; private set; } = AgentState.Dormant;
        public AgentIdentity Identity { get; private set; } = new AgentIdentity();

        private readonly AgentGoalStack _goalStack = new AgentGoalStack();
        private readonly Queue<BehaviorRecord> _behaviorHistory = new Queue<BehaviorRecord>();
        private StrategyOptimizer _strategyOptimizer = new StrategyOptimizer();
        private readonly PerceptionBuffer _perceptionBuffer = new PerceptionBuffer();
        private readonly PerceptionPipeline _perceptionPipeline;
        private readonly List<PerceptionBufferEntry> _pendingPerceptions = new List<PerceptionBufferEntry>();
        private Action<PerceptionEvent>? _perceptionHandler;
        private Action<ActionEvent>? _actionEventHandler;
        private string? _perceptionSubscriptionKey;
        private string? _actionSubscriptionKey;
        private int _lastThinkTick = -(RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000);

        private List<ChatMessage>? _lastMessages;
        private List<StructuredTool>? _lastTools;
        private List<StructuredTool>? _lastSensorTools;
        private string? _lastSchema;
        private int _toolCallDepth;
        private Verse.AI.Job? _pendingJob;

        public AgentGoalStack GoalStack => _goalStack;
        public IReadOnlyList<BehaviorRecord> BehaviorHistory => _behaviorHistory.ToList();
        public StrategyOptimizer StrategyOptimizer => _strategyOptimizer;
        public PerceptionBuffer PerceptionBuffer => _perceptionBuffer;

        public bool IsActive => State == AgentState.Active;

        public PawnAgent(Pawn pawn)
        {
            Pawn = pawn;
            _perceptionPipeline = new PerceptionPipeline();
            _perceptionPipeline.AddFilter(new DedupFilter());
            _perceptionPipeline.AddFilter(new PriorityFilter());
            _perceptionPipeline.AddFilter(new CooldownFilter());
            _perceptionHandler = OnPerceptionEvent;
            _actionEventHandler = OnActionEvent;
            string pawnSubKey = $"PawnAgent_{pawn?.thingIDNumber ?? 0}";
            _perceptionSubscriptionKey = $"{pawnSubKey}_Perception";
            _actionSubscriptionKey = $"{pawnSubKey}_Action";
            global::RimMind.Core.AgentBus.AgentBus.Subscribe(_perceptionSubscriptionKey, _perceptionHandler);
            global::RimMind.Core.AgentBus.AgentBus.Subscribe<ActionEvent>(_actionSubscriptionKey, _actionEventHandler);
        }

        public void Tick()
        {
            if (State != AgentState.Active) return;
            if (Pawn == null || Pawn.Dead) { TransitionTo(AgentState.Terminated); return; }
            if (!Pawn.IsHashIntervalTick(RimMindCoreMod.Settings?.agentTickInterval ?? 150)) return;

            _goalStack.CheckExpired(Pawn?.thingIDNumber ?? -1);
            _strategyOptimizer.DecayAll();
            Perceive();
            Think();
        }

        private void Perceive()
        {
            var raw = _perceptionBuffer.Flush();
            if (raw.Count == 0) return;
            var filtered = _perceptionPipeline.Process(raw);
            _pendingPerceptions.AddRange(filtered);

            // ���� Sensor ��֪���� ��������������������������������������������������
            var sensorMgr = Sensor.SensorManager.Instance;
            if (sensorMgr != null)
            {
                var sensorTools = sensorMgr.BuildAgentTools(Pawn);
                _lastSensorTools = sensorTools;
            }
        }

        private void Think()
        {
            if (Find.TickManager.TicksGame - _lastThinkTick < (RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000)) return;

            if (_goalStack.ActiveCount == 0)
            {
                GenerateGoalsIfNeeded();
                if (_goalStack.ActiveCount == 0) return;
            }

            var topGoal = _goalStack.ActiveGoals[0];
            var npcId = $"NPC-{Pawn.thingIDNumber}";
            var ctxRequest = new ContextRequest
            {
                NpcId = npcId,
                Scenario = ScenarioIds.Decision,
                Budget = RimMindCoreMod.Settings.Context.ContextBudget,
                CurrentQuery = topGoal.Description,
                MaxTokens = RimMindCoreMod.Settings.maxTokens,
                Temperature = RimMindCoreMod.Settings.defaultTemperature,
            };

            var schema = Context.SchemaRegistry.AgentDecision;

            var snapshot = RimMindAPI.BuildContextSnapshot(ctxRequest);
            var bridge = RimMindAPI.GetAgentActionBridge();
            var tools = bridge?.GetAvailableTools(Pawn);
            if (tools != null && tools.Count > 0)
                tools = _strategyOptimizer.GetWeightedTools(tools);
            _lastMessages = new List<ChatMessage>(snapshot.Messages);
            _lastSchema = schema;
            _toolCallDepth = 0;
            _lastTools = tools;

            var aiRequest = new AIRequest
            {
                SystemPrompt = null!,
                Messages = new List<ChatMessage>(snapshot.Messages),
                MaxTokens = snapshot.MaxTokens,
                Temperature = snapshot.Temperature,
                RequestId = $"Structured_{npcId}",
                ModId = ctxRequest.Scenario,
                ExpireAtTicks = Find.TickManager.TicksGame + (RimMindCoreMod.Settings?.requestExpireTicks ?? 30000),
                UseJsonMode = true,
                Priority = AIRequestPriority.Normal,
            };

            RimMindAPI.RequestStructuredAsync(aiRequest, schema, response =>
            {
                _lastThinkTick = Find.TickManager.TicksGame;
                HandleThinkResponse(response, topGoal);
            }, _lastTools);
            _pendingPerceptions.Clear();
        }

        private void GenerateGoalsIfNeeded()
        {
            if (_goalStack.TotalCount > 0) return;
            if (Pawn == null) return;

            var identityGoals = GoalGenerator.GenerateFromIdentity(Pawn);
            foreach (var g in identityGoals) _goalStack.TryAdd(g, Pawn.thingIDNumber);

            var stateGoals = GoalGenerator.GenerateFromState(Pawn);
            foreach (var g in stateGoals) _goalStack.TryAdd(g, Pawn.thingIDNumber);

            if (_pendingPerceptions.Count > 0)
            {
                var latest = _pendingPerceptions[_pendingPerceptions.Count - 1];
                var eventGoals = GoalGenerator.GenerateFromEvent(latest.PerceptionType, latest.Content);
                foreach (var g in eventGoals) _goalStack.TryAdd(g, Pawn.thingIDNumber);
            }
        }

        private void HandleThinkResponse(AIResponse response, AgentGoal goal)
        {
            if (!response.Success) return;

            if (string.IsNullOrEmpty(response.Content) && string.IsNullOrEmpty(response.ToolCallsJson))
                return;

            if (!string.IsNullOrEmpty(response.ToolCallsJson))
            {
                HandleToolCalls(response.ToolCallsJson!, goal);
                return;
            }

            var content = response.Content ?? "";
            var json = JsonTagExtractor.ExtractRaw(content, "Action")
                ?? content;

            try
            {
                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                string? action = obj["action"]?.ToString();
                string? reason = obj["reason"]?.ToString();

                if (!string.IsNullOrEmpty(action))
                {
                    PublishDecisionAndRecord(action!, null, reason ?? "");
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Core] HandleThinkResponse parse failed for NPC-{Pawn?.thingIDNumber}: {ex.Message}");
            }
        }

        private void HandleToolCalls(string toolCallsJson, AgentGoal goal)
        {
            List<StructuredToolCall>? toolCalls;
            try
            {
                toolCalls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<StructuredToolCall>>(toolCallsJson);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Core] PawnAgent ToolCalls parse failed for NPC-{Pawn?.thingIDNumber}: {ex.Message}");
                return;
            }

            if (toolCalls == null || toolCalls.Count == 0) return;

            var decisionResults = new List<(StructuredToolCall tc, string result)>();

            foreach (var tc in toolCalls)
            {
                if (string.IsNullOrEmpty(tc.Name)) continue;

                string? targetName = null;
                string? param = tc.Arguments;
                string? reason = tc.Name;

                if (!string.IsNullOrEmpty(tc.Arguments))
                {
                    try
                    {
                        var args = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(tc.Arguments);
                        if (args != null)
                        {
                            if (args.TryGetValue("target", out var t)) targetName = t;
                            if (args.TryGetValue("param", out var p)) param = p;
                            if (args.TryGetValue("reason", out var r)) reason = r;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"[RimMind-Core] PawnAgent tool call args parse failed for NPC-{Pawn?.thingIDNumber}: {ex.Message}");
                    }
                }

                PublishDecisionAndRecord(tc.Name, targetName, reason ?? tc.Name);
                decisionResults.Add((tc, $"Decision recorded: {tc.Name}"));
            }

            if (_toolCallDepth < (RimMindCoreMod.Settings?.maxToolCallDepth ?? 3) && decisionResults.Count > 0)
            {
                RequestToolFeedback(toolCallsJson, decisionResults);
            }
        }

        private void RequestToolFeedback(string toolCallsJson, List<(StructuredToolCall tc, string result)> results)
        {
            _toolCallDepth++;

            List<StructuredToolCall>? toolCalls;
            try
            {
                toolCalls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<StructuredToolCall>>(toolCallsJson);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimMind-Core] PawnAgent ToolCalls re-parse failed for NPC-{Pawn?.thingIDNumber}: {ex.Message}");
                return;
            }

            var messages = new List<ChatMessage>(_lastMessages ?? new List<ChatMessage>());

            messages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = "",
                ToolCalls = toolCalls?.Select(tc => new ChatToolCall
                {
                    Id = tc.Id,
                    Name = tc.Name,
                    Arguments = tc.Arguments,
                }).ToList() ?? new List<ChatToolCall>()
            });

            foreach (var (tc, result) in results)
            {
                messages.Add(new ChatMessage
                {
                    Role = "tool",
                    Content = result,
                    ToolCallId = tc.Id,
                });
            }

            _lastMessages = messages;

            var npcId = $"NPC-{Pawn.thingIDNumber}";
            var followUpRequest = new AIRequest
            {
                Messages = messages,
                MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
                RequestId = $"Structured_{npcId}_fb{_toolCallDepth}",
                ModId = "Decision",
                ExpireAtTicks = Find.TickManager.TicksGame + (RimMindCoreMod.Settings?.requestExpireTicks ?? 30000),
                UseJsonMode = true,
                Priority = AIRequestPriority.Normal,
            };

            var topGoal = _goalStack.ActiveCount > 0 ? _goalStack.ActiveGoals[0] : null;
            RimMindAPI.RequestStructuredAsync(followUpRequest, _lastSchema, response =>
            {
                _lastThinkTick = Find.TickManager.TicksGame;
                if (topGoal != null)
                    HandleThinkResponse(response, topGoal);
            }, _lastTools);
        }

        private void PublishDecisionAndRecord(string action, string? targetName, string reason)
        {
            string eventId = Guid.NewGuid().ToString("N").Substring(0, 8);

            var bridge = RimMindAPI.GetAgentActionBridge();
            bool executed = false;
            string resultReason = "No action bridge";

            if (bridge != null)
            {
                Pawn? targetPawn = null;
                if (!string.IsNullOrEmpty(targetName) && Pawn?.Map != null)
                {
                    if (int.TryParse(targetName, out int targetThingId))
                    {
                        var indexed = NpcManager.FindPawnByNpcId($"NPC-{targetThingId}");
                        if (indexed != null) targetPawn = indexed;
                    }
                    if (targetPawn == null)
                    {
                        targetPawn = Pawn.Map?.mapPawns?.AllPawns?
                            .FirstOrDefault(p => p.thingIDNumber.ToString() == targetName);
                    }
                    if (targetPawn == null)
                    {
                        targetPawn = Pawn.Map?.mapPawns?.AllPawns?
                            .FirstOrDefault(p => p.LabelShortCap == targetName);
                    }
                }
                try
                {
                    executed = bridge.Execute(action, Pawn!, targetPawn, null, eventId);
                    resultReason = executed ? "Executed" : "Execution failed";
                }
                catch (System.Exception ex)
                {
                    resultReason = $"Bridge error: {ex.Message}";
                    Log.Warning($"[RimMind-Core] ActionBridge error for {action}: {ex.Message}");
                }
            }

            global::RimMind.Core.AgentBus.AgentBus.Publish(new DecisionEvent(
                $"NPC-{Pawn?.thingIDNumber}",
                Pawn?.thingIDNumber ?? -1,
                "goal_driven",
                reason,
                action));

            var topGoal = _goalStack.ActiveCount > 0 ? _goalStack.ActiveGoals[0] : null;

            RecordBehavior(new BehaviorRecord
            {
                Action = action,
                Reason = reason,
                Success = executed,
                ResultReason = resultReason,
                GoalProgressDelta = ComputeGoalProgressDelta(action, executed),
                Timestamp = Find.TickManager?.TicksGame ?? 0,
                ActionEventId = eventId,
            });

            if (topGoal != null)
            {
                topGoal.Progress += ComputeGoalProgressDelta(action, executed);
                if (topGoal.Progress >= 1f)
                {
                    topGoal.Status = GoalStatus.Achieved;
                    _goalStack.Remove(topGoal.Description, Pawn?.thingIDNumber ?? -1);
                }
            }
        }

        private static float ComputeGoalProgressDelta(string action, bool executed)
        {
            float baseDelta = action switch
            {
                "force_rest" => 0.15f,
                "assign_work" => 0.2f,
                "move_to" => 0.05f,
                "tend_pawn" => 0.2f,
                "rescue_pawn" => 0.25f,
                "draft" or "undraft" => 0.1f,
                "eat_food" => 0.15f,
                _ => 0.1f
            };
            return executed ? baseDelta : baseDelta * -0.5f;
        }

        public bool TransitionTo(AgentState newState)
        {
            if (!AgentStateTransition.CanTransition(State, newState)) return false;

            var previous = State;
            State = newState;

            string npcId = Pawn != null ? $"NPC-{Pawn.thingIDNumber}" : "";
            int pawnId = Pawn?.thingIDNumber ?? -1;
            global::RimMind.Core.AgentBus.AgentBus.Publish(
                new AgentLifecycleEvent(npcId, pawnId, previous.ToString(), newState.ToString()));

            if (newState == AgentState.Terminated)
                Cleanup();

            return true;
        }

        public void AddGoal(AgentGoal goal)
        {
            if (goal == null) return;
            _goalStack.TryAdd(goal, Pawn?.thingIDNumber ?? -1);
        }

        public void ForceThink()
        {
            _lastThinkTick = -(RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000);
        }

        public Verse.AI.Job? ConsumePendingJob()
        {
            var job = _pendingJob;
            _pendingJob = null;
            return job;
        }

        public void SetPendingJob(Verse.AI.Job job)
        {
            _pendingJob = job;
        }

        public bool RemoveGoal(string goalDescription)
        {
            return _goalStack.Remove(goalDescription, Pawn?.thingIDNumber ?? -1);
        }

        public void RecordBehavior(BehaviorRecord record)
        {
            _behaviorHistory.Enqueue(record);
            while (_behaviorHistory.Count > (RimMindCoreMod.Settings?.behaviorHistoryMax ?? 100))
                _behaviorHistory.Dequeue();
        }

        public void ExposeData()
        {
            var state = State;
            Scribe_Values.Look(ref state, "agentState", AgentState.Dormant);
            State = state;

            var identity = Identity;
            Scribe_Deep.Look(ref identity, "identity");
            Identity = identity ?? new AgentIdentity();

            _goalStack.ExposeData();

            var behaviorHistory = _behaviorHistory.ToList();
            Scribe_Collections.Look(ref behaviorHistory, "behaviorHistory", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                _behaviorHistory.Clear();
                if (behaviorHistory != null)
                    foreach (var entry in behaviorHistory)
                        _behaviorHistory.Enqueue(entry);
            }

            var strategyOptimizer = _strategyOptimizer;
            Scribe_Deep.Look(ref strategyOptimizer, "strategyOptimizer");
            if (strategyOptimizer != null) _strategyOptimizer = strategyOptimizer;

            Scribe_Values.Look(ref _lastThinkTick, "lastThinkTick", -(RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000));
            Scribe_Values.Look(ref _toolCallDepth, "toolCallDepth", 0);
            Scribe_Values.Look(ref _lastSchema, "lastSchema", null);
        }

        internal void Cleanup()
        {
            if (_perceptionSubscriptionKey != null)
            {
                global::RimMind.Core.AgentBus.AgentBus.Unsubscribe<PerceptionEvent>(_perceptionSubscriptionKey);
                _perceptionSubscriptionKey = null;
            }
            if (_actionSubscriptionKey != null)
            {
                global::RimMind.Core.AgentBus.AgentBus.Unsubscribe<ActionEvent>(_actionSubscriptionKey);
                _actionSubscriptionKey = null;
            }
            _perceptionHandler = null;
            _actionEventHandler = null;
            _goalStack.Clear();
            _perceptionBuffer.Clear();
            _pendingPerceptions.Clear();
        }

        internal void ResubscribeEvents()
        {
            if (_perceptionHandler == null)
            {
                _perceptionHandler = OnPerceptionEvent;
                string pawnSubKey = $"PawnAgent_{Pawn?.thingIDNumber ?? 0}";
                _perceptionSubscriptionKey = $"{pawnSubKey}_Perception";
                global::RimMind.Core.AgentBus.AgentBus.Subscribe(_perceptionSubscriptionKey, _perceptionHandler);
            }
            if (_actionEventHandler == null)
            {
                _actionEventHandler = OnActionEvent;
                string pawnSubKey = $"PawnAgent_{Pawn?.thingIDNumber ?? 0}";
                _actionSubscriptionKey = $"{pawnSubKey}_Action";
                global::RimMind.Core.AgentBus.AgentBus.Subscribe<ActionEvent>(_actionSubscriptionKey, _actionEventHandler);
            }
        }

        private void OnPerceptionEvent(PerceptionEvent evt)
        {
            if (State != AgentState.Active) return;
            if (Pawn == null) return;
            if (evt.PawnId != Pawn.thingIDNumber) return;
            _perceptionBuffer.Add(new PerceptionBufferEntry
            {
                PerceptionType = evt.PerceptionType,
                Content = evt.Content,
                Importance = evt.Importance,
                Timestamp = evt.Timestamp,
                PawnId = evt.PawnId
            });
        }

        private void OnActionEvent(ActionEvent evt)
        {
            if (State != AgentState.Active) return;
            if (Pawn == null) return;
            if (evt.PawnId != Pawn.thingIDNumber) return;

            float delta = evt.Success ? 0.1f : -0.2f;
            _strategyOptimizer.AdjustWeight(evt.ActionName, delta);

            var record = _behaviorHistory.LastOrDefault(r => r.ActionEventId == evt.EventId);
            if (record != null)
            {
                record.Success = evt.Success;
                record.ResultReason = evt.ResultReason ?? "";
            }
        }
    }
}
