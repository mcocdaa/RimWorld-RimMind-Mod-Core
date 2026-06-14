using RimMind.Domain.Common;
using Xunit;

namespace RimMind.Tests.Domain
{
    public class UnitTests
    {
        [Fact]
        public void Value_IsSingleton()
        {
            Assert.Equal(Unit.Value, Unit.Value);
        }

        [Fact]
        public void ToString_ReturnsParentheses()
        {
            Assert.Equal("()", Unit.Value.ToString());
        }

        [Fact]
        public void GetHashCode_AlwaysZero()
        {
            Assert.Equal(0, Unit.Value.GetHashCode());
        }

        [Fact]
        public void Equals_SameType_ReturnsTrue()
        {
            Assert.True(Unit.Value.Equals(Unit.Value));
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            Assert.False(Unit.Value.Equals(null));
        }

        [Fact]
        public void Equals_DifferentType_ReturnsFalse()
        {
            Assert.False(Unit.Value.Equals("not a unit"));
        }

        [Fact]
        public void EqualityOperators_AlwaysTrue()
        {
            var left = Unit.Value;
            var right = new Unit();

            Assert.True(left == right);
            Assert.False(left != right);
        }
    }
}
