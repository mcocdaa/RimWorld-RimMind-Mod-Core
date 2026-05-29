using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP5
{
    public class P5_ToolSchemaQualityTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static string ReadSource(string fileName)
        {
            var file = Directory.GetFiles(ProjectRoot, fileName, SearchOption.AllDirectories)
                .FirstOrDefault(f => !f.Contains("backup") && !f.Contains("obj"))
                ?? throw new FileNotFoundException($"{fileName} not found");
            return File.ReadAllText(file);
        }

        [Fact]
        public void MechanismToolHandler_Has_Description_Generation()
        {
            var content = ReadSource("MechanismToolHandler.cs");

            Assert.Contains("description", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Description", content);
        }

        [Fact]
        public void Tool_Definitions_Include_Parameter_Info()
        {
            var content = ReadSource("MechanismToolHandler.cs");

            Assert.Contains("parameters", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Properties", content);
        }

        [Fact]
        public void Schema_Generation_Uses_Reflection()
        {
            var content = ReadSource("MechanismToolHandler.cs");

            bool hasSchemaGeneration = content.Contains("GetProperty", StringComparison.OrdinalIgnoreCase)
                || content.Contains("GetProperties", StringComparison.OrdinalIgnoreCase)
                || content.Contains("BuildParameterSchema");

            Assert.True(hasSchemaGeneration,
                "MechanismToolHandler must use reflection or BuildParameterSchema for schema generation");
        }
    }
}
