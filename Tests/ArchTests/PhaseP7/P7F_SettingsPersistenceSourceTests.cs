using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP7
{
    public class P7F_SettingsPersistenceSourceTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        [Fact]
        public void Settings_Window_Persists_On_Close()
        {
            string content = ReadSource("Infrastructure/UI/Window_RimMindSettings.cs");

            Assert.Contains("public override void PreClose()", content);
            Assert.Contains("_settingsProvider.Persist();", content);
            Assert.Contains("base.PreClose();", content);
        }

        [Fact]
        public void Connection_Test_Persists_Normalized_Settings_Before_Client_Rebuild()
        {
            string content = ReadSource("Presentation/UI/ApiTabDrawer.TestConnection.cs");
            int normalizeIndex = content.IndexOf("NormalizeConnectionSettings(s);", StringComparison.Ordinal);
            int persistIndex = content.IndexOf("s.Persist();", StringComparison.Ordinal);
            int invalidateIndex = content.IndexOf("GetClientManager()?.InvalidateCache();", StringComparison.Ordinal);

            Assert.True(normalizeIndex >= 0, "Connection test must normalize typed settings first.");
            Assert.True(persistIndex > normalizeIndex, "Connection test must persist normalized API settings.");
            Assert.True(invalidateIndex > persistIndex, "Client cache must rebuild after persisted settings are current.");
        }

        [Fact]
        public void Provider_Label_Normalizes_Provider_Id_For_Localization()
        {
            string content = ReadSource("Presentation/UI/ApiTabDrawer.TestConnection.cs");

            Assert.Contains("NormalizeProviderTranslationSuffix", content);
            Assert.Contains("\"openai\" => \"OpenAI\"", content);
            Assert.Contains("\"player2\" => \"Player2\"", content);
        }
    }
}
