using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseA
{
    public class NoPrivateStaticFieldsTests
    {
        private static readonly HashSet<string> Whitelist = new()
        {
            "_isShutdown",
            "_callbackCounter"
        };

        [Fact]
        [Trait("Phase", "A")]
        public void R_A3_RimMindAPI_ShouldNotHave_PrivateStaticFields()
        {
            var sourcePath = FindFileUpwards("RimMindAPI.cs");

            File.Exists(sourcePath).Should().BeTrue("RimMindAPI.cs source file must exist for analysis");

            var source = File.ReadAllText(sourcePath);
            var pattern = @"private\s+static\s+(?:readonly\s+)?[\w<>\[\],\s\?]+?\s+(_\w+)\s*[=;]";
            var matches = Regex.Matches(source, pattern);

            var violatingFields = matches
                .Select(m => m.Groups[1].Value)
                .Where(name => !Whitelist.Contains(name))
                .ToList();

            violatingFields.Should().BeEmpty(
                $"RimMindAPI must not contain private static fields beyond whitelist {string.Join(", ", Whitelist)}. " +
                $"Violating fields: {string.Join(", ", violatingFields)}");
        }

        private static string FindFileUpwards(string fileName)
        {
            var dir = Path.GetDirectoryName(typeof(NoPrivateStaticFieldsTests).Assembly.Location);
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
