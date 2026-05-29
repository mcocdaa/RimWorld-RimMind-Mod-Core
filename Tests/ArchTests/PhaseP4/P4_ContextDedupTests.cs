using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_ContextDedupTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void ContextBuildMiddleware_ChecksGameStateInfo_ForSkipLayers()
        {
            var file = Directory.GetFiles(ProjectRoot, "ContextBuildMiddleware.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Pipeline") && f.Contains("Unified"))
                ?? throw new FileNotFoundException("ContextBuildMiddleware.cs not found");

            var content = File.ReadAllText(file);
            Assert.Contains("GameStateInfo", content);
            Assert.Contains("skipLayer", content);
        }

        [Fact]
        public void IContextEngine_AcceptsSkipLayersParameter()
        {
            var file = Directory.GetFiles(ProjectRoot, "IContextBuilder.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException("IContextBuilder.cs not found");

            var content = File.ReadAllText(file);
            Assert.Matches(@"skipLayer|SkipLayer", content);
        }

        [Fact]
        public void ContextOrchestrator_UsesSkipLayers_InBuildSnapshot()
        {
            var file = Directory.GetFiles(ProjectRoot, "ContextOrchestrator.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException("ContextOrchestrator.cs not found");

            var content = File.ReadAllText(file);
            Assert.Matches(@"skipLayer|SkipLayer", content);
        }
    }
}
