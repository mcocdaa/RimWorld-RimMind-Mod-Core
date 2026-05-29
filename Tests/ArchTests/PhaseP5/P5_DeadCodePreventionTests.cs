using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP5
{
    public class P5_DeadCodePreventionTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void No_Unused_LayerSafe_Methods()
        {
            var sourceFile = Directory.GetFiles(ProjectRoot, "ContextOrchestrator.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Context"))
                ?? throw new FileNotFoundException("ContextOrchestrator.cs not found");

            var content = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("LayerSafe", content);
        }

        [Fact]
        public void No_Downcast_In_RimMindAPI_Chat()
        {
            var sourceFile = Directory.GetFiles(ProjectRoot, "RimMindAPI.Chat.cs", SearchOption.AllDirectories)
                .FirstOrDefault()
                ?? throw new FileNotFoundException("RimMindAPI.Chat.cs not found");

            var content = File.ReadAllText(sourceFile);
            Assert.DoesNotContain("is GameContextBuilder", content);
        }

        [Fact]
        public void SummaryConversationCompactor_Has_Todo_Marker()
        {
            var sourceFile = Directory.GetFiles(ProjectRoot, "SummaryConversationCompactor.cs", SearchOption.AllDirectories)
                .FirstOrDefault()
                ?? throw new FileNotFoundException("SummaryConversationCompactor.cs not found");

            var content = File.ReadAllText(sourceFile);
            Assert.Contains("TODO", content);
            Assert.Contains("Integrate", content);
        }

        [Fact]
        public void IGameContextBuilder_Has_BuildMapContextInstance()
        {
            var sourceFile = Directory.GetFiles(ProjectRoot, "IGameContextBuilder.cs", SearchOption.AllDirectories)
                .FirstOrDefault()
                ?? throw new FileNotFoundException("IGameContextBuilder.cs not found");

            var content = File.ReadAllText(sourceFile);
            Assert.Contains("BuildMapContextInstance", content);
        }
    }
}
