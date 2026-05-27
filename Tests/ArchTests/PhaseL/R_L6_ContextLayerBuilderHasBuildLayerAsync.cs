using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseL
{
    public class R_L6_ContextLayerBuilderHasBuildLayerAsync
    {
        private static readonly string RepoRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

        private static readonly string LayerBuilderPath = Path.Combine(
            RepoRoot, "RimMind-Core", "Source", "Application", "Features", "Context", "ContextLayerBuilder.cs");

        [Fact]
        public void ContextLayerBuilder_Contains_BuildLayerAsync_Method()
        {
            Assert.True(File.Exists(LayerBuilderPath), "ContextLayerBuilder.cs must exist");

            var content = File.ReadAllText(LayerBuilderPath);

            Assert.Contains("BuildLayerAsync", content);
            Assert.Contains("async", content);
            Assert.Contains("Task<List<ContextEntry>>", content);
        }

        [Fact]
        public void BuildLayerAsync_Uses_ContextProviderDef_Path()
        {
            Assert.True(File.Exists(LayerBuilderPath), "ContextLayerBuilder.cs must exist");

            var content = File.ReadAllText(LayerBuilderPath);

            Assert.Contains("ContextProviderDef", content);
            Assert.Contains("key.Def is ContextProviderDef", content);
        }

        [Fact]
        public void BuildLayerAsync_Has_ValueProvider_Fallback()
        {
            Assert.True(File.Exists(LayerBuilderPath), "ContextLayerBuilder.cs must exist");

            var content = File.ReadAllText(LayerBuilderPath);

            Assert.Contains("ValueProvider", content);
        }

        [Fact]
        public void BuildLayerAsync_Accepts_ProviderCache_And_CancellationToken()
        {
            Assert.True(File.Exists(LayerBuilderPath), "ContextLayerBuilder.cs must exist");

            var content = File.ReadAllText(LayerBuilderPath);

            Assert.Contains("ProviderCache", content);
            Assert.Contains("CancellationToken", content);
        }
    }
}
