using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseA
{
    public class NoReverseDependencyTests
    {
        private static readonly (string ns, string dir)[] InternalNamespaces = new[]
        {
            ("RimMind.Core.AgentBus", "Core\\AgentBus"),
            ("RimMind.Core.Context", "Core\\Context"),
            ("RimMind.Core.Agent", "Core\\Agent"),
            ("RimMind.Core.Perception", "Core\\Perception")
        };

        private static readonly HashSet<string> WhitelistFiles = new()
        {
            "AgentBusGameComponent.cs",
            "BudgetScheduler.cs",
            "AgentGoalStack.cs",
            "JobDriver_RimMindAction.cs",
            "PawnAgent.cs"
        };

        [Fact]
        [Trait("Phase", "A")]
        public void R_A1_InternalComponents_ShouldNotDependOn_RimMindAPI()
        {
            var sourceDir = FindSourceDirectory();
            sourceDir.Should().NotBeNull("Source directory must exist for analysis");

            var violatingFiles = new List<string>();

            foreach (var (ns, dir) in InternalNamespaces)
            {
                var nsDir = Path.Combine(sourceDir, dir);
                if (!Directory.Exists(nsDir)) continue;

                foreach (var file in Directory.GetFiles(nsDir, "*.cs", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(file);
                    if (WhitelistFiles.Contains(fileName)) continue;

                    var source = File.ReadAllText(file);
                    if (ContainsRimMindAPIReference(source, fileName))
                    {
                        violatingFiles.Add($"{ns}/{fileName}");
                    }
                }
            }

            violatingFiles.Should().BeEmpty(
                $"Internal components must not depend on RimMindAPI. " +
                $"Violating files:\n  {string.Join("\n  ", violatingFiles)}");
        }

        private static bool ContainsRimMindAPIReference(string source, string fileName)
        {
            var patterns = new[]
            {
                @"(?<!//.*?)\bRimMindAPI\b",
                @"using\s+RimMind\.Core\s*;",
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(source, pattern);
                foreach (Match m in matches)
                {
                    var lineStart = source.LastIndexOf('\n', m.Index) + 1;
                    var lineEnd = source.IndexOf('\n', m.Index);
                    if (lineEnd < 0) lineEnd = source.Length;
                    var line = source.Substring(lineStart, lineEnd - lineStart).Trim();

                    if (line.StartsWith("//") || line.StartsWith("///") || line.StartsWith("/*"))
                        continue;

                    return true;
                }
            }

            return false;
        }

        private static string FindSourceDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(NoReverseDependencyTests).Assembly.Location);
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
