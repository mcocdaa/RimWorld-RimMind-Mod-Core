using System;
using System.IO;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseN
{
    /// <summary>
    /// R_N5: ICompositeToolCall interface exists in the Application layer,
    /// under the RimMind.Application.Common.Interfaces.Tools namespace.
    /// This interface enables composite tool call orchestration for the Actions module.
    /// </summary>
    public class R_N5_CompositeToolCallInterfaceExists
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string CompositeToolCallPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Common", "Interfaces", "Tools",
            "ICompositeToolCall.cs");

        [Fact]
        public void ICompositeToolCall_File_Exists()
        {
            Assert.True(File.Exists(CompositeToolCallPath),
                "ICompositeToolCall.cs must exist in Application/Common/Interfaces/Tools/");
        }

        [Fact]
        public void ICompositeToolCall_Has_Correct_Namespace()
        {
            Assert.True(File.Exists(CompositeToolCallPath), "ICompositeToolCall.cs must exist");

            var content = File.ReadAllText(CompositeToolCallPath);

            Assert.Contains("namespace RimMind.Application.Common.Interfaces.Tools", content);
        }

        [Fact]
        public void ICompositeToolCall_Extends_IToolHandler()
        {
            Assert.True(File.Exists(CompositeToolCallPath), "ICompositeToolCall.cs must exist");

            var content = File.ReadAllText(CompositeToolCallPath);

            Assert.Contains("interface ICompositeToolCall", content);
            Assert.Contains("IToolHandler", content);
        }

        [Fact]
        public void ICompositeToolCall_Has_RequiredToolIds()
        {
            Assert.True(File.Exists(CompositeToolCallPath), "ICompositeToolCall.cs must exist");

            var content = File.ReadAllText(CompositeToolCallPath);

            Assert.Contains("RequiredToolIds", content);
            Assert.Contains("IReadOnlyList<string>", content);
        }
    }
}
