using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_ToolSchemaTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void MechanismToolHandler_BuildsParameterSchema()
        {
            var file = Directory.GetFiles(ProjectRoot, "MechanismToolHandler.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException("MechanismToolHandler.cs not found");
            var content = File.ReadAllText(file);
            Assert.Contains("BuildParameterSchema", content);
        }

        [Fact]
        public void MechanismToolHandler_SchemaContainsTypeObject()
        {
            var file = Directory.GetFiles(ProjectRoot, "MechanismToolHandler.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException("MechanismToolHandler.cs not found");
            var content = File.ReadAllText(file);
            Assert.True(content.Contains("type") && content.Contains("object"),
                "Tool schema should contain type: object");
        }

        [Fact]
        public void MechanismToolHandler_SchemaContainsProperties()
        {
            var file = Directory.GetFiles(ProjectRoot, "MechanismToolHandler.cs", SearchOption.AllDirectories)
                .FirstOrDefault() ?? throw new FileNotFoundException("MechanismToolHandler.cs not found");
            var content = File.ReadAllText(file);
            Assert.Contains("properties", content);
        }
    }
}
