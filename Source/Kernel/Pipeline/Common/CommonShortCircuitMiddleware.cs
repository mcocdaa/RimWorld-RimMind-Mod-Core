using System;
using System.Threading.Tasks;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Logging;

namespace RimMind.Core.Pipeline.Common
{
    public sealed class CommonShortCircuitMiddleware<TContext> : IMiddleware<TContext>
        where TContext : IPipelineContext
    {
        private readonly Func<TContext, string?> _predicate;
        private readonly string _name;

        public CommonShortCircuitMiddleware(Func<TContext, string?> predicate, string name = "ShortCircuit")
        {
            _predicate = predicate;
            _name = name;
        }

        public string Id => $"Common.{_name}";
        public string Name => _name;
        public int Order => -100;

        public async Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next)
        {
            var reason = _predicate(context);
            if (reason != null)
            {
                context.ShortCircuit(reason);
                RimMindLogger.Message($"[{_name}] ShortCircuited: {reason}");
                return;
            }
            await next(context).ConfigureAwait(false);
        }
    }
}
