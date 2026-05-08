using System.Collections.Generic;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Extensions;
using RimMind.Contracts.Internal;
using RimMind.Contracts.UI;

namespace RimMind.Contracts.Runtime
{
    public interface IRimMindRuntime
    {
        IClientManager ClientManager { get; }
        IProviderRegistry ProviderRegistry { get; }
        IAIRequestQueue Queue { get; }
        bool IsShutdown { get; }
        IReadOnlyList<IParameterTuner> ParameterTunersList { get; }
        IAgentActionBridge? AgentActionBridge { get; }
        IAudioPlayer AudioPlayer { get; }
        T GetExtensionRegistry<T>() where T : IExtension;
        void RegisterParameterTuner(IParameterTuner tuner);
    }
}
