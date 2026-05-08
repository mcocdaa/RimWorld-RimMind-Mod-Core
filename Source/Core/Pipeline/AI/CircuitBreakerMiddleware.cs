using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.AI;
using RimMind.Core.Client;

namespace RimMind.Core.Pipeline.AI
{
    public sealed class CircuitBreakerMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(CircuitBreakerMiddleware);
        public int Order => 5;

        private const int FailureThreshold = 5;
        private static readonly TimeSpan OpenDuration = TimeSpan.FromSeconds(60);

        private enum CircuitState { Closed, Open, HalfOpen }

        private CircuitState _state = CircuitState.Closed;
        private int _consecutiveFailures;
        private DateTime _openedAtUtc;

        public async Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            switch (_state)
            {
                case CircuitState.Open:
                    if (DateTime.UtcNow - _openedAtUtc >= OpenDuration)
                    {
                        _state = CircuitState.HalfOpen;
                    }
                    else
                    {
                        context.Response = AIResponse.Failure(context.Request.RequestId, "Circuit breaker is open");
                        context.ShortCircuit("circuit_open");
                        return;
                    }
                    break;
            }

            try
            {
                await next(context).ConfigureAwait(false);

                if (context.Error != null || (context.Response != null && !context.Response.Success))
                {
                    OnFailure();
                }
                else
                {
                    OnSuccess();
                }
            }
            catch
            {
                OnFailure();
                throw;
            }
        }

        private void OnSuccess()
        {
            _consecutiveFailures = 0;
            _state = CircuitState.Closed;
        }

        private void OnFailure()
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= FailureThreshold)
            {
                _state = CircuitState.Open;
                _openedAtUtc = DateTime.UtcNow;
            }
        }
    }
}
