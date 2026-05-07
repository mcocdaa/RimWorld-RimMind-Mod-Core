using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Pipeline.AI;
using RimMind.Kernel.Prompt;

namespace RimMind.Kernel.Pipeline.AI
{
    public sealed class RequestSanitizeMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(RequestSanitizeMiddleware);
        public int Order => 2;

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            context.Request.SystemPrompt = PromptSanitizer.Sanitize(context.Request.SystemPrompt);

            if (context.Request.Messages != null)
            {
                for (int i = 0; i < context.Request.Messages.Count; i++)
                {
                    context.Request.Messages[i].Content = PromptSanitizer.Sanitize(context.Request.Messages[i].Content);
                }
            }

            return next(context);
        }
    }
}
