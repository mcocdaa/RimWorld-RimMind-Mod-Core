using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Domain.Events.Extension;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Common;
using RimMind.Application.Features.Pipeline;
using Xunit;

namespace RimMind.Tests.Pipeline
{
    public class TestPipelineContext : PipelineContextBase
    {
        public List<string> ExecutionLog { get; } = new List<string>();
    }

    public class TestMiddleware : IMiddleware<TestPipelineContext>
    {
        private readonly string _name;
        private readonly int _order;
        private readonly Action<TestPipelineContext>? _onInvoke;
        private readonly bool _callNext;
        private readonly bool _shortCircuit;
        private readonly bool _throwException;

        public TestMiddleware(
            string name,
            int order = 0,
            Action<TestPipelineContext>? onInvoke = null,
            bool callNext = true,
            bool shortCircuit = false,
            bool throwException = false)
        {
            _name = name;
            _order = order;
            _onInvoke = onInvoke;
            _callNext = callNext;
            _shortCircuit = shortCircuit;
            _throwException = throwException;
        }

        public string Id => _name;
        public string Name => _name;
        public int Order => _order;

        public async Task InvokeAsync(TestPipelineContext context, MiddlewareDelegate<TestPipelineContext> next)
        {
            context.ExecutionLog.Add(_name);

            if (_shortCircuit)
            {
                context.ShortCircuit($"Short-circuited by {_name}");
                return;
            }

            if (_throwException)
            {
                throw new InvalidOperationException($"Exception from {_name}");
            }

            _onInvoke?.Invoke(context);

            if (_callNext)
            {
                await next(context).ConfigureAwait(false);
            }
        }
    }

    public class PipelineCoreTests
    {
        [Fact]
        public async Task Middlewares_ExecuteInRegistrationOrder()
        {
            var middlewares = new IMiddleware<TestPipelineContext>[]
            {
                new TestMiddleware("A"),
                new TestMiddleware("B"),
                new TestMiddleware("C")
            };
            var pipeline = new Pipeline<TestPipelineContext>(middlewares);
            var context = new TestPipelineContext();

            await pipeline.ExecuteAsync(context);

            Assert.Equal(new[] { "A", "B", "C" }, context.ExecutionLog);
        }

        [Fact]
        public async Task ShortCircuit_StopsSubsequentMiddlewares()
        {
            var middlewares = new IMiddleware<TestPipelineContext>[]
            {
                new TestMiddleware("A", shortCircuit: true),
                new TestMiddleware("B"),
                new TestMiddleware("C")
            };
            var pipeline = new Pipeline<TestPipelineContext>(middlewares);
            var context = new TestPipelineContext();

            await pipeline.ExecuteAsync(context);

            Assert.Equal(new[] { "A" }, context.ExecutionLog);
            Assert.True(context.IsShortCircuited);
            Assert.Equal("Short-circuited by A", context.ShortCircuitReason);
        }

        [Fact]
        public async Task Exception_BubblesUp()
        {
            var middlewares = new IMiddleware<TestPipelineContext>[]
            {
                new TestMiddleware("A"),
                new TestMiddleware("B", throwException: true),
                new TestMiddleware("C")
            };
            var pipeline = new Pipeline<TestPipelineContext>(middlewares);
            var context = new TestPipelineContext();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => pipeline.ExecuteAsync(context));

            Assert.Equal("Exception from B", ex.Message);
            Assert.Equal(new[] { "A", "B" }, context.ExecutionLog);
        }

        [Fact]
        public async Task NextCalledZeroTimes_StopsPipeline()
        {
            var middlewares = new IMiddleware<TestPipelineContext>[]
            {
                new TestMiddleware("A", callNext: false),
                new TestMiddleware("B"),
                new TestMiddleware("C")
            };
            var pipeline = new Pipeline<TestPipelineContext>(middlewares);
            var context = new TestPipelineContext();

            await pipeline.ExecuteAsync(context);

            Assert.Equal(new[] { "A" }, context.ExecutionLog);
        }

        [Fact]
        public void TraceId_Is12CharHex()
        {
            var context = new TestPipelineContext();

            Assert.Equal(12, context.TraceId.Length);
            Assert.Matches("^[0-9a-f]{12}$", context.TraceId);
        }

        [Fact]
        public async Task Items_Dictionary_PassesDataBetweenMiddlewares()
        {
            var middlewares = new IMiddleware<TestPipelineContext>[]
            {
                new TestMiddleware("A", onInvoke: ctx => ctx.Items["key"] = "value_from_A"),
                new TestMiddleware("B", onInvoke: ctx =>
                {
                    if (ctx.Items.TryGetValue("key", out var val))
                    {
                        ctx.ExecutionLog.Add($"B_read_{val}");
                    }
                })
            };
            var pipeline = new Pipeline<TestPipelineContext>(middlewares);
            var context = new TestPipelineContext();

            await pipeline.ExecuteAsync(context);

            Assert.Equal("value_from_A", context.Items["key"]);
            Assert.Contains("B_read_value_from_A", context.ExecutionLog);
        }

        [Fact]
        public async Task Pipeline_WithZeroMiddlewares_ExecutesWithoutError()
        {
            var pipeline = new Pipeline<TestPipelineContext>(Array.Empty<IMiddleware<TestPipelineContext>>());
            var context = new TestPipelineContext();

            await pipeline.ExecuteAsync(context);

            Assert.False(context.IsShortCircuited);
            Assert.Empty(context.ExecutionLog);
        }

        [Fact]
        public async Task ShortCircuit_WithCustomReason_PreservesExactReasonText()
        {
            var sc = new CommonShortCircuitMiddleware<TestPipelineContext>(
                _ => "custom_reason_42", "TestSC");
            var pipeline = new Pipeline<TestPipelineContext>(
                new IMiddleware<TestPipelineContext>[] { sc });
            var context = new TestPipelineContext();

            await pipeline.ExecuteAsync(context);

            Assert.True(context.IsShortCircuited);
            Assert.Equal("custom_reason_42", context.ShortCircuitReason);
        }

        [Fact]
        public async Task NestedShortCircuit_DownstreamMiddlewareNeverInvoked()
        {
            int bInvokeCount = 0;
            var middlewares = new IMiddleware<TestPipelineContext>[]
            {
                new TestMiddleware("A", shortCircuit: true),
                new TestMiddleware("B", onInvoke: _ => bInvokeCount++)
            };
            var pipeline = new Pipeline<TestPipelineContext>(middlewares);
            var context = new TestPipelineContext();

            await pipeline.ExecuteAsync(context);

            Assert.Equal(0, bInvokeCount);
            Assert.Equal(new[] { "A" }, context.ExecutionLog);
        }

        [Fact]
        public async Task Exception_InMiddleware_PropagatesWithTypeAndMessage()
        {
            var middlewares = new IMiddleware<TestPipelineContext>[]
            {
                new TestMiddleware("A"),
                new TestMiddleware("B", throwException: true)
            };
            var pipeline = new Pipeline<TestPipelineContext>(middlewares);
            var context = new TestPipelineContext();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => pipeline.ExecuteAsync(context));

            Assert.Equal("Exception from B", ex.Message);
            Assert.Equal(new[] { "A", "B" }, context.ExecutionLog);
        }
    }
}
