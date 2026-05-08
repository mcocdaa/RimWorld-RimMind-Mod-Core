using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Bus;
using RimMind.Kernel.Pipeline;
using Xunit;

namespace RimMind.Tests.Pipeline.Bus
{
    internal sealed class TestBusEvent : AgentBusEvent
    {
        public TestBusEvent(string npcId, int pawnId)
            : base(npcId, pawnId, AgentBusEventType.Lifecycle) { }
    }

    public class BusPublishPipelineTests
    {
        private static BusPublishContext<TestBusEvent> CreateContext(
            params Action<TestBusEvent>[] subscribers)
        {
            var delegateList = new List<Delegate>();
            foreach (var s in subscribers)
                delegateList.Add(s);

            return new BusPublishContext<TestBusEvent>
            {
                Event = new TestBusEvent("NPC-1", 1),
                Subscribers = delegateList,
            };
        }

        [Fact]
        public async Task ErrorIsolation_OneHandlerFails_OthersStillExecute()
        {
            var callLog = new List<string>();

            var context = CreateContext(
                e => { callLog.Add("handler1"); throw new InvalidOperationException("fail"); },
                e => callLog.Add("handler2"),
                e => callLog.Add("handler3")
            );

            var dispatch = new DispatchMiddleware<TestBusEvent>();
            var pipeline = new Pipeline<BusPublishContext<TestBusEvent>>(new IMiddleware<BusPublishContext<TestBusEvent>>[] { dispatch });

            await pipeline.ExecuteAsync(context);

            Assert.Equal(new[] { "handler1", "handler2", "handler3" }, callLog);
        }

        [Fact]
        public async Task ErrorIsolation_ErrorsCollectedInHandlerErrors()
        {
            var context = CreateContext(
                e => throw new InvalidOperationException("error1"),
                e => throw new ArgumentException("error2"),
                e => { }
            );

            var dispatch = new DispatchMiddleware<TestBusEvent>();
            var pipeline = new Pipeline<BusPublishContext<TestBusEvent>>(new IMiddleware<BusPublishContext<TestBusEvent>>[] { dispatch });

            await pipeline.ExecuteAsync(context);

            Assert.Equal(2, context.HandlerErrors.Count);
            Assert.IsType<InvalidOperationException>(context.HandlerErrors[0]);
            Assert.IsType<ArgumentException>(context.HandlerErrors[1]);
        }

        [Fact]
        public async Task Dispatch_CallsAllSubscribers()
        {
            var callCount = 0;

            var context = CreateContext(
                e => callCount++,
                e => callCount++,
                e => callCount++
            );

            var dispatch = new DispatchMiddleware<TestBusEvent>();
            var pipeline = new Pipeline<BusPublishContext<TestBusEvent>>(new IMiddleware<BusPublishContext<TestBusEvent>>[] { dispatch });

            await pipeline.ExecuteAsync(context);

            Assert.Equal(3, callCount);
        }
    }
}
