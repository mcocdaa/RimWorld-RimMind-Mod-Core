using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;

namespace RimMind.Application.Features.Pipeline.AI
{
    internal sealed class RequestSanitizeMiddleware : IMiddleware<AIRequestContext>
    {
        public string Name => "AIRequestSanitize";
        public int Order => 50;
        public string Id => "AIRequestSanitize";

        private readonly ILogSink? _log;

        public RequestSanitizeMiddleware(ILogSink? log = null) { _log = log; }

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            if (context.Request == null)
            {
                context.ShortCircuit("NullRequest");
                return Task.CompletedTask;
            }

            if (string.IsNullOrWhiteSpace(context.Request.SystemPrompt))
                context.Request.SystemPrompt = "You are a helpful assistant.";

            if (string.IsNullOrWhiteSpace(context.Request.UserPrompt))
            {
                context.ShortCircuit("EmptyUserPrompt");
                return Task.CompletedTask;
            }

            return next(context);
        }
    }
}
