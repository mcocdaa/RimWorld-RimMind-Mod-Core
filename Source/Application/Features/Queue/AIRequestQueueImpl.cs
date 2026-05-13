using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Client;

namespace RimMind.Application.Features.Queue
{
    internal sealed class AIRequestQueueImpl : IAIRequestQueue
    {
        private readonly ConcurrentQueue<TrackedRequest> _queue = new ConcurrentQueue<TrackedRequest>();
        private readonly ConcurrentDictionary<string, TrackedRequest> _active = new ConcurrentDictionary<string, TrackedRequest>();
        private readonly ConcurrentDictionary<string, int> _cooldowns = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<string, ConcurrentQueue<TrackedRequest>> _modQueues
            = new ConcurrentDictionary<string, ConcurrentQueue<TrackedRequest>>();
        private readonly ILogSink? _log;
        private readonly IThreadChecker? _threadChecker;
        private readonly ITickProvider? _tickProvider;
        private int _trackingIdCounter;
        private bool _paused;

        public AIRequestQueueImpl(ILogSink? log = null, IThreadChecker? threadChecker = null, ITickProvider? tickProvider = null)
        {
            _log = log;
            _threadChecker = threadChecker;
            _tickProvider = tickProvider;
        }

        public bool IsPaused => _paused;
        public int ActiveRequestCount => _active.Count;
        public bool IsLocalModelBusy => _active.Values.Count > 0;
        public int TotalQueuedCount => _queue.Count;

        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.Any)]
        public void Enqueue(AIRequest request, Action<AIResponse> callback, IAIClient client)
        {
            if (_paused) return;
            var tracked = CreateTrackedRequest(request, callback, client);
            _queue.Enqueue(tracked);
            GetOrCreateModQueue(request.ModId).Enqueue(tracked);
        }

        public void EnqueueImmediate(AIRequest request, Action<AIResponse> callback, IAIClient client)
        {
            var tracked = CreateTrackedRequest(request, callback, client);
            tracked.Request.Priority = AIRequestPriority.Critical;
            _queue.Enqueue(tracked);
        }

        public bool CancelRequest(string requestId)
        {
            return _active.TryRemove(requestId, out _);
        }

        public void CancelAllRequests()
        {
            while (_queue.TryDequeue(out _)) { }
            _active.Clear();
        }

        public void PauseQueue() => _paused = true;
        public void ResumeQueue() => _paused = false;

        public IReadOnlyList<TrackedRequest> GetActiveRequests()
        {
            return new List<TrackedRequest>(_active.Values);
        }

        public IReadOnlyList<TrackedRequest> GetAllQueuedRequests()
        {
            var result = new List<TrackedRequest>();
            foreach (var r in _queue) result.Add(r);
            return result;
        }

        public IReadOnlyList<TrackedRequest> GetQueuedRequests(string modId)
        {
            var result = new List<TrackedRequest>();
            if (_modQueues.TryGetValue(modId, out var q))
            {
                foreach (var r in q) result.Add(r);
            }
            return result;
        }

        public int GetCooldownTicksLeft(string modId)
        {
            return _cooldowns.TryGetValue(modId, out var ticks) ? ticks : 0;
        }

        public int GetQueueDepth(string modId)
        {
            return _modQueues.TryGetValue(modId, out var q) ? q.Count : 0;
        }

        public void ClearCooldown(string modId) => _cooldowns.TryRemove(modId, out _);
        public void ClearAllCooldowns() => _cooldowns.Clear();
        public void ClearAllQueues() { while (_queue.TryDequeue(out _)) { } _modQueues.Clear(); }

        public IReadOnlyDictionary<string, int> GetAllCooldowns() => new Dictionary<string, int>(_cooldowns);
        public IReadOnlyDictionary<string, int> GetAllQueueDepths()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in _modQueues)
                result[kvp.Key] = kvp.Value.Count;
            return result;
        }

        public void EnqueueLog(string msg, bool isWarning = false)
        {
            if (isWarning) _log?.Warning(msg);
            else _log?.Message(msg);
        }

        private TrackedRequest CreateTrackedRequest(AIRequest request, Action<AIResponse> callback, IAIClient client)
        {
            return new TrackedRequest
            {
                TrackingId = Interlocked.Increment(ref _trackingIdCounter),
                Request = request,
                Callback = callback,
                Client = client,
                IsLocalEndpointSnapshot = client.IsLocalEndpoint,
                State = AIRequestState.Queued,
                EnqueuedAtTick = _tickProvider?.TicksGame ?? 0,
                AttemptCount = 0,
                MaxAttempts = request.MaxRetryCount ?? 3
            };
        }

        private ConcurrentQueue<TrackedRequest> GetOrCreateModQueue(string modId)
        {
            return _modQueues.GetOrAdd(modId, _ => new ConcurrentQueue<TrackedRequest>());
        }
    }
}
