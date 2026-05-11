using System.Collections.Generic;

namespace RimMind.Contracts.Mechanisms
{
    public interface IGameMechanismRegistry
    {
        void Register(IGameMechanism mechanism);
        bool Unregister(string mechanismId);
        IGameMechanism? FindById(string mechanismId);
        IReadOnlyList<IGameMechanism> All { get; }
    }
}
