using System;
using System.Threading;
using RimMind.Application.Common.Defaults;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Presentation.Runtime.Services
{
    internal sealed class AgentActionBridgeSlot : IAgentActionBridgeAccessor
    {
        private IAgentActionBridge _current = NullAgentActionBridge.Instance;

        public IAgentActionBridge Current => Volatile.Read(ref _current);

        public void Replace(IAgentActionBridge bridge)
        {
            if (bridge == null)
            {
                throw new ArgumentNullException(nameof(bridge));
            }

            Volatile.Write(ref _current, bridge);
        }

        public void Reset()
        {
            Volatile.Write(ref _current, NullAgentActionBridge.Instance);
        }
    }
}
