using System.Threading.Tasks;
using RimMind.Contracts.Extension;

namespace RimMind.Contracts.Pipeline;

public interface IMiddleware<TContext> : IExtension where TContext : IPipelineContext
{
    string Name { get; }
    int Order { get; }
    Task InvokeAsync(TContext context, MiddlewareDelegate<TContext> next);
}
