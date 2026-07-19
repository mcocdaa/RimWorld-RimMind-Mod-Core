using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.Runtime;
using Verse;
using Xunit;

namespace RimMind.Tests
{
    public class OverlayServiceLifecycleTests
    {
        [Fact]
        public void Register_ConvertsRelativeLifetimeAndExpirationCompletesOnce()
        {
            Find.TickManager.TicksGame = 100;
            var completionCount = 0;
            var service = new OverlayService();
            var entry = new RequestEntry
            {
                expireTicks = 10,
                completionCallback = reason =>
                {
                    Assert.Equal(RequestCompletionReason.Expired, reason);
                    completionCount++;
                }
            };

            service.RegisterPendingRequest(entry);
            Assert.Equal(110, entry.ExpireAtTicks);

            Find.TickManager.TicksGame = 109;
            service.Tick();
            Assert.Single(service.GetPendingRequests());

            Find.TickManager.TicksGame = 110;
            service.Tick();
            service.Tick();

            Assert.Empty(service.GetPendingRequests());
            Assert.Equal(1, completionCount);
        }

        [Fact]
        public void CapacityEvictionTerminatesOldestEntry()
        {
            Find.TickManager.TicksGame = 0;
            var service = new OverlayService();
            var evicted = 0;
            var oldest = new RequestEntry
            {
                completionCallback = reason =>
                {
                    Assert.Equal(RequestCompletionReason.Evicted, reason);
                    evicted++;
                }
            };
            service.RegisterPendingRequest(oldest);

            for (var i = 0; i < 50; i++)
                service.RegisterPendingRequest(new RequestEntry());

            Assert.Equal(50, service.GetPendingRequests().Count);
            Assert.Equal(1, evicted);
        }

        [Fact]
        public void LegacyEntryExpirationSelectsItsFallbackOption()
        {
            Find.TickManager.TicksGame = 20;
            var selected = "";
            var service = new OverlayService();
            service.RegisterPendingRequest(new RequestEntry
            {
                expireTicks = 1,
                options = new[] { "yes", "ignore" },
                callback = choice => selected = choice
            });

            Find.TickManager.TicksGame = 21;
            service.Tick();

            Assert.Equal("ignore", selected);
        }

        [Fact]
        public void ClearDismissesEveryPendingEntry()
        {
            var service = new OverlayService();
            var dismissed = 0;
            for (var i = 0; i < 3; i++)
            {
                service.RegisterPendingRequest(new RequestEntry
                {
                    completionCallback = reason =>
                    {
                        Assert.Equal(RequestCompletionReason.Dismissed, reason);
                        dismissed++;
                    }
                });
            }

            service.Clear();

            Assert.Empty(service.GetPendingRequests());
            Assert.Equal(3, dismissed);
        }
    }
}
