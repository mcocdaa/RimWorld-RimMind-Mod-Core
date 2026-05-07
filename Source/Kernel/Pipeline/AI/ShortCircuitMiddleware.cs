using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Contracts.Pipeline.AI;
using RimMind.Core.Client;
using RimMind.Core.Runtime;
using RimMind.Core.Settings;

namespace RimMind.Kernel.Pipeline.AI
{
    public sealed class ShortCircuitMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(ShortCircuitMiddleware);
        public int Order => 0;

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            if (RimMindRuntime.Instance.IsShutdown)
            {
                context.Response = AIResponse.Failure(context.Request.RequestId, "shutdown");
                context.ShortCircuit("shutdown");
                return Task.CompletedTask;
            }

            if (RimMindCoreMod.Settings?.IsConfigured() != true)
            {
                context.Response = AIResponse.Failure(context.Request.RequestId, "not_configured");
                context.ShortCircuit("not_configured");
                return Task.CompletedTask;
            }

            if (context.Client == null)
            {
                context.Response = AIResponse.Failure(context.Request.RequestId, "no_client");
                context.ShortCircuit("no_client");
                return Task.CompletedTask;
            }

            return next(context);
        }
    }
}
