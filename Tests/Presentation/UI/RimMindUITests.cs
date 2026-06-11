using UnityEngine;
using Xunit;
using RimMind.Infrastructure.UI;

namespace RimMind.Tests.Presentation.UI
{
    public class RimMindUIThemeTests
    {
        // ── Constant Value Tests ──────────────────────────────

        [Fact]
        public void Padding_IsPositive()
        {
            Assert.True(RimMindUITheme.Padding > 0f);
        }

        [Fact]
        public void SectionGap_IsPositive()
        {
            Assert.True(RimMindUITheme.SectionGap > 0f);
        }

        [Fact]
        public void LineHeight_IsPositive()
        {
            Assert.True(RimMindUITheme.LineHeight > 0f);
        }

        [Fact]
        public void BtnHeight_IsPositive()
        {
            Assert.True(RimMindUITheme.BtnHeight > 0f);
        }

        [Fact]
        public void HeaderHeight_IsPositive()
        {
            Assert.True(RimMindUITheme.HeaderHeight > 0f);
        }

        [Fact]
        public void TabHeight_IsPositive()
        {
            Assert.True(RimMindUITheme.TabHeight > 0f);
        }

        [Fact]
        public void SpacingConsistency_BtnHeightNotLessThanLineHeight()
        {
            Assert.True(RimMindUITheme.BtnHeight >= RimMindUITheme.LineHeight,
                "BtnHeight should be >= LineHeight for consistent button sizing");
        }

        [Fact]
        public void SpacingConsistency_HeaderHeightGreaterThanLineHeight()
        {
            Assert.True(RimMindUITheme.HeaderHeight > RimMindUITheme.LineHeight,
                "HeaderHeight should be > LineHeight for visual hierarchy");
        }

        // ── Color Constant Tests ──────────────────────────────

        [Fact]
        public void ColorActive_IsGreenish()
        {
            Assert.True(RimMindUITheme.ColorActive.g > RimMindUITheme.ColorActive.r,
                "Active color should be green-dominant");
        }

        [Fact]
        public void ColorPaused_IsYellowish()
        {
            Assert.True(RimMindUITheme.ColorPaused.r > 0.5f && RimMindUITheme.ColorPaused.g > 0.5f,
                "Paused color should be yellowish (high R and G)");
        }

        [Fact]
        public void ColorError_IsReddish()
        {
            Assert.True(RimMindUITheme.ColorError.r > RimMindUITheme.ColorError.g,
                "Error color should be red-dominant");
        }

        [Fact]
        public void ColorSectionBg_HasAlpha()
        {
            Assert.True(RimMindUITheme.ColorSectionBg.a < 1f,
                "Section background should be semi-transparent");
        }

        [Fact]
        public void ColorCardBg_HasAlpha()
        {
            Assert.True(RimMindUITheme.ColorCardBg.a < 1f,
                "Card background should be semi-transparent");
        }

        // ── GetStateBadgeColors Tests ─────────────────────────

        [Fact]
        public void GetStateBadgeColors_Active_ReturnsActiveColors()
        {
            var (text, bg) = RimMindUITheme.GetStateBadgeColors(isActive: true);
            Assert.Equal(RimMindUITheme.ColorActive, text);
            Assert.Equal(RimMindUITheme.ColorBadgeActiveBg, bg);
        }

        [Fact]
        public void GetStateBadgeColors_Paused_ReturnsPausedColors()
        {
            var (text, bg) = RimMindUITheme.GetStateBadgeColors(isActive: false, isPaused: true);
            Assert.Equal(RimMindUITheme.ColorPaused, text);
            Assert.Equal(RimMindUITheme.ColorBadgePausedBg, bg);
        }

        [Fact]
        public void GetStateBadgeColors_Idle_ReturnsIdleColors()
        {
            var (text, bg) = RimMindUITheme.GetStateBadgeColors(isActive: false, isPaused: false);
            Assert.Equal(RimMindUITheme.ColorIdle, text);
            Assert.Equal(RimMindUITheme.ColorBadgeIdleBg, bg);
        }

        [Fact]
        public void GetStateBadgeColors_ActiveTakesPrecedenceOverPaused()
        {
            var (text, bg) = RimMindUITheme.GetStateBadgeColors(isActive: true, isPaused: true);
            Assert.Equal(RimMindUITheme.ColorActive, text);
            Assert.Equal(RimMindUITheme.ColorBadgeActiveBg, bg);
        }
    }
}
