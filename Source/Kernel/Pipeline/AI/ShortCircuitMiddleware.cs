using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.AI;
using RimMind.Contracts.Client;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Runtime;
using RimMind.Kernel.Logging;
using RimMind.Core;
using RimMind.Contracts.Result;

namespace RimMind.Kernel.Pipeline.AI
{
    public sealed class ShortCircuitMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(ShortCircuitMiddleware);
        public int Order => 0;

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            if (RimMindServiceLocator.Get<IRimMindRuntime>()?.IsShutdown == true)
            {
                context.Result = Result<AIResponse, RimMindError>.Err(RimMindErrors.PipelineShortCircuited("shutdown"));
                context.ShortCircuit("shutdown");
                return Task.CompletedTask;
            }

            if (RimMindCoreMod.Settings?.IsConfigured() != true)
            {
                context.Result = Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientNotConfigured("ShortCircuit"));
                context.ShortCircuit("not_configured");
                return Task.CompletedTask;
            }

            if (context.Client == null)
            {
                context.Result = Result<AIResponse, RimMindError>.Err(RimMindErrors.ClientNotConfigured("no_client"));
                context.ShortCircuit("no_client");
                return Task.CompletedTask;
            }

            return next(context);
        }
    }
}
