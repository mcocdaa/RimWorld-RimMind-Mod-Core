using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP7
{
    public class P7A_CoreIconEntryTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        [Fact]
        public void CoreIcon_PlainClick_Uses_RequestOverlay_Toggle()
        {
            string content = ReadSource("Infrastructure/Patches/RimMindPlaySettingsPatch.cs");

            Assert.Contains("ToggleCoreOverlay", content);
            Assert.Contains("toggle.Id == \"request_overlay\"", content);
            Assert.Contains("toggle.Toggle()", content);
        }

        [Fact]
        public void CoreIcon_Shift_And_Ctrl_Do_Not_Toggle_Overlay()
        {
            string content = ReadSource("Infrastructure/Patches/RimMindPlaySettingsPatch.cs");

            int shiftIndex = content.IndexOf("if (shift)", StringComparison.Ordinal);
            int ctrlIndex = content.IndexOf("else if (control)", StringComparison.Ordinal);
            // Skip past "else if (control)" to find the final else branch
            int afterCtrl = ctrlIndex + "else if (control)".Length;
            int plainIndex = content.IndexOf("else", afterCtrl, StringComparison.Ordinal);

            Assert.True(shiftIndex >= 0, "Shift branch must exist.");
            Assert.True(ctrlIndex > shiftIndex, "Ctrl branch must follow shift branch.");
            Assert.True(plainIndex > afterCtrl, "Plain click else branch must follow Ctrl branch.");

            string shiftBlock = content.Substring(shiftIndex, ctrlIndex - shiftIndex);
            string ctrlBlock = content.Substring(ctrlIndex, plainIndex - ctrlIndex);

            Assert.DoesNotContain("ToggleCoreOverlay", shiftBlock);
            Assert.DoesNotContain("ToggleCoreOverlay", ctrlBlock);
        }

        [Fact]
        public void CoreOverlayToggle_Persists_RequestOverlayEnabled()
        {
            string content = ReadSource("AICoreMod.cs");

            Assert.Contains("internal sealed class CoreOverlayToggle", content);
            Assert.Contains("public string Id => \"request_overlay\"", content);
            Assert.Contains("_settings.RequestOverlayEnabled = !_settings.RequestOverlayEnabled;", content);
            Assert.Contains("_settings.Persist()", content);
        }

        [Fact]
        public void RequestOverlay_Position_Save_Has_Persist_Hook()
        {
            string content = ReadSource("Infrastructure/UI/RequestOverlay.cs");

            Assert.Contains("SavePositionToSettings", content);
            Assert.Contains("RequestOverlayX", content);
            Assert.Contains("RequestOverlayY", content);
            Assert.Contains("RequestOverlayW", content);
            Assert.Contains("RequestOverlayH", content);
            Assert.Contains("s.Persist()", content);
        }

        [Fact]
        public void IOverlaySettings_Defines_Persist_Method()
        {
            string content = ReadSource("Application/Common/Interfaces/Internal/IOverlaySettings.cs");

            Assert.Contains("void Persist()", content);
        }
    }
}
