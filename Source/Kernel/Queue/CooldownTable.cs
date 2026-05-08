using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimMind.Contracts;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Runtime;

namespace RimMind.Kernel.Queue
{
    public class CooldownTable
    {
        private readonly object _lock = new object();
        private readonly ConcurrentDictionary<string, int> _modCooldowns = new ConcurrentDictionary<string, int>();

        public void Set(string modId, int tick)
        {
            lock (_lock) { _modCooldowns[modId] = tick; }
        }

        public bool IsOnCooldown(string modId, int currentTick)
        {
            lock (_lock)
            {
                if (!_modCooldowns.TryGetValue(modId, out int nextAllowed)) return false;
                return currentTick < nextAllowed;
            }
        }

        public int GetCooldownTicksLeft(string modId, int currentTick)
        {
            lock (_lock)
            {
                if (!_modCooldowns.TryGetValue(modId, out int nextAllowed)) return 0;
                int left = nextAllowed - currentTick;
                return left > 0 ? left : 0;
            }
        }

        public void Clear(string modId)
        {
            lock (_lock) { _modCooldowns.TryRemove(modId, out _); }
        }

        public void ClearAll()
        {
            lock (_lock) { _modCooldowns.Clear(); }
        }

        public IReadOnlyDictionary<string, int> GetAll()
        {
            lock (_lock) { return _modCooldowns.ToDictionary(k => k.Key, k => k.Value); }
        }

        public Dictionary<string, int> GetSnapshot()
        {
            lock (_lock) { return new Dictionary<string, int>(_modCooldowns); }
        }

        public int GetModCooldownTicks(string modId)
        {
            var cooldown = RimMindServiceLocator.Get<IRimMindRuntime>()?.GetExtensionRegistry<IModCooldown>().FindById(modId);
            if (cooldown != null)
            {
                try { return cooldown.CooldownTicks; }
                catch { }
            }
            return RimMindModAccessor.Settings?.defaultModCooldownTicks ?? 3600;
        }
    }
}
