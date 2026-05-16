using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Application.Common.Interfaces.Runtime
{
    public interface IRimMindRuntime
    {
        bool IsShutdown { get; }
        IExtensionRegistry<T> GetExtensionRegistry<T>() where T : class, IExtension;
    }
}
