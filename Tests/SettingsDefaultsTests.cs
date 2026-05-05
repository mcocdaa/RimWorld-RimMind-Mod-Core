using System;
using Xunit;

namespace RimMind.Core.Tests
{
    public class SettingsDefaultsTests
    {
        [Fact]
        public void AICoreSettings_DefaultValues_AreCorrect()
        {
            var settings = new AICoreSettings();

            Assert.Equal(30000, settings.thinkCooldownTicks);
            Assert.Equal(150, settings.agentTickInterval);
            Assert.Equal(3, settings.maxToolCallDepth);
            Assert.Equal(30000, settings.requestExpireTicks);
            Assert.Equal(100, settings.behaviorHistoryMax);
            Assert.Equal(60, settings.queueProcessInterval);
            Assert.Equal(3600, settings.defaultModCooldownTicks);
        }

        [Fact]
        public void AICoreSettings_RequestExpireTicks_CanBeModified()
        {
            var settings = new AICoreSettings { requestExpireTicks = 60000 };
            Assert.Equal(60000, settings.requestExpireTicks);
        }

        [Fact]
        public void AICoreSettings_BehaviorHistoryMax_CanBeModified()
        {
            var settings = new AICoreSettings { behaviorHistoryMax = 50 };
            Assert.Equal(50, settings.behaviorHistoryMax);
        }

        [Fact]
        public void AICoreSettings_QueueProcessInterval_CanBeModified()
        {
            var settings = new AICoreSettings { queueProcessInterval = 120 };
            Assert.Equal(120, settings.queueProcessInterval);
        }

        [Fact]
        public void AICoreSettings_DefaultModCooldownTicks_CanBeModified()
        {
            var settings = new AICoreSettings { defaultModCooldownTicks = 7200 };
            Assert.Equal(7200, settings.defaultModCooldownTicks);
        }

        [Fact]
        public void ContextSettings_DefaultValues_AreCorrect()
        {
            var ctx = new ContextSettings();

            Assert.Equal(100, ctx.maxCacheEntries);
            Assert.Equal(200, ctx.contextBriefLimit);
            Assert.Equal(5f, ctx.moodDiffThreshold);
            Assert.Equal(5f, ctx.temperatureDiffThreshold);
            Assert.Equal(5, ctx.environmentScanRadius);
            Assert.Equal(8, ctx.environmentMaxItems);
            Assert.Equal(200000f, ctx.threatThresholdHigh);
            Assert.Equal(100000f, ctx.threatThresholdMedium);
            Assert.Equal(50000f, ctx.threatThresholdLow);
        }

        [Fact]
        public void ContextSettings_MaxCacheEntries_CanBeModified()
        {
            var ctx = new ContextSettings { maxCacheEntries = 200 };
            Assert.Equal(200, ctx.maxCacheEntries);
        }

        [Fact]
        public void ContextSettings_ContextBriefLimit_CanBeModified()
        {
            var ctx = new ContextSettings { contextBriefLimit = 400 };
            Assert.Equal(400, ctx.contextBriefLimit);
        }

        [Fact]
        public void ContextSettings_MoodDiffThreshold_CanBeModified()
        {
            var ctx = new ContextSettings { moodDiffThreshold = 10f };
            Assert.Equal(10f, ctx.moodDiffThreshold);
        }

        [Fact]
        public void ContextSettings_TemperatureDiffThreshold_CanBeModified()
        {
            var ctx = new ContextSettings { temperatureDiffThreshold = 3f };
            Assert.Equal(3f, ctx.temperatureDiffThreshold);
        }

        [Fact]
        public void ContextSettings_EnvironmentScanRadius_CanBeModified()
        {
            var ctx = new ContextSettings { environmentScanRadius = 10 };
            Assert.Equal(10, ctx.environmentScanRadius);
        }

        [Fact]
        public void ContextSettings_EnvironmentMaxItems_CanBeModified()
        {
            var ctx = new ContextSettings { environmentMaxItems = 16 };
            Assert.Equal(16, ctx.environmentMaxItems);
        }

        [Fact]
        public void ContextSettings_ThreatThresholds_CanBeModified()
        {
            var ctx = new ContextSettings
            {
                threatThresholdHigh = 300000f,
                threatThresholdMedium = 150000f,
                threatThresholdLow = 75000f
            };
            Assert.Equal(300000f, ctx.threatThresholdHigh);
            Assert.Equal(150000f, ctx.threatThresholdMedium);
            Assert.Equal(75000f, ctx.threatThresholdLow);
        }

        [Fact]
        public void RimMindCoreMod_Settings_Null_Fallbacks_Work()
        {
            RimMindCoreMod.Settings = null;

            Assert.Equal(30000, RimMindCoreMod.Settings?.thinkCooldownTicks ?? 30000);
            Assert.Equal(30000, RimMindCoreMod.Settings?.requestExpireTicks ?? 30000);
            Assert.Equal(100, RimMindCoreMod.Settings?.behaviorHistoryMax ?? 100);
            Assert.Equal(60, RimMindCoreMod.Settings?.queueProcessInterval ?? 60);
            Assert.Equal(3600, RimMindCoreMod.Settings?.defaultModCooldownTicks ?? 3600);
        }

        [Fact]
        public void RimMindCoreMod_Context_Null_Fallbacks_Work()
        {
            RimMindCoreMod.Settings = new AICoreSettings { Context = null };

            Assert.Equal(100, RimMindCoreMod.Settings?.Context?.maxCacheEntries ?? 100);
            Assert.Equal(200, RimMindCoreMod.Settings?.Context?.contextBriefLimit ?? 200);
            Assert.Equal(5f, RimMindCoreMod.Settings?.Context?.moodDiffThreshold ?? 5f);
            Assert.Equal(5f, RimMindCoreMod.Settings?.Context?.temperatureDiffThreshold ?? 5f);
            Assert.Equal(5, RimMindCoreMod.Settings?.Context?.environmentScanRadius ?? 5);
            Assert.Equal(8, RimMindCoreMod.Settings?.Context?.environmentMaxItems ?? 8);
            Assert.Equal(200000f, RimMindCoreMod.Settings?.Context?.threatThresholdHigh ?? 200000f);
            Assert.Equal(100000f, RimMindCoreMod.Settings?.Context?.threatThresholdMedium ?? 100000f);
            Assert.Equal(50000f, RimMindCoreMod.Settings?.Context?.threatThresholdLow ?? 50000f);
        }

        [Fact]
        public void AICoreSettings_WithValues_ReturnsActualValues()
        {
            var settings = new AICoreSettings
            {
                requestExpireTicks = 60000,
                behaviorHistoryMax = 50,
                queueProcessInterval = 120,
                defaultModCooldownTicks = 7200,
                Context = new ContextSettings
                {
                    maxCacheEntries = 200,
                    contextBriefLimit = 400,
                    moodDiffThreshold = 10f,
                    temperatureDiffThreshold = 3f,
                    environmentScanRadius = 10,
                    environmentMaxItems = 16,
                    threatThresholdHigh = 300000f,
                    threatThresholdMedium = 150000f,
                    threatThresholdLow = 75000f
                }
            };

            Assert.Equal(60000, settings.requestExpireTicks);
            Assert.Equal(50, settings.behaviorHistoryMax);
            Assert.Equal(120, settings.queueProcessInterval);
            Assert.Equal(7200, settings.defaultModCooldownTicks);
            Assert.Equal(200, settings.Context?.maxCacheEntries ?? 100);
            Assert.Equal(400, settings.Context?.contextBriefLimit ?? 200);
            Assert.Equal(10f, settings.Context?.moodDiffThreshold ?? 5f);
            Assert.Equal(3f, settings.Context?.temperatureDiffThreshold ?? 5f);
            Assert.Equal(10, settings.Context?.environmentScanRadius ?? 5);
            Assert.Equal(16, settings.Context?.environmentMaxItems ?? 8);
            Assert.Equal(300000f, settings.Context?.threatThresholdHigh ?? 200000f);
            Assert.Equal(150000f, settings.Context?.threatThresholdMedium ?? 100000f);
            Assert.Equal(75000f, settings.Context?.threatThresholdLow ?? 50000f);
        }

        [Fact]
        public void Validate_ClampsMaxTokens_To100()
        {
            var settings = new AICoreSettings { maxTokens = 50 };
            settings.Validate();
            Assert.Equal(100, settings.maxTokens);
        }

        [Fact]
        public void Validate_DoesNotClampMaxTokens_WhenAbove100()
        {
            var settings = new AICoreSettings { maxTokens = 500 };
            settings.Validate();
            Assert.Equal(500, settings.maxTokens);
        }

        [Fact]
        public void Validate_ClampsDefaultTemperature_ToRange()
        {
            var settings = new AICoreSettings { defaultTemperature = -0.5f };
            settings.Validate();
            Assert.Equal(0.0f, settings.defaultTemperature, 4);

            settings.defaultTemperature = 3.0f;
            settings.Validate();
            Assert.Equal(2.0f, settings.defaultTemperature, 4);
        }

        [Fact]
        public void Validate_DoesNotClampDefaultTemperature_WhenInRange()
        {
            var settings = new AICoreSettings { defaultTemperature = 1.0f };
            settings.Validate();
            Assert.Equal(1.0f, settings.defaultTemperature, 4);
        }

        [Fact]
        public void Validate_ClampsMaxConcurrentRequests_To1()
        {
            var settings = new AICoreSettings { maxConcurrentRequests = 0 };
            settings.Validate();
            Assert.Equal(1, settings.maxConcurrentRequests);
        }

        [Fact]
        public void Validate_ClampsRequestTimeoutMs_To1000()
        {
            var settings = new AICoreSettings { requestTimeoutMs = 500 };
            settings.Validate();
            Assert.Equal(1000, settings.requestTimeoutMs);
        }

        [Fact]
        public void Validate_ClampsThinkCooldownTicks_To60()
        {
            var settings = new AICoreSettings { thinkCooldownTicks = 30 };
            settings.Validate();
            Assert.Equal(60, settings.thinkCooldownTicks);
        }

        [Fact]
        public void Validate_ClampsAgentTickInterval_To10()
        {
            var settings = new AICoreSettings { agentTickInterval = 5 };
            settings.Validate();
            Assert.Equal(10, settings.agentTickInterval);
        }

        [Fact]
        public void Validate_ClampsMaxToolCallDepth_To1()
        {
            var settings = new AICoreSettings { maxToolCallDepth = 0 };
            settings.Validate();
            Assert.Equal(1, settings.maxToolCallDepth);
        }

        [Fact]
        public void Validate_ClampsContextDiffLifetimeTicks_To600()
        {
            var settings = new AICoreSettings { contextDiffLifetimeTicks = 100 };
            settings.Validate();
            Assert.Equal(600, settings.contextDiffLifetimeTicks);
        }

        [Fact]
        public void Validate_DefaultValues_PassValidation()
        {
            var settings = new AICoreSettings();
            int beforeTokens = settings.maxTokens;
            float beforeTemp = settings.defaultTemperature;
            int beforeConcurrent = settings.maxConcurrentRequests;
            int beforeTimeout = settings.requestTimeoutMs;
            int beforeCooldown = settings.thinkCooldownTicks;
            int beforeTickInterval = settings.agentTickInterval;
            int beforeToolDepth = settings.maxToolCallDepth;
            int beforeDiffLifetime = settings.contextDiffLifetimeTicks;

            settings.Validate();

            Assert.Equal(beforeTokens, settings.maxTokens);
            Assert.Equal(beforeTemp, settings.defaultTemperature);
            Assert.Equal(beforeConcurrent, settings.maxConcurrentRequests);
            Assert.Equal(beforeTimeout, settings.requestTimeoutMs);
            Assert.Equal(beforeCooldown, settings.thinkCooldownTicks);
            Assert.Equal(beforeTickInterval, settings.agentTickInterval);
            Assert.Equal(beforeToolDepth, settings.maxToolCallDepth);
            Assert.Equal(beforeDiffLifetime, settings.contextDiffLifetimeTicks);
        }

        [Fact]
        public void AICoreSettings_ContextDiffLifetimeTicks_DefaultIs36000()
        {
            var settings = new AICoreSettings();
            Assert.Equal(36000, settings.contextDiffLifetimeTicks);
        }
    }
}
