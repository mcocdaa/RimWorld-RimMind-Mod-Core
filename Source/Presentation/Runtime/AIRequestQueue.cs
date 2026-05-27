using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using RimMind.Application.Features.Queue;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using DomainAIRequestPriority = RimMind.Domain.Llm.AIRequestPriority;

namespace RimMind.Presentation.Runtime
{
    public class AIRequestQueue
    {
        private readonly List<QueuedRequest> _queue = new List<QueuedRequest>();
        private readonly List<TrackedRequest> _activeRequests = new List<TrackedRequest>();
        private readonly CooldownTable _cooldowns = new CooldownTable();
        private int _activeCount;
        private int _maxConcurrent;
        private bool _paused;
        private int _processInterval;
        private int _lastProcessTick;

        public int ActiveRequestCount => _activeCount;
        public int TotalQueuedCount => _queue.Count;
        public bool IsPaused => _paused;

        public AIRequestQueue(IQueueSettings? queueSettings = null)
        {
            _maxConcurrent = queueSettings?.MaxConcurrentRequests ?? 3;
            _processInterval = queueSettings?.QueueProcessInterval ?? RimMindDefaults.QueueProcessInterval;
        }

        public void Enqueue(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> onComplete, IAIClient client)
        {
            if (envelope == null) return;
            _queue.Add(new QueuedRequest(envelope, onComplete, client));
        }

        public void EnqueueImmediate(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> onComplete, IAIClient client)
        {
            if (envelope == null) return;
            var immediateEnvelope = new LlmRequestEnvelope
            {
                RequestId = envelope.RequestId,
                TraceId = envelope.TraceId,
                ScenarioId = envelope.ScenarioId,
                ModId = envelope.ModId,
                Messages = envelope.Messages,
                JsonSchema = envelope.JsonSchema,
                Tools = envelope.Tools,
                MaxTokens = envelope.MaxTokens,
                Temperature = envelope.Temperature,
                Priority = DomainAIRequestPriority.Immediate,
                ExpireAtTicks = envelope.ExpireAtTicks,
                MaxRetryCount = envelope.MaxRetryCount,
                IsStreaming = envelope.IsStreaming,
                OnStreamChunk = envelope.OnStreamChunk,
                Ct = envelope.Ct,
                NpcId = envelope.NpcId,
                GameStateInfo = envelope.GameStateInfo,
            };
            _queue.Insert(0, new QueuedRequest(immediateEnvelope, onComplete, client));
        }

        public void PauseQueue() => _paused = true;
        public void ResumeQueue() => _paused = false;

        public IReadOnlyList<TrackedRequest> GetActiveRequests() => _activeRequests;
        public IReadOnlyList<TrackedRequest> GetAllQueuedRequests()
        {
            var result = new List<TrackedRequest>();
            foreach (var qr in _queue)
                result.Add(new TrackedRequest(qr.Envelope.RequestId, qr.Envelope.ModId, qr.Envelope.Priority, "Queued"));
            return result;
        }

        public void ClearCooldown(string modId)
        {
            _cooldowns.Clear(modId);
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
            _activeRequests.Add(new TrackedRequest(item.Envelope.RequestId, item.Envelope.ModId, item.Envelope.Priority, "Active"));
            item.Client.SendAsync(item.Envelope).ContinueWith(t =>
            {
                _activeCount--;
                _activeRequests.RemoveAll(r => r.RequestId == item.Envelope.RequestId);
                if (t.IsCompletedSuccessfully && t.Result.IsOk)
                {
                    item.OnComplete(t.Result);
                }
                else
                {
                    var errResult = t.Result.IsErr
                        ? t.Result
                        : Result<LlmResponse, RimMindError>.Err(
                            new RimMindError(RimMindErrorCode.InternalError, "Unknown error"));
                    item.OnComplete(errResult);
                }
            });
        }

        private class QueuedRequest
        {
            public LlmRequestEnvelope Envelope;
            public Action<Result<LlmResponse, RimMindError>> OnComplete;
            public IAIClient Client;

            public QueuedRequest(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> onComplete, IAIClient client)
            {
                Envelope = envelope;
                OnComplete = onComplete;
                Client = client;
            }
        }
    }
}
