using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Pipeline;

namespace RimMind.Application.Common.Interfaces.Runtime
{
    public interface IRimMindRuntime
    {
        bool IsShutdown { get; }
        IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension;
        void AddMiddleware<TContext>(IMiddleware<TContext> middleware) where TContext : IPipelineContext;
        T? GetService<T>() where T : class;
    }
}
