using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP5
{
    public class P5_LayerCorrectnessTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void ThinkContextEnricher_In_Application_Layer()
        {
            var files = Directory.GetFiles(ProjectRoot, "ThinkContextEnricher.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("backup") && !f.Contains("obj"))
                .ToList();

            Assert.NotEmpty(files);

            bool foundInApplicationLayer = files.Any(f =>
            {
                var content = File.ReadAllText(f);
                return content.Contains("Application.Features.Agent");
            });

            Assert.True(foundInApplicationLayer,
                "At least one ThinkContextEnricher.cs must have namespace containing 'Application.Features.Agent'");
        }

        [Fact]
        public void IEnvelopeEnricher_Interface_Exists()
        {
            var file = Directory.GetFiles(ProjectRoot, "IEnvelopeEnricher.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => !f.Contains("backup") && !f.Contains("obj"));

            Assert.NotNull(file);
        }

        [Fact]
        public void EnvelopeEnrichmentCoordinator_Exists()
        {
            var file = Directory.GetFiles(ProjectRoot, "EnvelopeEnrichmentCoordinator.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => !f.Contains("backup") && !f.Contains("obj"));

            Assert.NotNull(file);
        }

        [Fact]
        public void ProactiveBehaviorOrchestrator_In_Application_Layer()
        {
            var file = Directory.GetFiles(ProjectRoot, "ProactiveBehaviorOrchestrator.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => !f.Contains("backup") && !f.Contains("obj"));

            Assert.NotNull(file);

            var content = File.ReadAllText(file);
            Assert.Contains("Application.Features.Agent", content);
        }

        [Fact]
        public void IProactiveBehaviorOrchestrator_Interface_Exists()
        {
            var file = Directory.GetFiles(ProjectRoot, "IProactiveBehaviorOrchestrator.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => !f.Contains("backup") && !f.Contains("obj"));

            Assert.NotNull(file);
        }
    }
}
