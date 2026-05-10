using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.AI;
using RimMind.Contracts.Client;

namespace RimMind.Kernel.Pipeline.AI
{
    public sealed class ClientInvokeMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(ClientInvokeMiddleware);
        public int Order => 7;

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            context.Result = await context.Client!.SendAsync(context.Request).ConfigureAwait(false);
        }
    }
}
