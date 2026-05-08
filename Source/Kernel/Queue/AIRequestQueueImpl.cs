using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Client;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Settings;
using RimMind.Contracts.Runtime;

namespace RimMind.Kernel.Queue
{
    public class AIRequestQueueImpl : IAIRequestQueue
    {
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

        private Func<AIRequest, IAIClient, AIResponse>? _executeViaPipeline;

        private int _lastQueueProcessTick;
        private int _nextTrackingId;
        private bool _isPaused;
        private bool _isProcessingLocalRequest;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public int CurrentTick { get; set; }
        public Action<string, bool>? LogHandler { get; set; }
        public Action? FlushBackgroundQueue { get; set; }

        private int QueueProcessInterval => RimMindModAccessor.Settings?.queueProcessInterval ?? 60;

        public static IAIRequestQueue Instance
        {
            get => RimMindServiceLocator.Get<IAIRequestQueue>()
                ?? throw new InvalidOperationException("AIRequestQueue has not been initialized.");
        }

        public static void LogFromBackground(string msg, bool isWarning = false)
            => RimMindServiceLocator.Get<IAIRequestQueue>()?.EnqueueLog(msg, isWarning);

        public AIRequestQueueImpl()
        {
            RimMindServiceLocator.Register<IAIRequestQueue>(this);
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
                    var response = AIResponse.Cancelled(kvp.Value.Request.RequestId, "Reset, request cancelled");
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
                if (RimMindModAccessor.Settings?.debugLogging == true)
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
                    if (RimMindModAccessor.Settings?.debugLogging == true) EnqueueLog($"[RimMind-Core] Immediate request {request.RequestId} deferred: local model busy");
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
                if (RimMindModAccessor.Settings?.debugLogging == true)
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
                        { queue.RemoveAt(0); if (RimMindModAccessor.Settings?.debugLogging == true) EnqueueLog($"[RimMind-Core] Expired request {t.Request.RequestId} skipped"); continue; }
                        break;
                    }
                    if (queue.Count > 0) readyRequests.Add((modId, queue[0]));
                }
                readyRequests.Sort((a, b) => { int p = (int)a.tracked.Request.Priority - (int)b.tracked.Request.Priority; if (p != 0) return p; return a.tracked.EnqueuedAtTick - b.tracked.EnqueuedAtTick; });
                int maxConcurrent = RimMindModAccessor.Settings?.maxConcurrentRequests ?? 4;
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
                    if (RimMindModAccessor.Settings?.debugLogging == true)
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
                        response = await tracked.Client.SendAsync(tracked.Request);
                    }
                }
                catch (OperationCanceledException) { response = AIResponse.Cancelled(tracked.Request.RequestId, "Request cancelled"); }
                catch (Exception ex) { AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Execute threw for {tracked.Request.RequestId}: {ex.Message}", isWarning: true); response = AIResponse.Failure(tracked.Request.RequestId, ex.Message); }
                long queueWaitMs = (tracked.StartedProcessingAtTick > 0 && tracked.EnqueuedAtTick > 0) ? (tracked.StartedProcessingAtTick - tracked.EnqueuedAtTick) * 16L : 0;
                response.QueueWaitMs = queueWaitMs; response.Priority = tracked.Request.Priority;
                if (!response.Success && QuotaExceededException.IsQuotaError(response.Error))
                    AIRequestQueueImpl.LogFromBackground($"[RimMind-Core] Player2 quota exceeded for request {tracked.Request.RequestId}. Please top up your Joules balance or switch to another provider.", isWarning: true);
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
                int now = CurrentTick; int timeoutMs = RimMindModAccessor.Settings?.requestTimeoutMs ?? 30000; int timeoutTicks = timeoutMs / 16;
                var timedOut = new List<TrackedRequest>();
                foreach (var kvp in _activeRequests) { if (kvp.Value.StartedProcessingAtTick > 0 && now - kvp.Value.StartedProcessingAtTick > timeoutTicks) timedOut.Add(kvp.Value); }
                foreach (var tracked in timedOut)
                {
                    _activeRequests.TryRemove(tracked.TrackingId, out _); _requestIdToActive.TryRemove(tracked.Request.RequestId, out _);
                    if (tracked.IsLocalEndpointSnapshot) _isProcessingLocalRequest = false;
                    var response = AIResponse.Failure(tracked.Request.RequestId, $"Request timed out after {timeoutMs}ms");
                    response.Priority = tracked.Request.Priority;
                    _results.Enqueue((response, tracked.Callback));
                    if (RimMindModAccessor.Settings?.debugLogging == true) EnqueueLog($"[RimMind-Core] Request {tracked.Request.RequestId} timed out after {timeoutTicks} ticks");
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
                    var response = AIResponse.Cancelled(requestId, "Cancelled by user"); response.Priority = active.Request.Priority;
                    _results.Enqueue((response, active.Callback)); return true;
                }
                foreach (var kvp in _modQueues)
                {
                    int idx = kvp.Value.FindIndex(t => t.Request.RequestId == requestId);
                    if (idx >= 0) { var tracked = kvp.Value[idx]; kvp.Value.RemoveAt(idx); var response = AIResponse.Cancelled(requestId, "Cancelled by user"); response.Priority = tracked.Request.Priority; _results.Enqueue((response, tracked.Callback)); return true; }
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
    }
}
