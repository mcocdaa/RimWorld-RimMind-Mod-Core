using System;
using RimMind.Contracts;
using RimMind.Core.Agent;
using RimMind.Kernel.Bus;
using RimMind.Contracts.Client;
using RimMind.Contracts.Internal;
using RimMind.Kernel.Context;
using RimMind.Contracts.Context;
using RimMind.Adapters.UI;
using RimMind.Contracts.UI;

namespace RimMind.Core.Runtime
{
    internal sealed class RuntimeOverrides
    {
        private readonly RimMindRuntime _runtime;

        public RuntimeOverrides(RimMindRuntime runtime)
        {
            _runtime = runtime;
        }

        public IEventBus? EventBus { get; set; }
        public IContextEngine? ContextEngine { get; set; }
        public IClientManager? ClientManager { get; set; }
        public IHistoryManager? HistoryManager { get; set; }
        public IAudioPlayer? AudioPlayer { get; set; }

        public void Apply()
        {
            if (EventBus != null) _runtime.EventBus = EventBus;
            if (ContextEngine != null) _runtime.ContextEngine = ContextEngine;
            if (ClientManager != null) _runtime.ClientManager = ClientManager;
            if (HistoryManager != null) _runtime.HistoryManager = HistoryManager;
            if (AudioPlayer != null) _runtime.AudioPlayer = AudioPlayer;
        }
    }
}
