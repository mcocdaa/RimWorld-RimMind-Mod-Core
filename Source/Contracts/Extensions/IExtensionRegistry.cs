using System.Collections.Generic;

namespace RimMind.Contracts.Extensions;

public interface IExtensionRegistry<T> where T : class, IExtension
{
    void Register(T extension);
    bool Unregister(string id);
    IReadOnlyList<T> All { get; }
    T? FindById(string id);
}
