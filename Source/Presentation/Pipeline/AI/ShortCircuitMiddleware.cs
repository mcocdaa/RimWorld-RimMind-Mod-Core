using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Models.Client;

namespace RimMind.Presentation.Pipeline.AI
{
    public sealed class ShortCircuitMiddleware : IMiddleware<AIRequestContext>
    {
        private readonly IApiCredentialSettings _credentialSettings;

        public ShortCircuitMiddleware(IApiCredentialSettings credentialSettings)
        {
            _credentialSettings = credentialSettings;
        }

        public string Id => Name;
        public string Name => nameof(ShortCircuitMiddleware);
        public int Order => 0;

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            if (_credentialSettings.IsConfigured != true)
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
