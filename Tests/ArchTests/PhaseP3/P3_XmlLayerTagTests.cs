using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP3
{
    public class P3_XmlLayerTagTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string SrcRoot = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Context");

        [Fact]
        public void ContextLayerBuilder_EntriesToLayerMessage_WrapsContentInXmlTags()
        {
            var code = File.ReadAllText(Path.Combine(SrcRoot, "ContextLayerBuilder.cs"));
            Assert.Contains("EntriesToLayerMessage", code);
            Assert.Contains("<layer_", code);
            Assert.Contains("</layer_", code);
        }

        [Fact]
        public void ContextLayerBuilder_BuildL0_ContainsL0Tag()
        {
            var code = File.ReadAllText(Path.Combine(SrcRoot, "ContextLayerBuilder.cs"));
            Assert.Contains("L0_Static", code);
            Assert.Contains("BuildLayer(", code);
            Assert.Matches(@"BuildL0\([^)]*\)\s*=>\s*BuildLayer\(", code);
        }

        [Fact]
        public void ContextLayerBuilder_BuildL1_ContainsL1Tag()
        {
            var code = File.ReadAllText(Path.Combine(SrcRoot, "ContextLayerBuilder.cs"));
            Assert.Matches(@"BuildL1\([^)]*\)\s*=>\s*BuildLayer\(\s*keys\s*,\s*""L1""", code);
        }

        [Fact]
        public void ContextLayerBuilder_BuildL2_ContainsL2Tag()
        {
            var code = File.ReadAllText(Path.Combine(SrcRoot, "ContextLayerBuilder.cs"));
            Assert.Matches(@"BuildContextLayer\([^)]*\)\s*=>\s*BuildLayer\(\s*keys\s*,\s*""L2""", code);
        }

        [Fact]
        public void ContextLayerBuilder_BuildL3_ContainsL3Tag()
        {
            var code = File.ReadAllText(Path.Combine(SrcRoot, "ContextLayerBuilder.cs"));
            Assert.Matches(@"BuildL3\([^)]*\)\s*=>\s*BuildLayer\(\s*keys\s*,\s*""L3""", code);
        }

        [Fact]
        public void ContextLayerBuilder_BuildL5_ContainsL5Tag()
        {
            var code = File.ReadAllText(Path.Combine(SrcRoot, "ContextLayerBuilder.cs"));
            Assert.Matches(@"BuildL5\([^)]*\)\s*=>\s*BuildLayer\(\s*keys\s*,\s*""L5""", code);
        }

        [Fact]
        public void ContextLayerBuilder_NoRawBracketKeyFormat_Removed()
        {
            var code = File.ReadAllText(Path.Combine(SrcRoot, "ContextLayerBuilder.cs"));
            var buildLayerIdx = code.IndexOf("public ChatMessage? BuildLayer(", StringComparison.Ordinal);
            Assert.True(buildLayerIdx >= 0, "BuildLayer method not found");
            var braceStart = code.IndexOf('{', buildLayerIdx);
            var braceCount = 0;
            var methodEnd = braceStart;
            for (var i = braceStart; i < code.Length; i++)
            {
                if (code[i] == '{') braceCount++;
                else if (code[i] == '}') { braceCount--; if (braceCount == 0) { methodEnd = i; break; } }
            }
            var methodBody = code.Substring(buildLayerIdx, methodEnd - buildLayerIdx);
            Assert.True(methodBody.Contains("<layer_"),
                "BuildLayer uses key bracket format but has no <layer_ XML wrapper");
        }
    }
}
