using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseB
{
    public class AgentBusOnlyInImplTests
    {
        [Fact]
        [Trait("Phase", "B")]
        public void R_B2_StaticAgentBusClass_ShouldNotExist()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var agentBusFile = Path.Combine(sourceDir, "Core", "AgentBus", "AgentBus.cs");
            File.Exists(agentBusFile).Should().BeFalse(
                "The static AgentBus.cs class file must be deleted. " +
                "All AgentBus functionality should be in AgentBusImpl.cs (instance-based).");
        }

        [Fact]
        [Trait("Phase", "B")]
        public void R_B2_NoStaticAgentBusCalls_OutsideImpl()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var allowedFiles = new HashSet<string>
            {
                "AgentBusImpl.cs"
            };

            var violatingFiles = new List<string>();
            var staticCallPattern = @"AgentBus\.(Subscribe|Publish|Unsubscribe|PublishFromBackground|FlushBackgroundQueue|ClearAllSubscribers)";

            var agentBusDir = Path.Combine(sourceDir, "Core", "AgentBus");
            if (Directory.Exists(agentBusDir))
            {
                foreach (var file in Directory.GetFiles(agentBusDir, "*.cs", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(file);
                    if (allowedFiles.Contains(fileName)) continue;

                    var source = File.ReadAllText(file);
                    if (Regex.IsMatch(source, staticCallPattern))
                    {
                        violatingFiles.Add($"Core/AgentBus/{fileName}");
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                $"Static AgentBus calls are only allowed in AgentBusImpl.cs. " +
                $"All other code should use IEventBus (injected) or RimMindAPI.GetEventBus(). " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        [Fact]
        [Trait("Phase", "B")]
        public void R_B2_NoDynamicInvoke_InAgentBusImpl()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var implFile = Path.Combine(sourceDir, "Core", "AgentBus", "AgentBusImpl.cs");
            if (!File.Exists(implFile)) return;

            var source = File.ReadAllText(implFile);
            source.Should().NotContain("DynamicInvoke",
                "AgentBusImpl must not use DynamicInvoke (reflection-based dispatch). " +
                "All Publish calls should use the generic Publish<T> method.");
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(AgentBusOnlyInImplTests).Assembly.Location);
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "RimMind-Core", "Source");
                if (Directory.Exists(candidate)) return candidate;

                candidate = Path.Combine(dir, "Source");
                if (Directory.Exists(candidate)) return candidate;

                dir = Directory.GetParent(dir)?.FullName;
            }
            return "";
        }
    }
}
