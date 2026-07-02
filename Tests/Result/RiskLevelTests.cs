using RimMind.Domain.Enums;
using Xunit;

namespace RimMind.Presentation.Tests.Result
{
    /// <summary>
    /// RiskLevel 枚举测试：迁移自 RimMind-Actions/Tests。
    /// </summary>
    public class RiskLevelTests
    {
        [Fact]
        public void RiskLevel_Values_AreSequential()
        {
            Assert.Equal(0, (int)RiskLevel.Low);
            Assert.Equal(1, (int)RiskLevel.Medium);
            Assert.Equal(2, (int)RiskLevel.High);
            Assert.Equal(3, (int)RiskLevel.Critical);
        }

        [Fact]
        public void RiskLevel_HasFourValues()
        {
            var values = System.Enum.GetValues(typeof(RiskLevel));
            Assert.Equal(4, values.Length);
        }

        [Theory]
        [InlineData(RiskLevel.Low)]
        [InlineData(RiskLevel.Medium)]
        [InlineData(RiskLevel.High)]
        [InlineData(RiskLevel.Critical)]
        public void RiskLevel_CanParseFromString(RiskLevel expected)
        {
            var parsed = System.Enum.Parse(typeof(RiskLevel), expected.ToString());
            Assert.Equal(expected, parsed);
        }
    }
}
