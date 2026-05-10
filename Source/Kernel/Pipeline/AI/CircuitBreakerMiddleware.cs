using System;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.AI;
using RimMind.Kernel.Logging;
using RimMind.Core;
using RimMind.Contracts.Result;

namespace RimMind.Kernel.Pipeline.AI
{
    public sealed class CircuitBreakerMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(CircuitBreakerMiddleware);
        public int Order => 5;

        private int FailureThreshold => RimMindCoreMod.Settings?.circuitBreakerFailureThreshold > 0
            ? RimMindCoreMod.Settings.circuitBreakerFailureThreshold : 5;
        private TimeSpan OpenDuration => TimeSpan.FromSeconds(
            RimMindCoreMod.Settings?.circuitBreakerOpenDurationSec > 0
            ? RimMindCoreMod.Settings.circuitBreakerOpenDurationSec : 60);

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
                        context.Result = Result<AIResponse, RimMindError>.Err(RimMindErrors.CircuitOpen());
                        context.ShortCircuit("circuit_open");
                        return;
                    }
                    break;
            }

            await next(context).ConfigureAwait(false);

            if (context.Result?.IsErr == true)
            {
                OnFailure();
            }
            else
            {
                OnSuccess();
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
