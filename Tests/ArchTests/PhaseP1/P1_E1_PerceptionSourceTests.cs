using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-E1: IPerceptionSource Registration Mechanism.
    /// Verifies that the perception source extension points exist,
    /// built-in sources are implemented, PawnPerceiver uses the registry,
    /// and RimMindAPI.Perception.Sources facade is available.
    /// </summary>
    public class P1_E1_PerceptionSourceTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SourceDir = Path.Combine(ProjectRoot, "Source");

        [Fact]
        public void IPerceptionSource_Interface_Exists()
        {
            var path = Path.Combine(SourceDir, "Application", "Common", "Interfaces", "Agent", "Perception", "IPerceptionSource.cs");
            Assert.True(File.Exists(path), "IPerceptionSource.cs should exist");
        }

        [Fact]
        public void IPerceptionSourceRegistry_Interface_Exists()
        {
            var path = Path.Combine(SourceDir, "Application", "Common", "Interfaces", "Agent", "Perception", "IPerceptionSourceRegistry.cs");
            Assert.True(File.Exists(path), "IPerceptionSourceRegistry.cs should exist");
        }

        [Fact]
        public void IPerceptionSource_Extends_IExtension()
        {
            var path = Path.Combine(SourceDir, "Application", "Common", "Interfaces", "Agent", "Perception", "IPerceptionSource.cs");
            Assert.True(File.Exists(path), "IPerceptionSource.cs should exist");
            var content = File.ReadAllText(path);
            Assert.Contains("IExtension", content);
            Assert.Contains("ShouldSense", content);
            Assert.Contains("Sense", content);
            Assert.Contains("Priority", content);
        }

        [Fact]
        public void Six_Builtin_PerceptionSources_Exist()
        {
            var perceptionDir = Path.Combine(SourceDir, "Presentation", "Agent", "Perception");
            Assert.True(Directory.Exists(perceptionDir), "Perception directory should exist");
            var files = Directory.GetFiles(perceptionDir, "*PerceptionSource.cs");
            Assert.True(files.Length >= 6, $"Expected at least 6 perception sources, found {files.Length}");

            var names = files.Select(f => Path.GetFileName(f)).ToList();
            Assert.Contains("MoodPerceptionSource.cs", names);
            Assert.Contains("HealthPerceptionSource.cs", names);
            Assert.Contains("CombatPerceptionSource.cs", names);
            Assert.Contains("NeedPerceptionSource.cs", names);
            Assert.Contains("SocialPerceptionSource.cs", names);
            Assert.Contains("EnvironmentPerceptionSource.cs", names);
        }

        [Fact]
        public void PawnPerceiver_Uses_IPerceptionSourceRegistry()
        {
            var path = Path.Combine(SourceDir, "Presentation", "Agent", "PawnPerceiver.cs");
            Assert.True(File.Exists(path), "PawnPerceiver.cs should exist");
            var content = File.ReadAllText(path);
            Assert.Contains("IPerceptionSource", content);
            Assert.Contains("_sourceRegistry", content);
            Assert.Contains("SenseInline", content);
        }

        [Fact]
        public void RimMindAPI_Has_Perception_Sources()
        {
            var path = Path.Combine(SourceDir, "Presentation", "Api", "RimMindAPI.Perception.cs");
            Assert.True(File.Exists(path), "RimMindAPI.Perception.cs should exist");
            var content = File.ReadAllText(path);
            Assert.Contains("Sources", content);
            Assert.Contains("IPerceptionSource", content);
        }

        [Fact]
        public void PawnAgentFactory_Passes_PerceptionSourceRegistry()
        {
            var path = Path.Combine(SourceDir, "Presentation", "Agent", "PawnAgentFactory.cs");
            Assert.True(File.Exists(path), "PawnAgentFactory.cs should exist");
            var content = File.ReadAllText(path);
            Assert.Contains("IPerceptionSource", content);
            Assert.Contains("_perceptionSourceRegistry", content);
        }

        [Fact]
        public void MoodPerceptionSource_Implements_IPerceptionSource()
        {
            var path = Path.Combine(SourceDir, "Presentation", "Agent", "Perception", "MoodPerceptionSource.cs");
            Assert.True(File.Exists(path), "MoodPerceptionSource.cs should exist");
            var content = File.ReadAllText(path);
            Assert.Contains("IPerceptionSource", content);
            Assert.Contains("Priority", content);
            Assert.Contains("ShouldSense", content);
            Assert.Contains("Sense", content);
        }
    }
}
