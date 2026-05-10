using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Npc;
using RimMind.Kernel.Pipeline.Npc;
using RimMind.Core.Runtime;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Kernel.Pipeline;
using Xunit;

namespace RimMind.Tests.Pipeline.Npc
{
    internal sealed class NpcChatTestMiddleware : IMiddleware<NpcChatContext>
    {
        private readonly string _name;
        private readonly int _order;
        private readonly bool _shortCircuit;

        public NpcChatTestMiddleware(string name, int order = 0, bool shortCircuit = false)
        {
            _name = name;
            _order = order;
            _shortCircuit = shortCircuit;
        }

        public string Id => _name;
        public string Name => _name;
        public int Order => _order;

        public async Task InvokeAsync(NpcChatContext context, MiddlewareDelegate<NpcChatContext> next)
        {
            ((List<string>)context.Items["log"]!).Add(_name);

            if (_shortCircuit)
            {
                context.ShortCircuit($"short_circuited_by_{_name}");
                return;
            }

            await next(context);
        }
    }

    public class NpcChatPipelineTests
    {
        private static NpcChatContext CreateContext()
        {
            return new NpcChatContext
            {
                Request = new ContextRequest
                {
                    NpcId = "NPC-1",
                    Scenario = "dialogue",
                },
                Ct = CancellationToken.None,
            };
        }

        [Fact]
        public async Task ShortCircuit_WhenShutdown()
        {
            RimMindRuntime.Initialize();
            try
            {
                RimMindRuntime.Instance.IsShutdown = true;

                var middleware = new NpcChatShortCircuitMiddleware();
                var context = CreateContext();
                var pipeline = new Pipeline<NpcChatContext>(new IMiddleware<NpcChatContext>[] { middleware });

                await pipeline.ExecuteAsync(context);

                Assert.True(context.IsShortCircuited);
                Assert.Equal("shutdown", context.ShortCircuitReason);
                Assert.NotNull(context.Result);
                Assert.NotNull(context.Result.Error);
            }
            finally
            {
                RimMindRuntime.Instance.IsShutdown = false;
                RimMindRuntime.Instance.Dispose();
            }
        }

        [Fact]
        public async Task Pipeline_ExecutesInOrder()
        {
            var middlewares = new IMiddleware<NpcChatContext>[]
            {
                new NpcChatTestMiddleware("A", order: 0),
                new NpcChatTestMiddleware("B", order: 1),
                new NpcChatTestMiddleware("C", order: 2),
            };

            var pipeline = new Pipeline<NpcChatContext>(middlewares);
            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            var log = (List<string>)context.Items["log"]!;
            Assert.Equal(new[] { "A", "B", "C" }, log);
        }

        [Fact]
        public async Task ShortCircuit_PreventsDownstreamMiddlewareExecution()
        {
            var middlewares = new IMiddleware<NpcChatContext>[]
            {
                new NpcChatTestMiddleware("A", order: 0, shortCircuit: true),
                new NpcChatTestMiddleware("B", order: 1),
                new NpcChatTestMiddleware("C", order: 2),
            };

            var pipeline = new Pipeline<NpcChatContext>(middlewares);
            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            var log = (List<string>)context.Items["log"]!;
            Assert.Equal(new[] { "A" }, log);
            Assert.True(context.IsShortCircuited);
            Assert.Equal("short_circuited_by_A", context.ShortCircuitReason);
        }

        [Fact]
        public async Task StreamingContextFlag_IsPreservedThroughPipeline()
        {
            var middlewares = new IMiddleware<NpcChatContext>[]
            {
                new NpcChatTestMiddleware("A", order: 0),
                new NpcChatTestMiddleware("B", order: 1),
            };

            var pipeline = new Pipeline<NpcChatContext>(middlewares);
            var context = CreateContext();
            context.Items["log"] = new List<string>();
            context.IsStreaming = true;

            await pipeline.ExecuteAsync(context);

            Assert.True(context.IsStreaming);
        }
    }
}
