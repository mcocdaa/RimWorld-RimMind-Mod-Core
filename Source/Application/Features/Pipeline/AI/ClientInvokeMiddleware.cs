using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Features.Pipeline.AI
{
    internal sealed class ClientInvokeMiddleware : IMiddleware<AIRequestContext>
    {
        public string Name => "AIClientInvoke";
        public int Order => 500;
        public string Id => "AIClientInvoke";

        private readonly Func<IAIClient?> _clientFactory;
        private readonly ILogSink? _log;

        public ClientInvokeMiddleware(Func<IAIClient?> clientFactory, ILogSink? log = null)
        {
            _clientFactory = clientFactory;
            _log = log;
        }

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            var client = _clientFactory();
            if (client == null)
            {
                context.Response = new AIResponse
                {
                    RequestId = context.Request.RequestId,
                    State = AIRequestState.Error
                };
                context.ShortCircuit("NoClient");
                return;
            }

            var result = await client.SendAsync(context.Request);
            if (result.IsOk)
            {
                context.Response = result.Value;
            }
            else
            {
                context.Response = new AIResponse
                {
                    RequestId = context.Request.RequestId,
                    State = AIRequestState.Error
                };
                _log?.Warning($"[AIClientInvoke] Error: {result.Error.Message}");
            }
            await next(context);
        }
    }
}
