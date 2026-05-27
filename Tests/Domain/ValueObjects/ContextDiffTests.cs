using Xunit;

namespace RimMind.Tests.Domain.ValueObjects
{
    public class ContextDiffTests
    {
        [Fact]
        public void IsExpired_CurrentTickEqualsExpireTick_ReturnsFalse()
        {
            var diff = new ContextDiff { ExpireTick = 100 };

            Assert.False(diff.IsExpired(100));
        }

        [Fact]
        public void IsExpired_CurrentTickGreaterThanExpireTick_ReturnsTrue()
        {
            var diff = new ContextDiff { ExpireTick = 100 };

            Assert.True(diff.IsExpired(101));
        }

        [Fact]
        public void IsExpired_CurrentTickLessThanExpireTick_ReturnsFalse()
        {
            var diff = new ContextDiff { ExpireTick = 100 };

            Assert.False(diff.IsExpired(99));
        }

        [Fact]
        public void Format_OldValueEmpty_ShowsOnlyNewValue()
        {
            var diff = new ContextDiff
            {
                Key = "health",
                OldValue = "",
                NewValue = "80"
            };

            var result = diff.Format();

            Assert.Equal("[health] 80", result);
        }

        [Fact]
        public void Format_OldValueNonEmpty_ShowsOldToNew()
        {
            var diff = new ContextDiff
            {
                Key = "health",
                OldValue = "100",
                NewValue = "80"
            };

            var result = diff.Format();

            Assert.Equal("[health] 100 -> 80", result);
        }
    }
}
