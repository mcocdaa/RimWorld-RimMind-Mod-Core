using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Contracts.Extension;

namespace RimMind.Core.Registry;

internal sealed class ExtensionRegistry<T> : IExtensionRegistry<T> where T : class, IExtension
{
    private readonly ConcurrentDictionary<string, T> _items = new ConcurrentDictionary<string, T>();

    public void Register(T ext)
    {
        _items[ext.Id] = ext;
    }

    public bool Unregister(string id)
    {
        return _items.TryRemove(id, out _);
    }

    public IReadOnlyList<T> All => _items.Values.ToList();

    public T? FindById(string id)
    {
        return _items.TryGetValue(id, out var v) ? v : null;
    }
}
