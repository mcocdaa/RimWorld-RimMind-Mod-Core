using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Features.Pipeline.Unified;
using RimMind.Domain.Llm;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    public class RequestSanitizeMiddlewareTests
    {
        [Fact]
        public async Task NullEnvelope_ShortCircuits()
        {
            var middleware = new RequestSanitizeMiddleware();
            var context = new LlmRequestContext(); // Envelope is null!

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.True(context.IsShortCircuited);
            Assert.Equal("NullEnvelope", context.ShortCircuitReason);
        }

        [Fact]
        public async Task EmptyMessages_ShortCircuits()
        {
            var middleware = new RequestSanitizeMiddleware();
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    Messages = new List<ChatMessage>(),
                },
            };

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            Assert.True(context.IsShortCircuited);
            Assert.Equal("EmptyMessages", context.ShortCircuitReason);
        }

        [Fact]
        public async Task ValidMessages_CallsNext()
        {
            var middleware = new RequestSanitizeMiddleware();
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "user", Content = "hello" },
                    },
                },
            };
            bool nextCalled = false;

            await middleware.InvokeAsync(context, ctx =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            Assert.True(nextCalled);
            Assert.False(context.IsShortCircuited);
        }

        [Fact]
        public async Task SanitizesMessageContent()
        {
            var middleware = new RequestSanitizeMiddleware();
            var context = new LlmRequestContext
            {
                Envelope = new LlmRequestEnvelope
                {
                    RequestId = "req-1",
                    ScenarioId = "test",
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "user", Content = "  hello world  " },
                    },
                },
            };

            await middleware.InvokeAsync(context, _ => Task.CompletedTask);

            // PromptSanitizer.Sanitize trims and normalizes whitespace
            var sanitized = context.Envelope.Messages[0].Content;
            Assert.DoesNotContain("  ", sanitized);
        }
    }
}
