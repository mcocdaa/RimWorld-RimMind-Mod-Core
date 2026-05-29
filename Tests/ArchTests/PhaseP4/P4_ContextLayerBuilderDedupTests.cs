using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_ContextLayerBuilderDedupTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static string ReadContextLayerBuilder()
        {
            return File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "ContextLayerBuilder.cs", SearchOption.AllDirectories)
                .First(f => f.Contains("Application") && f.Contains("Context")));
        }

        [Fact]
        public void ContextLayerBuilder_HasGenericBuildLayerMethod()
        {
            var content = ReadContextLayerBuilder();
            Assert.Contains("BuildLayer(", content);
        }

        [Fact]
        public void BuildL0_DelegatesToBuildLayer()
        {
            var content = ReadContextLayerBuilder();
            Assert.Matches(@"BuildL0\([^)]*\)\s*=>\s*BuildLayer\(", content);
        }

        [Fact]
        public void BuildL1_DelegatesToBuildLayer()
        {
            var content = ReadContextLayerBuilder();
            Assert.Matches(@"BuildL1\([^)]*\)\s*=>\s*BuildLayer\(", content);
        }

        [Fact]
        public void BuildContextLayer_DelegatesToBuildLayer()
        {
            var content = ReadContextLayerBuilder();
            Assert.Matches(@"BuildContextLayer\([^)]*\)\s*=>\s*BuildLayer\(", content);
        }

        [Fact]
        public void BuildL3_DelegatesToBuildLayer()
        {
            var content = ReadContextLayerBuilder();
            Assert.Matches(@"BuildL3\([^)]*\)\s*=>\s*BuildLayer\(", content);
        }

        [Fact]
        public void BuildL5_DelegatesToBuildLayer()
        {
            var content = ReadContextLayerBuilder();
            Assert.Matches(@"BuildL5\([^)]*\)\s*=>\s*BuildLayer\(", content);
        }

        [Fact]
        public void BuildLayer_AcceptsXmlTagAndLayerTag()
        {
            var content = ReadContextLayerBuilder();
            Assert.Matches(@"BuildLayer\(.*xmlTag.*layerTag|BuildLayer\(.*layerTag.*xmlTag", content);
        }

        [Fact]
        public void NoDuplicateForeachLoopInBuildMethods()
        {
            var content = ReadContextLayerBuilder();
            var methodNames = new[] { "BuildL0", "BuildL1", "BuildContextLayer", "BuildL3", "BuildL5" };
            foreach (var method in methodNames)
            {
                var pattern = $@"public\s+ChatMessage\?\s+{Regex.Escape(method)}\s*\([^)]*\)\s*=>\s*BuildLayer\(";
                Assert.Matches(pattern, content);
            }
        }
    }
}
