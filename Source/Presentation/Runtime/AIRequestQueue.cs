using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Runtime
{
    public class AIRequestQueue
    {
        private readonly List<QueuedRequest> _queue = new List<QueuedRequest>();
        private readonly List<TrackedRequest> _activeRequests = new List<TrackedRequest>();
        private int _activeCount;
        private int _maxConcurrent;
        private bool _paused;
        private int _processInterval;
        private int _lastProcessTick;

        public int ActiveRequestCount => _activeCount;
        public int TotalQueuedCount => _queue.Count;
        public bool IsPaused => _paused;

        public AIRequestQueue()
        {
            _maxConcurrent = RimMindServiceLocator.Get<IQueueSettings>()?.MaxConcurrentRequests ?? 3;
            _processInterval = RimMindServiceLocator.Get<IQueueSettings>()?.QueueProcessInterval ?? 60;
        }

        public void Enqueue(AIRequest request, Action<AIResponse> onComplete, IAIClient client)
        {
            if (request == null) return;
            _queue.Add(new QueuedRequest(request, onComplete, client));
        }

        public void EnqueueImmediate(AIRequest request, Action<AIResponse> onComplete, IAIClient client)
        {
            if (request == null) return;
            request.Priority = AIRequestPriority.Immediate;
            _queue.Insert(0, new QueuedRequest(request, onComplete, client));
        }

        public void PauseQueue() => _paused = true;
        public void ResumeQueue() => _paused = false;

        public IReadOnlyList<TrackedRequest> GetActiveRequests() => _activeRequests;
        public IReadOnlyList<TrackedRequest> GetAllQueuedRequests()
        {
            var result = new List<TrackedRequest>();
            foreach (var qr in _queue)
                result.Add(new TrackedRequest(qr.Request.RequestId, qr.Request.ModId, qr.Request.Priority, "Queued"));
            return result;
        }

        public void ClearCooldown(string modId)
        {
        }

        public void Tick()
        {
            if (_paused) return;
            int now = 0;
            try { now = Verse.Find.TickManager.TicksGame; } catch { return; }
            if (now - _lastProcessTick < _processInterval) return;
            _lastProcessTick = now;

            while (_activeCount < _maxConcurrent && _queue.Count > 0)
            {
                var item = _queue[0];
                _queue.RemoveAt(0);
                _activeCount++;
                ProcessRequest(item);
            }
        }

        private void ProcessRequest(QueuedRequest item)
        {
            _activeRequests.Add(new TrackedRequest(item.Request.RequestId, item.Request.ModId, item.Request.Priority, "Active"));
            item.Client.SendAsync(item.Request).ContinueWith(t =>
            {
                _activeCount--;
                _activeRequests.RemoveAll(r => r.RequestId == item.Request.RequestId);
                if (t.IsCompletedSuccessfully && t.Result.IsOk && t.Result.Value != null)
                    item.OnComplete(t.Result.Value);
                else
                    item.OnComplete(AIResponse.Ok(item.Request.RequestId, "", 0));
            });
        }

        private class QueuedRequest
        {
            public AIRequest Request;
            public Action<AIResponse> OnComplete;
            public IAIClient Client;

            public QueuedRequest(AIRequest request, Action<AIResponse> onComplete, IAIClient client)
            {
                Request = request;
                OnComplete = onComplete;
                Client = client;
            }
        }
    }
}
