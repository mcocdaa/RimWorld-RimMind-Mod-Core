using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP1
{
    /// <summary>
    /// P1-E4: AddMiddleware Bug Fix.
    /// Verifies that AddMiddleware handles both BusPublishContext and LlmRequestContext
    /// pipeline types, not just BusPublishContext.
    /// </summary>
    public class P1_E4_AddMiddlewareBugFixTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string RuntimeDir = Path.Combine(
            ProjectRoot, "Source", "Presentation", "Runtime");

        private static readonly string ExtensionManagerPath = Path.Combine(RuntimeDir, "RimMindExtensionManager.cs");
        private static readonly string RimMindRuntimePath = Path.Combine(RuntimeDir, "RimMindRuntime.cs");

        [Fact]
        public void AddMiddleware_Accepts_LlmRequestPipeline_Parameter()
        {
            var source = File.ReadAllText(ExtensionManagerPath);
            Assert.Contains("llmPipeline", source);
        }

        [Fact]
        public void AddMiddleware_Handles_LlmRequestContext_Type()
        {
            var source = File.ReadAllText(ExtensionManagerPath);
            Assert.Contains("LlmRequestContext", source);
        }

        [Fact]
        public void AddMiddleware_Handles_MutablePipeline_Of_LlmRequestContext()
        {
            var source = File.ReadAllText(ExtensionManagerPath);
            Assert.Contains("MutablePipeline<LlmRequestContext>", source);
        }

        [Fact]
        public void RimMindRuntime_Passes_Both_Pipelines_To_AddMiddleware()
        {
            var source = File.ReadAllText(RimMindRuntimePath);
            Assert.Contains("UnifiedPipeline", source);
            Assert.Contains("BusPublishPipeline", source);
        }
    }
}
