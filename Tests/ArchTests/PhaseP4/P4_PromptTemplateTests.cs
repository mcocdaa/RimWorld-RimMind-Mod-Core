using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_PromptTemplateTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static string GetContextLayerBuilderSource()
        {
            var file = Directory.GetFiles(ProjectRoot, "ContextLayerBuilder.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Application") && f.Contains("Context"))
                ?? throw new FileNotFoundException("ContextLayerBuilder.cs not found");
            return File.ReadAllText(file);
        }

        [Fact]
        public void L0_Static_Layer_ContainsRoleDefinition()
        {
            var content = GetContextLayerBuilderSource();
            Assert.Contains("L0_Static", content);
            Assert.Contains("BuildLayer", content);
        }

        [Fact]
        public void L0_Static_Layer_ContainsOutputFormat()
        {
            var source = GetContextLayerBuilderSource();
            Assert.True(source.Contains("Action") || source.Contains("action"),
                "L0 Static layer source should reference Action output format");
        }

        [Fact]
        public void ContextLayerBuilder_HasBuildMethod_ForL0()
        {
            var content = GetContextLayerBuilderSource();
            Assert.Contains("BuildL0", content);
        }

        [Fact]
        public void AllLayerMethods_UseXmlTags()
        {
            var content = GetContextLayerBuilderSource();
            Assert.Contains("<layer_", content);
            Assert.Contains("</layer_", content);
            Assert.Contains("L0_Static", content);
            Assert.Contains("L1", content);
            Assert.Contains("L2", content);
            Assert.Contains("L3", content);
            Assert.Contains("L5", content);
        }
    }
}
