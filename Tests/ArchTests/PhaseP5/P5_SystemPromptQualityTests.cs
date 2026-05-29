using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP5
{
    public class P5_SystemPromptQualityTests
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
        public void L0_Layer_Source_Contains_Role_Definition()
        {
            var builderContent = ReadSource("ContextLayerBuilder.cs");

            Assert.Contains("L0_Static", builderContent);

            bool hasRoleOrIdentity = builderContent.Contains("role", StringComparison.OrdinalIgnoreCase)
                || builderContent.Contains("identity", StringComparison.OrdinalIgnoreCase);
            Assert.True(hasRoleOrIdentity,
                "ContextLayerBuilder source must contain 'role' or 'identity' for L0 layer role definition");
        }

        [Fact]
        public void L0_Layer_Source_Contains_Output_Format()
        {
            var builderContent = ReadSource("ContextLayerBuilder.cs");

            bool hasAction = builderContent.Contains("Action", StringComparison.OrdinalIgnoreCase)
                || builderContent.Contains("action", StringComparison.OrdinalIgnoreCase);

            if (!hasAction)
            {
                var providerContent = ReadSource("CoreContextProviders.cs");
                hasAction = providerContent.Contains("Action", StringComparison.OrdinalIgnoreCase)
                    || providerContent.Contains("action", StringComparison.OrdinalIgnoreCase);
            }

            Assert.True(hasAction,
                "L0 layer source must contain 'Action' or 'action' for output format definition");

            bool hasSchema = builderContent.Contains("schema", StringComparison.OrdinalIgnoreCase);
            if (!hasSchema)
            {
                var providerContent = ReadSource("CoreContextProviders.cs");
                hasSchema = providerContent.Contains("schema", StringComparison.OrdinalIgnoreCase);
            }

            Assert.True(hasSchema,
                "L0 layer source must contain 'schema' for output format definition");
        }

        [Fact]
        public void System_Prompt_Has_Structured_Sections()
        {
            var providerContent = ReadSource("CoreContextProviders.cs");
            var builderContent = ReadSource("ContextLayerBuilder.cs");

            bool hasXmlMarkers = builderContent.Contains("<layer_")
                || providerContent.Contains("<role", StringComparison.OrdinalIgnoreCase)
                || providerContent.Contains("<capabilities", StringComparison.OrdinalIgnoreCase)
                || providerContent.Contains("<constraints", StringComparison.OrdinalIgnoreCase);

            Assert.True(hasXmlMarkers,
                "L0 context source must contain XML-like section markers for structured prompts");
        }

        [Fact]
        public void Context_Layers_Use_Xml_Wrapping()
        {
            var content = ReadSource("ContextLayerBuilder.cs");

            Assert.Contains("layer_", content);
        }
    }
}
