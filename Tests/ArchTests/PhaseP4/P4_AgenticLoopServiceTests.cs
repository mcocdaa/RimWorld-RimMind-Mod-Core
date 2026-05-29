using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_AgenticLoopServiceTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void IAgenticLoopService_Exists()
        {
            var file = Directory.GetFiles(ProjectRoot, "IAgenticLoopService.cs", SearchOption.AllDirectories)
                .FirstOrDefault();
            Assert.NotNull(file);
        }

        [Fact]
        public void AgenticLoopService_ImplementsInterface()
        {
            var file = Directory.GetFiles(ProjectRoot, "AgenticLoopService.cs", SearchOption.AllDirectories)
                .FirstOrDefault();
            Assert.NotNull(file);
            var content = File.ReadAllText(file);
            Assert.Contains("IAgenticLoopService", content);
        }

        [Fact]
        public void DecisionProcessor_UsesAgenticLoopService()
        {
            var file = Directory.GetFiles(ProjectRoot, "DecisionProcessor.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException();
            var content = File.ReadAllText(file);
            Assert.Contains("IAgenticLoopService", content);
        }

        [Fact]
        public void AgenticLoopService_HasMaxDepth()
        {
            var file = Directory.GetFiles(ProjectRoot, "IAgenticLoopService.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException();
            var content = File.ReadAllText(file);
            Assert.Contains("MaxDepth", content);
        }

        [Fact]
        public void AgenticLoopService_HasShouldContinue()
        {
            var file = Directory.GetFiles(ProjectRoot, "IAgenticLoopService.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException();
            var content = File.ReadAllText(file);
            Assert.Contains("ShouldContinue", content);
        }

        [Fact]
        public void LoopResult_Exists()
        {
            var file = Directory.GetFiles(ProjectRoot, "IAgenticLoopService.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? Directory.GetFiles(ProjectRoot, "AgenticLoopService.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException();
            var content = File.ReadAllText(file);
            Assert.Contains("LoopResult", content);
        }
    }
}
