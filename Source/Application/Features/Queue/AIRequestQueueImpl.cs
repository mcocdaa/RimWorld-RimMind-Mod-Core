using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Helpers;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Context;

namespace RimMind.Application.Features.Queue
{
    public class AIRequestQueueImpl : IAIRequestQueueTickable
    {
        private static AIRequestQueueImpl? _instance;

        private readonly ConcurrentQueue<(AIResponse response, Action<AIResponse> callback)> _results
            = new ConcurrentQueue<(AIResponse, Action<AIResponse>)>();
        private readonly ConcurrentQueue<(string msg, bool isWarning)> _pendingLogs
            = new ConcurrentQueue<(string, bool)>();

        private readonly object _queueLock = new object();
        private readonly ConcurrentDictionary<string, List<TrackedRequest>> _modQueues
            = new ConcurrentDictionary<string, List<TrackedRequest>>();
        private readonly ConcurrentDictionary<int, TrackedRequest> _activeRequests
            = new ConcurrentDictionary<int, TrackedRequest>();
        private readonly ConcurrentDictionary<string, TrackedRequest> _requestIdToActive
            = new ConcurrentDictionary<string, TrackedRequest>();

        private readonly CooldownTable _cooldowns = new CooldownTable();

        private readonly ILogSink? _logSink;

        private ILogSink? LogSink => _logSink;

        private readonly Func<ISettingsProvider?>? _settingsFactory;

        private Func<AIRequest, IAIClient, AIResponse>? _executeViaPipeline;

        private int _lastQueueProcessTick;
        private int _nextTrackingId;
        private bool _isPaused;
        private bool _isProcessingLocalRequest;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public int CurrentTick { get; set; }
        public Action<string, bool>? LogHandler { get; set; }
        public Action? FlushBackgroundQueue { get; set; }

        private ISettingsProvider Settings => _settingsFactory?.Invoke() ?? new DefaultSettingsProvider();

        private int QueueProcessInterval => Settings.QueueProcessInterval;

        public static IAIRequestQueue Instance
        {
            get => _instance ?? throw new InvalidOperationException("AIRequestQueue has not been initialized.");
        }

        public static void LogFromBackground(string msg, bool isWarning = false)
            => _instance?.EnqueueLog(msg, isWarning);

        public AIRequestQueueImpl(Func<ISettingsProvider?>? settingsFactory = null, ILogSink? logSink = null)
        {
            _settingsFactory = settingsFactory;
            _logSink = logSink;
            _instance = this;
        }

        internal void SetExecuteViaPipeline(Func<AIRequest, IAIClient, AIResponse> executeViaPipeline)
        {
            _executeViaPipeline = executeViaPipeline;
        }

        public void Tick()
        {
            FlushBackgroundQueue?.Invoke();
            while (_pendingLogs.TryDequeue(out var log)) { LogHandler?.Invoke(log.msg, log.isWarning); }
            while (_results.TryDequeue(out var item))
            {
                try { item.callback?.Invoke(item.response); }
                catch (Exception ex) { LogHandler?.Invoke($"[RimMind-Core] Callback exception for {item.response.RequestId}: {ex}", true); }
            }
            CheckActiveRequestTimeouts();
            int now = CurrentTick;
            if (now - _lastQueueProcessTick >= QueueProcessInterval) { _lastQueueProcessTick = now; ProcessAllQueues(now); }
        }

        public void Reset()
        {
            CancelAllRequests();
            lock (_queueLock)
            {
                foreach (var kvp in _activeRequests)
                {
                    var response = AIResponse.Ok(kvp.Value.Request.RequestId, "", 0);
                    response.Priority = kvp.Value.Request.Priority;
                    _results.Enqueue((response, kvp.Value.Callback));
                }
                ClearAllQueues();
                _activeRequests.Clear();
                _requestIdToActive.Clear();
                _isProcessingLocalRequest = false;
                _isPaused = false;
            }
            _cooldowns.ClearAll();
        }

        public void CancelAllRequests() { _cts.Cancel(); _cts.Dispose(); _cts = new CancellationTokenSource(); }

        public void Enqueue(AIRequest request, Action<AIResponse> callback, IAIClient client)
        {
            lock (_queueLock)
            {
                string modId = !string.IsNullOrEmpty(request.ModId) ? request.ModId : "Unknown";
                if (!_modQueues.TryGetValue(modId, out var queue)) { queue = new List<TrackedRequest>(); _modQueues[modId] = queue; }
                int trackingId = _nextTrackingId++;
                var tracked = new TrackedRequest
                {
                    TrackingId = trackingId, Request = request, Callback = callback, Client = client,
                    IsLocalEndpointSnapshot = client.IsLocalEndpoint, State = AIRequestState.Queued,
                    EnqueuedAtTick = CurrentTick, AttemptCount = 1, MaxAttempts = 1,
                };
                int insertIdx = queue.FindIndex(t => t.Request.Priority > request.Priority);
                if (insertIdx >= 0) queue.Insert(insertIdx, tracked); else queue.Add(tracked);
                if (Settings.DebugLogging)
                    EnqueueLog($"[RimMind-Core] Enqueued request {request.RequestId} (track={trackingId}) for mod {modId}, priority={request.Priority}, queue depth={queue.Count}");
                TryProcessModQueue(modId, CurrentTick);
            }
        }

        public void EnqueueImmediate(AIRequest request, Action<AIResponse> callback, IAIClient client)
        {
            lock (_queueLock)
            {
                if (client.IsLocalEndpoint && _isProcessingLocalRequest)
                {
                    if (Settings.DebugLogging) EnqueueLog($"[RimMind-Core] Immediate request {request.RequestId} deferred: local model busy");
                    Enqueue(request, callback, client);
                    return;
                }
                int trackingId = _nextTrackingId++;
                var tracked = new TrackedRequest
                {
                    TrackingId = trackingId, Request = request, Callback = callback, Client = client,
                    IsLocalEndpointSnapshot = client.IsLocalEndpoint, State = AIRequestState.Processing,
                    EnqueuedAtTick = CurrentTick, StartedProcessingAtTick = CurrentTick, AttemptCount = 1, MaxAttempts = 1,
                };
                _activeRequests[trackingId] = tracked;
                _requestIdToActive[request.RequestId] = tracked;
                if (client.IsLocalEndpoint) _isProcessingLocalRequest = true;
                if (Settings.DebugLogging)
                    EnqueueLog($"[RimMind-Core] Immediate request {request.RequestId} (track={trackingId}) for mod {request.ModId}, bypassing queue");
                FireRequest(tracked);
            }
        }

        private void ProcessAllQueues(int now)
        {
            if (_isPaused) return;
            lock (_queueLock)
            {
                var cooldownSnapshot = _cooldowns.GetSnapshot();
                var readyRequests = new List<(string modId, TrackedRequest tracked)>();
                foreach (var kvp in _modQueues)
                {
                    string modId = kvp.Key; var queue = kvp.Value;
                    if (queue.Count == 0) continue;
                    if (cooldownSnapshot.TryGetValue(modId, out int nextAllowed) && now < nextAllowed) continue;
                    while (queue.Count > 0)
                    {
                        var t = queue[0];
                        if (t.Request.ExpireAtTicks > 0 && now > t.Request.ExpireAtTicks)
                        { queue.RemoveAt(0); if (Settings.DebugLogging) EnqueueLog($"[RimMind-Core] Expired request {t.Request.RequestId} skipped"); continue; }
                        break;
                    }
                    if (queue.Count > 0) readyRequests.Add((modId, queue[0]));
                }
                readyRequests.Sort((a, b) => { int p = (int)a.tracked.Request.Priority - (int)b.tracked.Request.Priority; if (p != 0) return p; return a.tracked.EnqueuedAtTick - b.tracked.EnqueuedAtTick; });
                int maxConcurrent = Settings.MaxConcurrentRequests;
                foreach (var (modId, tracked) in readyRequests)
                {
                    if (_activeRequests.Count >= maxConcurrent) break;
                    if (tracked.IsLocalEndpointSnapshot && _isProcessingLocalRequest) continue;
                    if (!_modQueues.TryGetValue(modId, out var queue) || queue.Count == 0 || queue[0] != tracked) continue;
                    queue.RemoveAt(0);
                    int cooldownTicks = _cooldowns.GetModCooldownTicks(modId);
                    _cooldowns.Set(modId, now + cooldownTicks);
                    tracked.State = AIRequestState.Processing; tracked.StartedProcessingAtTick = now;
                    _activeRequests[tracked.TrackingId] = tracked; _requestIdToActive[tracked.Request.RequestId] = tracked;
                    if (tracked.IsLocalEndpointSnapshot) _isProcessingLocalRequest = true;
                    if (Settings.DebugLogging)
                        EnqueueLog($"[RimMind-Core] Processing request {tracked.Request.RequestId} (track={tracked.TrackingId}) for mod {modId}, priority={tracked.Request.Priority}, cooldown={cooldownTicks}t, active={_activeRequests.Count}/{maxConcurrent}");
                    FireRequest(tracked);
                }
            }
        }

        private void TryProcessModQueue(string modId, int now)
        {
            if (_isPaused) return;
            if (_cooldowns.IsOnCooldown(modId, now)) return;
            lock (_queueLock) { if (!_modQueues.TryGetValue(modId, out var q) || q.Count == 0) return; }
            ProcessAllQueues(now);
        }

        private void FireRequest(TrackedRequest tracked)
        {
            var ct = _cts.Token;
            Task.Run(async () =>
            {
                AIResponse response;
                try
                {
                    ct.ThrowIfCancellationRequested();
                    if (_executeViaPipeline != null)
                    {
                        response = _executeViaPipeline(tracked.Request, tracked.Client);
                    }
                    else
                    {
                        var result = await tracked.Client.SendAsync(tracked.Request);
                        response = result.Match<AIResponse>(ok => ok, err => AIResponse.Ok(tracked.Request.RequestId, "", 0));
                    }
                }
                catch (OperationCanceledException) { response = AIResponse.Ok(tracked.Request.RequestId, "", 0); }
                catch (Exception ex) { LogSink?.LogFromBackground($"[RimMind-Core] Execute threw for {tracked.Request.RequestId}: {ex.Message}", isWarning: true); response = AIResponse.Ok(tracked.Request.RequestId, "", 0); }
                long queueWaitMs = (tracked.StartedProcessingAtTick > 0 && tracked.EnqueuedAtTick > 0) ? (tracked.StartedProcessingAtTick - tracked.EnqueuedAtTick) * 16L : 0;
                response.QueueWaitMs = queueWaitMs; response.Priority = tracked.Request.Priority;
                lock (_queueLock)
                {
                    _activeRequests.TryRemove(tracked.TrackingId, out _);
                    _requestIdToActive.TryRemove(tracked.Request.RequestId, out _);
                    if (tracked.IsLocalEndpointSnapshot) _isProcessingLocalRequest = false;
                }
                _results.Enqueue((response, tracked.Callback));
            }, ct);
        }

        private void CheckActiveRequestTimeouts()
        {
            lock (_queueLock)
            {
                if (_activeRequests.Count == 0) return;
                int now = CurrentTick; int timeoutMs = Settings.RequestTimeoutMs; int timeoutTicks = timeoutMs / 16;
                var timedOut = new List<TrackedRequest>();
                foreach (var kvp in _activeRequests) { if (kvp.Value.StartedProcessingAtTick > 0 && now - kvp.Value.StartedProcessingAtTick > timeoutTicks) timedOut.Add(kvp.Value); }
                foreach (var tracked in timedOut)
                {
                    _activeRequests.TryRemove(tracked.TrackingId, out _); _requestIdToActive.TryRemove(tracked.Request.RequestId, out _);
                    if (tracked.IsLocalEndpointSnapshot) _isProcessingLocalRequest = false;
                    var response = AIResponse.Ok(tracked.Request.RequestId, "", 0);
                    response.Priority = tracked.Request.Priority;
                    _results.Enqueue((response, tracked.Callback));
                    if (Settings.DebugLogging) EnqueueLog($"[RimMind-Core] Request {tracked.Request.RequestId} timed out after {timeoutTicks} ticks");
                }
            }
        }

        public bool CancelRequest(string requestId)
        {
            lock (_queueLock)
            {
                if (_requestIdToActive.TryGetValue(requestId, out var active))
                {
                    active.State = AIRequestState.Cancelled; _activeRequests.TryRemove(active.TrackingId, out _); _requestIdToActive.TryRemove(requestId, out _);
                    if (active.IsLocalEndpointSnapshot) _isProcessingLocalRequest = false;
                    var response = AIResponse.Ok(requestId, "", 0); response.Priority = active.Request.Priority;
                    _results.Enqueue((response, active.Callback)); return true;
                }
                foreach (var kvp in _modQueues)
                {
                    int idx = kvp.Value.FindIndex(t => t.Request.RequestId == requestId);
                    if (idx >= 0) { var tracked = kvp.Value[idx]; kvp.Value.RemoveAt(idx); var response = AIResponse.Ok(requestId, "", 0); response.Priority = tracked.Request.Priority; _results.Enqueue((response, tracked.Callback)); return true; }
                }
                return false;
            }
        }

        public void PauseQueue() => _isPaused = true;
        public void ResumeQueue() => _isPaused = false;
        public bool IsPaused => _isPaused;
        public int ActiveRequestCount => _activeRequests.Count;
        public bool IsLocalModelBusy => _isProcessingLocalRequest;
        public IReadOnlyList<TrackedRequest> GetActiveRequests() { lock (_queueLock) { return _activeRequests.Values.ToList(); } }
        public int GetCooldownTicksLeft(string modId) => _cooldowns.GetCooldownTicksLeft(modId, CurrentTick);
        public int GetQueueDepth(string modId) { lock (_queueLock) { return _modQueues.TryGetValue(modId, out var q) ? q.Count : 0; } }
        public void ClearCooldown(string modId) => _cooldowns.Clear(modId);
        public void ClearAllCooldowns() => _cooldowns.ClearAll();
        public void ClearAllQueues() { lock (_queueLock) { foreach (var kvp in _modQueues) kvp.Value.Clear(); _modQueues.Clear(); } }
        public IReadOnlyDictionary<string, int> GetAllCooldowns() => _cooldowns.GetAll();
        public IReadOnlyDictionary<string, int> GetAllQueueDepths() { lock (_queueLock) { var r = new Dictionary<string, int>(); foreach (var kvp in _modQueues) r[kvp.Key] = kvp.Value.Count; return r; } }
        public IReadOnlyList<TrackedRequest> GetQueuedRequests(string modId) { lock (_queueLock) { return _modQueues.TryGetValue(modId, out var q) ? q.ToList() : new List<TrackedRequest>(); } }
        public IReadOnlyList<TrackedRequest> GetAllQueuedRequests() { lock (_queueLock) { var r = new List<TrackedRequest>(); foreach (var kvp in _modQueues) r.AddRange(kvp.Value); return r; } }
        public int TotalQueuedCount { get { lock (_queueLock) { return _modQueues.Values.Sum(q => q.Count); } } }
        public void EnqueueLog(string msg, bool isWarning = false) => _pendingLogs.Enqueue((msg, isWarning));
        internal CancellationTokenSource GetCts() => _cts;

        private sealed class DefaultSettingsProvider : ISettingsProvider
        {
            public int QueueProcessInterval { get => 60; set { } }
            public int MaxConcurrentRequests { get => 4; set { } }
            public int RequestTimeoutMs { get => 30000; set { } }
            public int MaxRetryCount { get => 2; set { } }
            public int RequestExpireTicks { get => 30000; set { } }
            public int AgentTickInterval => 150;
            public int BehaviorHistoryMax { get => 100; set { } }
            public int ThinkCooldownTicks => 30000;
            public int MaxToolCallDepth => 3;
            public int DefaultModCooldownTicks { get => 3600; set { } }
            public int MaxTokens { get => 800; set { } }
            public float DefaultTemperature { get => 0.7f; set { } }
            public bool ForceJsonMode { get => true; set { } }
            public string ModelName { get => ""; set { } }
            public string Provider { get => ProviderHelper.GetDefaultProviderId(); set { } }
            public string ApiKey { get => ""; set { } }
            public string ApiEndpoint { get => ""; set { } }
            public string Player2RemoteUrl { get => ""; set { } }
            public bool DebugLogging { get => false; set { } }
            public int CircuitBreakerFailureThreshold => 5;
            public int CircuitBreakerOpenDurationSec => 60;
            public int ContextCalibrateInterval { get => 10000; set { } }
            public int ContextDiffLifetimeTicks { get => 36000; set { } }
            public bool IsConfigured => false;
            public IContextSettings Context => new DefaultContextSettings();
            public bool RequestOverlayEnabled { get => true; set { } }
            public float RequestOverlayX { get => 20f; set { } }
            public float RequestOverlayY { get => 20f; set { } }
            public float RequestOverlayW { get => 300f; set { } }
            public float RequestOverlayH { get => 200f; set { } }
            public string CustomPawnPrompt { get => ""; set { } }
            public string CustomMapPrompt { get => ""; set { } }
            public Domain.Enums.FlywheelAutoApplyMode AutoApplyMode { get => Domain.Enums.FlywheelAutoApplyMode.Off; set { } }
            public float AutoApplyConfidenceThreshold { get => 0.8f; set { } }
            public bool IsOpenAIConfigured() => false;

            bool IContextIncludeSettings.IncludeRace { get => Context.IncludeRace; set => Context.IncludeRace = value; }
            bool IContextIncludeSettings.IncludeAge { get => Context.IncludeAge; set => Context.IncludeAge = value; }
            bool IContextIncludeSettings.IncludeGender { get => Context.IncludeGender; set => Context.IncludeGender = value; }
            bool IContextIncludeSettings.IncludeBackstory { get => Context.IncludeBackstory; set => Context.IncludeBackstory = value; }
            bool IContextIncludeSettings.IncludeIdeology { get => Context.IncludeIdeology; set => Context.IncludeIdeology = value; }
            bool IContextIncludeSettings.IncludeTraits { get => Context.IncludeTraits; set => Context.IncludeTraits = value; }
            bool IContextIncludeSettings.IncludeSkills { get => Context.IncludeSkills; set => Context.IncludeSkills = value; }
            int IContextIncludeSettings.MinSkillLevel { get => Context.MinSkillLevel; set => Context.MinSkillLevel = value; }
            bool IContextIncludeSettings.IncludeHealth { get => Context.IncludeHealth; set => Context.IncludeHealth = value; }
            bool IContextIncludeSettings.IncludeCapacities { get => Context.IncludeCapacities; set => Context.IncludeCapacities = value; }
            bool IContextIncludeSettings.IncludeMood { get => Context.IncludeMood; set => Context.IncludeMood = value; }
            bool IContextIncludeSettings.IncludeMoodThoughts { get => Context.IncludeMoodThoughts; set => Context.IncludeMoodThoughts = value; }
            bool IContextIncludeSettings.IncludeCurrentJob { get => Context.IncludeCurrentJob; set => Context.IncludeCurrentJob = value; }
            bool IContextIncludeSettings.IncludeWorkPriorities { get => Context.IncludeWorkPriorities; set => Context.IncludeWorkPriorities = value; }
            bool IContextIncludeSettings.IncludeEquipment { get => Context.IncludeEquipment; set => Context.IncludeEquipment = value; }
            bool IContextIncludeSettings.IncludeInventory { get => Context.IncludeInventory; set => Context.IncludeInventory = value; }
            bool IContextIncludeSettings.IncludeLocation { get => Context.IncludeLocation; set => Context.IncludeLocation = value; }
            bool IContextIncludeSettings.IncludeRelations { get => Context.IncludeRelations; set => Context.IncludeRelations = value; }
            bool IContextIncludeSettings.IncludeGenes { get => Context.IncludeGenes; set => Context.IncludeGenes = value; }
            bool IContextIncludeSettings.IncludeSurroundings { get => Context.IncludeSurroundings; set => Context.IncludeSurroundings = value; }
            bool IContextIncludeSettings.IncludeCombatStatus { get => Context.IncludeCombatStatus; set => Context.IncludeCombatStatus = value; }
            bool IContextIncludeSettings.IncludeGameTime { get => Context.IncludeGameTime; set => Context.IncludeGameTime = value; }
            bool IContextIncludeSettings.IncludeColonistCount { get => Context.IncludeColonistCount; set => Context.IncludeColonistCount = value; }
            bool IContextIncludeSettings.IncludeColonistNames { get => Context.IncludeColonistNames; set => Context.IncludeColonistNames = value; }
            bool IContextIncludeSettings.IncludeWealth { get => Context.IncludeWealth; set => Context.IncludeWealth = value; }
            bool IContextIncludeSettings.IncludeFood { get => Context.IncludeFood; set => Context.IncludeFood = value; }
            bool IContextIncludeSettings.IncludeSeason { get => Context.IncludeSeason; set => Context.IncludeSeason = value; }
            bool IContextIncludeSettings.IncludeWeather { get => Context.IncludeWeather; set => Context.IncludeWeather = value; }
            bool IContextIncludeSettings.IncludeThreats { get => Context.IncludeThreats; set => Context.IncludeThreats = value; }

            float IContextBudgetSettings.ContextBudget { get => Context.ContextBudget; set => Context.ContextBudget = value; }
            int IContextBudgetSettings.ContextBriefLimit => Context.ContextBriefLimit;
            int IContextBudgetSettings.MaxCacheEntries => Context.MaxCacheEntries;
            float IContextBudgetSettings.BudgetW1 { get => Context.BudgetW1; set => Context.BudgetW1 = value; }
            float IContextBudgetSettings.BudgetW2 { get => Context.BudgetW2; set => Context.BudgetW2 = value; }

            int IContextEnvironmentSettings.EnvironmentScanRadius => Context.EnvironmentScanRadius;
            int IContextEnvironmentSettings.EnvironmentMaxItems => Context.EnvironmentMaxItems;
            float IContextEnvironmentSettings.ThreatThresholdHigh => Context.ThreatThresholdHigh;
            float IContextEnvironmentSettings.ThreatThresholdMedium => Context.ThreatThresholdMedium;
            float IContextEnvironmentSettings.ThreatThresholdLow => Context.ThreatThresholdLow;
            float IContextEnvironmentSettings.MoodDiffThreshold => Context.MoodDiffThreshold;
            float IContextEnvironmentSettings.TemperatureDiffThreshold => Context.TemperatureDiffThreshold;

            void IContextSettings.ApplyPreset(Domain.Enums.ContextPreset preset) => Context.ApplyPreset(preset);
            void IContextSettings.ResetToDefault() => Context.ResetToDefault();
        }

        private sealed class DefaultContextSettings : IContextSettings
        {
            public float ContextBudget { get => 0.6f; set { } }
            public int ContextBriefLimit => 200;
            public int EnvironmentScanRadius => 5;
            public int EnvironmentMaxItems => 8;
            public float ThreatThresholdHigh => 200000f;
            public float ThreatThresholdMedium => 100000f;
            public float ThreatThresholdLow => 50000f;
            public int MaxCacheEntries => 100;
            public float MoodDiffThreshold => 5f;
            public float TemperatureDiffThreshold => 5f;
            public bool IncludeRace { get => true; set { } }
            public bool IncludeAge { get => true; set { } }
            public bool IncludeGender { get => true; set { } }
            public bool IncludeBackstory { get => true; set { } }
            public bool IncludeIdeology { get => false; set { } }
            public bool IncludeTraits { get => true; set { } }
            public bool IncludeSkills { get => true; set { } }
            public int MinSkillLevel { get => 4; set { } }
            public bool IncludeHealth { get => true; set { } }
            public bool IncludeCapacities { get => true; set { } }
            public bool IncludeMood { get => true; set { } }
            public bool IncludeMoodThoughts { get => false; set { } }
            public bool IncludeCurrentJob { get => true; set { } }
            public bool IncludeWorkPriorities { get => true; set { } }
            public bool IncludeEquipment { get => true; set { } }
            public bool IncludeInventory { get => false; set { } }
            public bool IncludeLocation { get => false; set { } }
            public bool IncludeRelations { get => true; set { } }
            public bool IncludeGenes { get => true; set { } }
            public bool IncludeSurroundings { get => false; set { } }
            public bool IncludeCombatStatus { get => true; set { } }
            public bool IncludeGameTime { get => true; set { } }
            public bool IncludeColonistCount { get => true; set { } }
            public bool IncludeColonistNames { get => true; set { } }
            public bool IncludeWealth { get => false; set { } }
            public bool IncludeFood { get => true; set { } }
            public bool IncludeSeason { get => true; set { } }
            public bool IncludeWeather { get => true; set { } }
            public bool IncludeThreats { get => true; set { } }
            public float BudgetW1 { get => 0.4f; set { } }
            public float BudgetW2 { get => 0.6f; set { } }
            public void ApplyPreset(Domain.Enums.ContextPreset preset) { }
            public void ResetToDefault() { }
        }
    }
}
