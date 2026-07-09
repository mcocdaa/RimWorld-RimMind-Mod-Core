using FluentAssertions;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN
{
    public class R_N9_ResultEquatableTests
    {
        [Fact]
        [Trait("Phase", "N")]
        public void TwoOkResults_WithSameValue_ShouldBeEqual()
        {
            var r1 = Result<int, string>.Ok(42);
            var r2 = Result<int, string>.Ok(42);
            r1.Equals(r2).Should().BeTrue();
            (r1 == r2).Should().BeTrue();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void TwoOkResults_WithDifferentValue_ShouldNotBeEqual()
        {
            var r1 = Result<int, string>.Ok(42);
            var r2 = Result<int, string>.Ok(43);
            r1.Equals(r2).Should().BeFalse();
            (r1 != r2).Should().BeTrue();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void TwoErrResults_WithSameError_ShouldBeEqual()
        {
            var r1 = Result<int, string>.Err("fail");
            var r2 = Result<int, string>.Err("fail");
            r1.Equals(r2).Should().BeTrue();
            (r1 == r2).Should().BeTrue();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void Ok_And_Err_ShouldNotBeEqual()
        {
            var r1 = Result<int, string>.Ok(1);
            var r2 = Result<int, string>.Err("x");
            r1.Equals(r2).Should().BeFalse();
            (r1 == r2).Should().BeFalse();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void GetHashCode_ShouldBeConsistent_WithEquals()
        {
            var r1 = Result<int, string>.Ok(42);
            var r2 = Result<int, string>.Ok(42);
            r1.GetHashCode().Should().Be(r2.GetHashCode());
        }

        [Fact]
        [Trait("Phase", "N")]
        public void Equals_NullObject_ShouldReturnFalse()
        {
            var r1 = Result<int, string>.Ok(42);
            r1.Equals(null).Should().BeFalse();
        }

        [Fact]
        [Trait("Phase", "N")]
        public void Equals_DifferentType_ShouldReturnFalse()
        {
            var r1 = Result<int, string>.Ok(42);
            r1.Equals("not a result").Should().BeFalse();
        }
    }
}
