using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseL
{
    public sealed class R_L2_PublicDebugApiBoundaryTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        private static string ReadSource(string relativePath)
            => File.ReadAllText(Path.Combine(SourceDir, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        [Fact]
        [Trait("Phase", "L")]
        public void RimMindAPI_Should_Expose_Debug_Window_Openers_For_Submods()
        {
            string content = ReadSource("Presentation/Api/RimMindAPI.Debug.cs");

            content.Should().Contain("public static class Debug");
            content.Should().Contain("OpenAIRequests");
            content.Should().Contain("RimMindRuntime.Instance.WindowService?.OpenAIRequests()");
            content.Should().NotContain("Window_RimMindHub.OpenAIRequests()");
            content.Should().NotContain("Find.WindowStack");
        }

        [Fact]
        [Trait("Phase", "L")]
        public void RimMindAPI_Should_Expose_ScopedAgent_Control_For_Submods()
        {
            string content = ReadSource("Presentation/Api/RimMindAPI.Agents.cs");

            content.Should().Contain("public static class Agents");
            content.Should().Contain("FindScoped");
            content.Should().Contain("GetOrCreateScoped");
            content.Should().Contain("StartScoped");
            content.Should().Contain("PauseScoped");
            content.Should().Contain("ForceThinkScoped");
            content.Should().Contain("IScopedAgentManager");
        }
    }
}
