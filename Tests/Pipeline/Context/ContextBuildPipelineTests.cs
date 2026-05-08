using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.Context;
using RimMind.Kernel.Context;
using RimMind.Kernel.Pipeline;
using Xunit;

namespace RimMind.Tests.Pipeline.Context
{
    internal sealed class ContextBuildTestMiddleware : IMiddleware<ContextBuildContext>
    {
        private readonly string _name;
        private readonly int _order;
        private readonly bool _shortCircuit;

        public ContextBuildTestMiddleware(string name, int order = 0, bool shortCircuit = false)
        {
            _name = name;
            _order = order;
            _shortCircuit = shortCircuit;
        }

        public string Id => _name;
        public string Name => _name;
        public int Order => _order;

        public async Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
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

    public class ContextBuildPipelineTests
    {
        private static ContextBuildContext CreateContext()
        {
            return new ContextBuildContext
            {
                Request = new ContextRequest
                {
                    NpcId = "NPC-1",
                    Scenario = "dialogue",
                },
            };
        }

        [Fact]
        public async Task Pipeline_ExecutesInOrder()
        {
            var middlewares = new IMiddleware<ContextBuildContext>[]
            {
                new ContextBuildTestMiddleware("A", order: 0),
                new ContextBuildTestMiddleware("B", order: 1),
                new ContextBuildTestMiddleware("C", order: 2),
            };

            var pipeline = new Pipeline<ContextBuildContext>(middlewares);
            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            var log = (List<string>)context.Items["log"]!;
            Assert.Equal(new[] { "A", "B", "C" }, log);
        }

        [Fact]
        public async Task ShortCircuit_StopsSubsequentMiddlewares()
        {
            var middlewares = new IMiddleware<ContextBuildContext>[]
            {
                new ContextBuildTestMiddleware("A", order: 0, shortCircuit: true),
                new ContextBuildTestMiddleware("B", order: 1),
                new ContextBuildTestMiddleware("C", order: 2),
            };

            var pipeline = new Pipeline<ContextBuildContext>(middlewares);
            var context = CreateContext();
            context.Items["log"] = new List<string>();

            await pipeline.ExecuteAsync(context);

            var log = (List<string>)context.Items["log"]!;
            Assert.Equal(new[] { "A" }, log);
            Assert.True(context.IsShortCircuited);
        }
    }
}
