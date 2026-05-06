using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseB
{
    public class NoLegacyRegistrationTests
    {
        private static readonly string[] LegacyMethodNames = new[]
        {
            "RegisterSettingsTab",
            "RegisterToggleBehavior",
            "RegisterDialogueSkipCheck",
            "RegisterFloatMenuSkipCheck",
            "RegisterActionSkipCheck",
            "RegisterStorytellerIncidentSkipCheck",
            "RegisterModCooldown",
            "RegisterDialogueTrigger",
            "RegisterIncidentExecutedCallback"
        };

        private static readonly string[] LegacyPropertyNames = new[]
        {
            "SettingsTabs",
            "IsAnyToggleActive",
            "ToggleAll",
            "HasToggleBehaviors",
            "GetModCooldownGetter",
            "ModCooldownGetters",
            "UnregisterDialogueSkipCheck",
            "UnregisterFloatMenuSkipCheck",
            "UnregisterActionSkipCheck",
            "UnregisterStorytellerIncidentSkipCheck",
            "UnregisterIncidentExecutedCallback"
        };

        [Fact]
        [Trait("Phase", "B")]
        public void R_B1_RimMindAPI_ShouldNotContain_LegacyRegistrationMethods()
        {
            var sourcePath = FindFileUpwards("RimMindAPI.cs");
            File.Exists(sourcePath).Should().BeTrue("RimMindAPI.cs source file must exist for analysis");

            var source = File.ReadAllText(sourcePath);
            var violatingEntries = new List<string>();

            foreach (var methodName in LegacyMethodNames)
            {
                var pattern = $@"\b{Regex.Escape(methodName)}\s*\(";
                if (Regex.IsMatch(source, pattern))
                {
                    violatingEntries.Add($"method: {methodName}");
                }
            }

            foreach (var propName in LegacyPropertyNames)
            {
                var pattern = $@"\b{Regex.Escape(propName)}\b";
                if (Regex.IsMatch(source, pattern))
                {
                    violatingEntries.Add($"property/field: {propName}");
                }
            }

            violatingEntries.Should().BeEmpty(
                $"RimMindAPI must not contain any of the 9 legacy stringly-typed registration methods or their associated properties. " +
                $"These should be replaced by RimMindAPI.Extensions<T>().Register(impl). " +
                $"Violating entries:\n  {string.Join("\n  ", violatingEntries)}");
        }

        private static string FindFileUpwards(string fileName)
        {
            var dir = Path.GetDirectoryName(typeof(NoLegacyRegistrationTests).Assembly.Location);
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
