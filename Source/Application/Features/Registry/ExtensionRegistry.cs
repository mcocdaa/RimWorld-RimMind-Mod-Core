using System.Collections.Concurrent;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Interfaces.Extension;

namespace RimMind.Application.Features.Registry
{
    internal sealed class ExtensionRegistry<T> : IExtensionRegistry<T>
        where T : class, IExtension
    {
        private readonly ConcurrentDictionary<string, T> _extensions
            = new ConcurrentDictionary<string, T>();
        private readonly ILogSink? _log;

        public ExtensionRegistry(ILogSink? log = null) { _log = log; }

        public void Register(T extension)
        {
            if (extension == null) return;
            _extensions[extension.Id] = extension;
            _log?.Message($"[ExtensionRegistry] Registered {typeof(T).Name}: {extension.Id}");
        }

        public bool Unregister(string id)
        {
            return _extensions.TryRemove(id, out _);
        }

        public IReadOnlyList<T> All
        {
            get { return new List<T>(_extensions.Values); }
        }

        public T? FindById(string id)
        {
            return _extensions.TryGetValue(id, out var ext) ? ext : null;
        }
    }
}
