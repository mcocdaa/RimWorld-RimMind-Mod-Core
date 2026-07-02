using System;
using RimMind.Domain.Enums;
using Xunit;

namespace RimMind.Presentation.Tests.Result
{
    /// <summary>
    /// MechanismRisk 枚举测试：验证值序列、数量和解析。
    /// Migrated from RimMind-Actions/Tests (was the only test source for this Core enum).
    /// </summary>
    public class MechanismRiskTests
    {
        [Fact]
        public void MechanismRisk_值按顺序排列()
        {
            Assert.Equal(0, (int)MechanismRisk.Safe);
            Assert.Equal(1, (int)MechanismRisk.Moderate);
            Assert.Equal(2, (int)MechanismRisk.Dangerous);
        }

        [Fact]
        public void MechanismRisk_有且仅有三个值()
        {
            var values = Enum.GetValues(typeof(MechanismRisk));
            Assert.Equal(3, values.Length);
        }

        [Theory]
        [InlineData(MechanismRisk.Safe)]
        [InlineData(MechanismRisk.Moderate)]
        [InlineData(MechanismRisk.Dangerous)]
        public void MechanismRisk_可从字符串解析(MechanismRisk expected)
        {
            var parsed = Enum.Parse(typeof(MechanismRisk), expected.ToString());
            Assert.Equal(expected, parsed);
        }

        [Fact]
        public void MechanismRisk_与RiskLevel语义不同()
        {
            // MechanismRisk 有3级，RiskLevel 有4级，二者不应混用
            Assert.NotEqual(
                Enum.GetNames(typeof(MechanismRisk)).Length,
                Enum.GetNames(typeof(RiskLevel)).Length);
        }
    }
}
