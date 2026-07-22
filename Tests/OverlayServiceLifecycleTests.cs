using RimMind.Application.Common.Models.UI;
using RimMind.Presentation.Runtime;
using Verse;
using Xunit;

namespace RimMind.Tests
{
    public class OverlayServiceLifecycleTests
    {
        [Fact]
        public void Register_PreservesRelativeLifetimeAndSetsAbsoluteDeadline()
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
            Assert.Equal(10, entry.expireTicks);
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
        public void Register_PreservesExplicitAbsoluteDeadline()
        {
            Find.TickManager.TicksGame = 100;
            var completionCount = 0;
            var service = new OverlayService();
            var entry = new RequestEntry
            {
                expireTicks = 25,
                ExpireAtTicks = 110,
                completionCallback = reason =>
                {
                    Assert.Equal(RequestCompletionReason.Expired, reason);
                    completionCount++;
                }
            };

            service.RegisterPendingRequest(entry);

            Assert.Equal(25, entry.expireTicks);
            Assert.Equal(110, entry.ExpireAtTicks);

            Find.TickManager.TicksGame = 109;
            service.Tick();
            Assert.Single(service.GetPendingRequests());
            Assert.Equal(0, completionCount);

            Find.TickManager.TicksGame = 110;
            service.Tick();
            service.Tick();

            Assert.Empty(service.GetPendingRequests());
            Assert.Equal(1, completionCount);
        }

        [Fact]
        public void Register_SameInstanceTwiceIsIdempotent()
        {
            Find.TickManager.TicksGame = 100;
            var service = new OverlayService();
            var entry = new RequestEntry { expireTicks = 10 };

            service.RegisterPendingRequest(entry);
            Find.TickManager.TicksGame = 150;
            service.RegisterPendingRequest(entry);

            Assert.Single(service.GetPendingRequests());
            Assert.Equal(100, entry.tick);
            Assert.Equal(10, entry.expireTicks);
            Assert.Equal(110, entry.ExpireAtTicks);
        }

        [Fact]
        public void Register_SaturatesRelativeDeadlineOnOverflow()
        {
            var previousTicksGame = Find.TickManager.TicksGame;
            try
            {
                Find.TickManager.TicksGame = int.MaxValue - 5;
                var service = new OverlayService();
                var entry = new RequestEntry { expireTicks = 10 };

                service.RegisterPendingRequest(entry);

                Assert.Equal(10, entry.expireTicks);
                Assert.Equal(int.MaxValue, entry.ExpireAtTicks);
            }
            finally
            {
                Find.TickManager.TicksGame = previousTicksGame;
            }
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
