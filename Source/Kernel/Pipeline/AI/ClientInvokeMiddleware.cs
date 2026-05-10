using System;
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
            try
            {
                var response = await context.Client!.SendAsync(context.Request).ConfigureAwait(false);
                context.Response = response;
            }
            catch (Exception ex)
            {
                context.Error = ex;
                context.Response = AIResponse.Failure(context.Request.RequestId, ex.Message);
            }
        }
    }
}
