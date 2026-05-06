using System.Collections.Generic;

namespace RimMind.Contracts.Extension
{
    public interface IExtension
    {
        string Id { get; }
    }

    public interface IExtensionRegistry<T> where T : class, IExtension
    {
        void Register(T extension);
        bool Unregister(string id);
        IReadOnlyList<T> All { get; }
        T? FindById(string id);
    }

    public interface IModCooldown : IExtension
    {
        int CooldownTicks { get; }
    }
}
