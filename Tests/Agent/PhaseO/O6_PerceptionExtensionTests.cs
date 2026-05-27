using System.Reflection;
using Xunit;

namespace RimMind.Tests.Agent.PhaseO
{
    /// <summary>
    /// O6: Perception Extension tests.
    /// ArchTest-style reflection-based structural verification for
    /// RimMindDefaults perception thresholds and PerceptionBufferEntry fields.
    /// </summary>
    public class O6_PerceptionExtensionTests
    {
        private static readonly Assembly AppAssembly = typeof(RimMind.Application.Common.Models.RimMindDefaults).Assembly;

        // === RimMindDefaults perception threshold constants ===

        [Fact]
        public void RimMindDefaults_PerceptionLowThreshold_IsPositive()
        {
            var field = typeof(RimMind.Application.Common.Models.RimMindDefaults)
                .GetField("PerceptionLowThreshold", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            var value = (float)field!.GetValue(null)!;
            Assert.True(value > 0f, $"PerceptionLowThreshold should be positive, got {value}");
        }

        [Fact]
        public void RimMindDefaults_PerceptionMediumThreshold_IsPositive()
        {
            var field = typeof(RimMind.Application.Common.Models.RimMindDefaults)
                .GetField("PerceptionMediumThreshold", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            var value = (float)field!.GetValue(null)!;
            Assert.True(value > 0f, $"PerceptionMediumThreshold should be positive, got {value}");
        }

        [Fact]
        public void RimMindDefaults_PerceptionHighThreshold_IsPositive()
        {
            var field = typeof(RimMind.Application.Common.Models.RimMindDefaults)
                .GetField("PerceptionHighThreshold", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            var value = (float)field!.GetValue(null)!;
            Assert.True(value > 0f, $"PerceptionHighThreshold should be positive, got {value}");
        }

        [Fact]
        public void RimMindDefaults_PerceptionCriticalThreshold_IsPositive()
        {
            var field = typeof(RimMind.Application.Common.Models.RimMindDefaults)
                .GetField("PerceptionCriticalThreshold", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            var value = (float)field!.GetValue(null)!;
            Assert.True(value > 0f, $"PerceptionCriticalThreshold should be positive, got {value}");
        }

        // === PerceptionBufferEntry field existence ===

        [Fact]
        public void PerceptionBufferEntry_Has_PerceptionType()
        {
            var field = typeof(RimMind.Application.Common.Models.Pipeline.PerceptionBufferEntry)
                .GetField("PerceptionType", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field);
            Assert.Equal(typeof(string), field!.FieldType);
        }

        [Fact]
        public void PerceptionBufferEntry_Has_Importance()
        {
            var field = typeof(RimMind.Application.Common.Models.Pipeline.PerceptionBufferEntry)
                .GetField("Importance", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(field);
            Assert.Equal(typeof(float), field!.FieldType);
        }
    }
}
