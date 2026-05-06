using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseA
{
    public class NoSetBackdoorTests
    {
        private static readonly HashSet<string> Whitelist = new();

        private const string SourceRelativePath = @"..\..\Source\RimMindAPI.cs";

        [Fact]
        [Trait("Phase", "A")]
        public void R_A4_RimMindAPI_ShouldNotHave_InternalSetBackdoorMethods()
        {
            var sourcePath = FindFileUpwards("RimMindAPI.cs");

            File.Exists(sourcePath).Should().BeTrue("RimMindAPI.cs source file must exist for analysis");

            var source = File.ReadAllText(sourcePath);
            var pattern = @"internal\s+static\s+void\s+(Set\w+)\s*\(";
            var matches = Regex.Matches(source, pattern);

            var violatingMethods = matches
                .Select(m => m.Groups[1].Value)
                .Where(name => !Whitelist.Contains(name))
                .ToList();

            violatingMethods.Should().BeEmpty(
                $"RimMindAPI must not contain 'internal static void Set*' backdoor methods. " +
                $"Violating methods: {string.Join(", ", violatingMethods)}");
        }

        private static string FindFileUpwards(string fileName)
        {
            var dir = Path.GetDirectoryName(typeof(NoSetBackdoorTests).Assembly.Location);
            while (dir != null)
            {
                var candidate = Path.Combine(dir, "RimMind-Core", "Source", fileName);
                if (File.Exists(candidate)) return candidate;

                candidate = Path.Combine(dir, "Source", fileName);
                if (File.Exists(candidate)) return candidate;

                dir = Directory.GetParent(dir)?.FullName;
            }
            return fileName;
        }
    }
}
