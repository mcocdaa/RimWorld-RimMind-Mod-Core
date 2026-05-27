using System.Reflection;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using Xunit;

namespace RimMind.Tests.Agent
{
    /// <summary>
    /// ArchTest-style tests verifying structural elements of PawnThinker's timeout mechanism.
    /// PawnThinker depends on RimWorld's Find.TickManager which cannot be easily mocked in unit tests,
    /// so we verify the code structure via the configuration constants and interfaces that drive it.
    /// The Presentation layer (containing PawnThinker) is not compiled into this test project
    /// due to Verse dependencies, so we verify the Application-layer contracts instead.
    /// </summary>
    public class PawnThinkerTimeoutTests
    {
        [Fact]
        public void RimMindDefaults_Contains_ThinkRequestTimeoutTicks()
        {
            var field = typeof(RimMindDefaults).GetField("ThinkRequestTimeoutTicks",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            Assert.Equal(1800, field.GetValue(null));
        }

        [Fact]
        public void RimMindDefaults_Contains_ThinkCooldownTicks()
        {
            var field = typeof(RimMindDefaults).GetField("ThinkCooldownTicks",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            Assert.Equal(30000, field.GetValue(null));
        }

        [Fact]
        public void IAgentTickSettings_Defines_ThinkCooldownTicks()
        {
            var property = typeof(IAgentTickSettings).GetProperty("ThinkCooldownTicks");
            Assert.NotNull(property);
            Assert.Equal(typeof(int), property.PropertyType);
        }

        [Fact]
        public void IAgentTickSettings_Defines_MaxToolCallDepth()
        {
            var property = typeof(IAgentTickSettings).GetProperty("MaxToolCallDepth");
            Assert.NotNull(property);
            Assert.Equal(typeof(int), property.PropertyType);
        }

        [Fact]
        public void RimMindDefaults_Contains_DefaultMaxToolCallDepth()
        {
            var field = typeof(RimMindDefaults).GetField("DefaultMaxToolCallDepth",
                BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(field);
            Assert.Equal(3, field.GetValue(null));
        }

        [Fact]
        public void ThinkRequestTimeoutTicks_IsLessThan_ThinkCooldownTicks()
        {
            // Timeout should be shorter than cooldown so a lost request doesn't block the next think
            var timeout = (int)typeof(RimMindDefaults)
                .GetField("ThinkRequestTimeoutTicks", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;
            var cooldown = (int)typeof(RimMindDefaults)
                .GetField("ThinkCooldownTicks", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;
            Assert.True(timeout < cooldown,
                $"ThinkRequestTimeoutTicks ({timeout}) should be less than ThinkCooldownTicks ({cooldown})");
        }
    }
}
