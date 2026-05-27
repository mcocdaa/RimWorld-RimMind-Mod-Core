using System;
using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Features.Agent.InnerVoice;
using RimMind.Domain.Events;
using RimMind.Tests.Application.Stubs;
using Xunit;

namespace RimMind.Tests.Application.Features.Agent.InnerVoice
{
    public class InnerVoiceHandlerTests
    {
        private readonly StubTickProvider _tick = new() { TicksGame = 10000 };
        private readonly StubAgentBus _bus = new();

        private InnerVoiceHandler CreateHandler(ILogSink? log = null)
            => new(_bus, _tick, log);

        [Fact]
        public void StartListening_SubscribesToInnerVoiceEvent()
        {
            var handler = CreateHandler();
            handler.StartListening();

            Assert.True(_bus.HasSubscription<InnerVoiceEvent>());
        }

        [Fact]
        public void StopListening_Unsubscribes()
        {
            var handler = CreateHandler();
            handler.StartListening();
            handler.StopListening();

            Assert.False(_bus.HasSubscription<InnerVoiceEvent>());
        }

        [Fact]
        public void OnInnerVoiceEvent_EmptyVoiceText_DoesNotStore()
        {
            var handler = CreateHandler();
            handler.StartListening();

            _bus.Publish(new InnerVoiceEvent("npc-1", 0, "", 20000));

            Assert.Null(handler.GetPendingVoiceText("npc-1"));
        }

        [Fact]
        public void OnInnerVoiceEvent_EmptyNpcId_DoesNotStore()
        {
            var handler = CreateHandler();
            handler.StartListening();

            _bus.Publish(new InnerVoiceEvent("", 0, "hello", 20000));

            Assert.Null(handler.GetPendingVoiceText(""));
        }

        [Fact]
        public void OnInnerVoiceEvent_WhitespaceVoiceText_DoesNotStore()
        {
            var handler = CreateHandler();
            handler.StartListening();

            _bus.Publish(new InnerVoiceEvent("npc-1", 0, "   ", 20000));

            Assert.Null(handler.GetPendingVoiceText("npc-1"));
        }

        [Fact]
        public void GetPendingVoiceText_NotExpired_ReturnsText()
        {
            var handler = CreateHandler();
            handler.StartListening();

            _bus.Publish(new InnerVoiceEvent("npc-1", 0, "I feel uneasy", 20000));

            // currentTick = 10000, expiryTick = 20000, so not expired
            Assert.Equal("I feel uneasy", handler.GetPendingVoiceText("npc-1"));
        }

        [Fact]
        public void GetPendingVoiceText_Expired_RemovesAndReturnsNull()
        {
            var handler = CreateHandler();
            handler.StartListening();

            _bus.Publish(new InnerVoiceEvent("npc-1", 0, "I feel uneasy", 8000));

            // currentTick = 10000, expiryTick = 8000, so expired
            Assert.Null(handler.GetPendingVoiceText("npc-1"));

            // Verify it was removed (second call also returns null)
            Assert.Null(handler.GetPendingVoiceText("npc-1"));
        }

        [Fact]
        public void GetPendingVoiceText_NonExistentNpc_ReturnsNull()
        {
            var handler = CreateHandler();
            Assert.Null(handler.GetPendingVoiceText("non-existent"));
        }

        [Fact]
        public void ClearVoice_RemovesPendingVoice()
        {
            var handler = CreateHandler();
            handler.StartListening();

            _bus.Publish(new InnerVoiceEvent("npc-1", 0, "I feel uneasy", 20000));

            // Verify it's there first
            Assert.Equal("I feel uneasy", handler.GetPendingVoiceText("npc-1"));

            handler.ClearVoice("npc-1");

            Assert.Null(handler.GetPendingVoiceText("npc-1"));
        }

        [Fact]
        public void ClearVoice_NonExistentNpc_DoesNotThrow()
        {
            var handler = CreateHandler();
            var ex = Record.Exception(() => handler.ClearVoice("non-existent"));
            Assert.Null(ex);
        }

        [Fact]
        public void Constructor_NullAgentBus_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new InnerVoiceHandler(null!, _tick));
        }

        [Fact]
        public void Constructor_NullTickProvider_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new InnerVoiceHandler(_bus, null!));
        }

        [Fact]
        public void OnInnerVoiceEvent_OverwritesPreviousVoiceForSameNpc()
        {
            var handler = CreateHandler();
            handler.StartListening();

            _bus.Publish(new InnerVoiceEvent("npc-1", 0, "first thought", 20000));
            _bus.Publish(new InnerVoiceEvent("npc-1", 0, "second thought", 25000));

            Assert.Equal("second thought", handler.GetPendingVoiceText("npc-1"));
        }

        [Fact]
        public void OnInnerVoiceEvent_DifferentNpcs_StoreIndependently()
        {
            var handler = CreateHandler();
            handler.StartListening();

            _bus.Publish(new InnerVoiceEvent("npc-1", 0, "thought 1", 20000));
            _bus.Publish(new InnerVoiceEvent("npc-2", 0, "thought 2", 20000));

            Assert.Equal("thought 1", handler.GetPendingVoiceText("npc-1"));
            Assert.Equal("thought 2", handler.GetPendingVoiceText("npc-2"));
        }
    }
}
