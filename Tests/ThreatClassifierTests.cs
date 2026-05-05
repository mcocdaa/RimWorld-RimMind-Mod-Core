using RimMind.Core.Internal;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ThreatClassifierTests
    {
        const float High = 200000f;
        const float Medium = 100000f;
        const float Low = 50000f;

        [Fact]
        public void ClassifyThreatTier_WealthBelowLow_ReturnsLow()
        {
            Assert.Equal("Low", ThreatClassifier.ClassifyThreatTier(30000f, High, Medium, Low, 1f));
        }

        [Fact]
        public void ClassifyThreatTier_WealthBetweenLowAndMedium_ReturnsMedium()
        {
            Assert.Equal("Medium", ThreatClassifier.ClassifyThreatTier(75000f, High, Medium, Low, 1f));
        }

        [Fact]
        public void ClassifyThreatTier_WealthBetweenMediumAndHigh_ReturnsHigh()
        {
            Assert.Equal("High", ThreatClassifier.ClassifyThreatTier(150000f, High, Medium, Low, 1f));
        }

        [Fact]
        public void ClassifyThreatTier_WealthAboveHigh_ReturnsExtreme()
        {
            Assert.Equal("Extreme", ThreatClassifier.ClassifyThreatTier(300000f, High, Medium, Low, 1f));
        }

        [Fact]
        public void ClassifyThreatTier_ExactLowBoundary_ReturnsMedium()
        {
            Assert.Equal("Medium", ThreatClassifier.ClassifyThreatTier(Low, High, Medium, Low, 1f));
        }

        [Fact]
        public void ClassifyThreatTier_ExactMediumBoundary_ReturnsHigh()
        {
            Assert.Equal("High", ThreatClassifier.ClassifyThreatTier(Medium, High, Medium, Low, 1f));
        }

        [Fact]
        public void ClassifyThreatTier_ExactHighBoundary_ReturnsExtreme()
        {
            Assert.Equal("Extreme", ThreatClassifier.ClassifyThreatTier(High, High, Medium, Low, 1f));
        }

        [Fact]
        public void ClassifyThreatTier_HigherThreatScale_LowersEffectiveThresholds()
        {
            Assert.Equal("Extreme", ThreatClassifier.ClassifyThreatTier(150000f, High, Medium, Low, 2f));
        }

        [Fact]
        public void ClassifyThreatTier_ThreatScale2_Wealth75000_ReturnsHigh()
        {
            Assert.Equal("High", ThreatClassifier.ClassifyThreatTier(75000f, High, Medium, Low, 2f));
        }

        [Fact]
        public void ClassifyThreatTier_ThreatScale2_Wealth30000_ReturnsMedium()
        {
            Assert.Equal("Medium", ThreatClassifier.ClassifyThreatTier(30000f, High, Medium, Low, 2f));
        }

        [Fact]
        public void ClassifyThreatTier_LowerThreatScale_RaisesEffectiveThresholds()
        {
            Assert.Equal("Low", ThreatClassifier.ClassifyThreatTier(75000f, High, Medium, Low, 0.5f));
        }

        [Fact]
        public void ClassifyThreatScale05_Wealth150000_ReturnsMedium()
        {
            Assert.Equal("Medium", ThreatClassifier.ClassifyThreatTier(150000f, High, Medium, Low, 0.5f));
        }

        [Fact]
        public void ClassifyThreatScale05_Wealth300000_ReturnsHigh()
        {
            Assert.Equal("High", ThreatClassifier.ClassifyThreatTier(300000f, High, Medium, Low, 0.5f));
        }

        [Fact]
        public void ClassifyThreatTier_ZeroThreatScale_TreatedAs1()
        {
            Assert.Equal("Medium", ThreatClassifier.ClassifyThreatTier(75000f, High, Medium, Low, 0f));
        }

        [Fact]
        public void ClassifyThreatTier_NegativeThreatScale_TreatedAs1()
        {
            Assert.Equal("Medium", ThreatClassifier.ClassifyThreatTier(75000f, High, Medium, Low, -1f));
        }

        [Fact]
        public void ClassifyThreatTier_ThreatScale1_SameAsDefault()
        {
            Assert.Equal("Medium", ThreatClassifier.ClassifyThreatTier(75000f, High, Medium, Low, 1f));
        }
    }
}
