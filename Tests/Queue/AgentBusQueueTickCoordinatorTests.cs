using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Queue;
using RimMind.Domain.Events;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Presentation.Tests.Queue
{
    public sealed class AgentBusQueueTickCoordinatorTests
    {
        [Fact]
        public void Tick_SetsCurrentTickThenFlushesBusBeforeTickingQueue()
        {
            var events = new List<string>();
            var bus = new RecordingAgentBus(events);
            var queue = new RecordingQueue(events);
            var coordinator = new AgentBusQueueTickCoordinator(bus, queue);

            coordinator.Tick(42);

            Assert.Equal(42, queue.CurrentTick);
            Assert.Equal(new[] { "bus.flush", "queue.tick" }, events);
        }

        [Fact]
        public void Tick_WhenBusFlushThrows_DoesNotTickQueue()
        {
            var events = new List<string>();
            var bus = new RecordingAgentBus(events) { FlushException = new InvalidOperationException("flush failed") };
            var queue = new RecordingQueue(events);
            var coordinator = new AgentBusQueueTickCoordinator(bus, queue);

            var exception = Assert.Throws<InvalidOperationException>(() => coordinator.Tick(42));

            Assert.Equal("flush failed", exception.Message);
            Assert.Equal(42, queue.CurrentTick);
            Assert.Equal(new[] { "bus.flush" }, events);
        }

        private sealed class RecordingAgentBus : IAgentBus
        {
            private readonly List<string> _events;
            public RecordingAgentBus(List<string> events) => _events = events;
            public Exception? FlushException { get; set; }
            public event Action? SubscribersCleared;
            public Action<AgentBusEvent>? DispatchAction => null;
            public void FlushBackgroundQueue() { _events.Add("bus.flush"); if (FlushException != null) throw FlushException; }
            public void Publish<T>(T evt) where T : AgentBusEvent { }
            public void PublishFromBackground<T>(T evt) where T : AgentBusEvent { }
            public string Subscribe<T>(Action<T> handler) where T : AgentBusEvent => "subscription";
            public void Subscribe<T>(string key, Action<T> handler) where T : AgentBusEvent { }
            public void Unsubscribe<T>(string key) where T : AgentBusEvent { }
            public void Unsubscribe<T>(Action<T> handler) where T : AgentBusEvent { }
            public string SubscribeByName(string eventTypeName, Action<AgentBusEvent> handler) => "subscription";
            public void SetPipeline(IPipeline<BusPublishContext> pipeline) { }
            public void ClearAllSubscribers() => SubscribersCleared?.Invoke();
            public int GetHandlerCount() => 0;
            public int GetBackgroundQueueCount() => 0;
            public void RegisterEventType(string name, Type eventType) { }
        }

        private sealed class RecordingQueue : IAIRequestQueueTickable
        {
            private readonly List<string> _events;
            public RecordingQueue(List<string> events) => _events = events;
            public int CurrentTick { get; set; }
            public Action<string, bool>? LogHandler { get; set; }
            public bool IsPaused => false;
            public int ActiveRequestCount => 0;
            public bool IsLocalModelBusy => false;
            public int TotalQueuedCount => 0;
            public void Tick() => _events.Add("queue.tick");
            public void Reset() { }
            public void Enqueue(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> callback, IAIClient client) { }
            public void Enqueue(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> callback, Func<CancellationToken, Task<Result<LlmResponse, RimMindError>>> executor, bool isLocalEndpoint = false) { }
            public void EnqueueImmediate(LlmRequestEnvelope envelope, Action<Result<LlmResponse, RimMindError>> callback, IAIClient client) { }
            public bool CancelRequest(string requestId) => false;
            public void CancelAllRequests() { }
            public void PauseQueue() { }
            public void ResumeQueue() { }
            public IReadOnlyList<TrackedRequest> GetActiveRequests() => Array.Empty<TrackedRequest>();
            public IReadOnlyList<TrackedRequest> GetAllQueuedRequests() => Array.Empty<TrackedRequest>();
            public IReadOnlyList<TrackedRequest> GetQueuedRequests(string modId) => Array.Empty<TrackedRequest>();
            public int GetCooldownTicksLeft(string modId) => 0;
            public int GetQueueDepth(string modId) => 0;
            public void ClearCooldown(string modId) { }
            public void ClearAllCooldowns() { }
            public void ClearAllQueues() { }
            public IReadOnlyDictionary<string, int> GetAllCooldowns() => new Dictionary<string, int>();
            public IReadOnlyDictionary<string, int> GetAllQueueDepths() => new Dictionary<string, int>();
            public void EnqueueLog(string msg, bool isWarning = false) { }
        }
    }
}
