using System.Collections.Generic;
using RimMind.Infrastructure.UI;
using Xunit;

namespace RimMind.Tests.Infrastructure.UI
{
    public sealed class NpcSyncStateStoreTests
    {
        [Fact]
        public void Adding_past_capacity_cancels_and_evicts_the_oldest_active_state_first()
        {
            var active = new HashSet<string> { "oldest", "newer" };
            var cancelled = new List<string>();
            var states = new NpcSyncStateStore<string, string>(capacity: 2);

            states.GetOrAdd("oldest", () => "state-1", active.Contains, Cancel);
            states.GetOrAdd("newer", () => "state-2", active.Contains, Cancel);
            states.GetOrAdd("new", () => "state-3", active.Contains, Cancel);

            Assert.Equal(2, states.Count);
            Assert.Equal(new[] { "oldest" }, cancelled);
            Assert.False(states.TryGetValue("oldest", out _));
            Assert.True(states.TryGetValue("new", out _));

            void Cancel(string key)
            {
                cancelled.Add(key);
                active.Remove(key);
            }
        }
    }
}
