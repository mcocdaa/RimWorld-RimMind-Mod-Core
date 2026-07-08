using System;
using System.IO;
using RimMind.Application.Features.Pipeline.Unified;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP3
{
    public class P3_OutputGuardrailTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string PipelineDir = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Pipeline", "Unified");

        [Fact]
        public void OutputGuardrailMiddleware_File_Exists()
        {
            var path = Path.Combine(PipelineDir, "OutputGuardrailMiddleware.cs");
            Assert.True(File.Exists(path), "OutputGuardrailMiddleware.cs does not exist");
        }

        [Fact]
        public void OutputGuardrailMiddleware_ImplementsIMiddleware()
        {
            var code = File.ReadAllText(Path.Combine(PipelineDir, "OutputGuardrailMiddleware.cs"));
            Assert.Contains("IMiddleware<LlmRequestContext>", code);
        }

        [Fact]
        public void OutputGuardrailMiddleware_HasInvokeAsync()
        {
            var code = File.ReadAllText(Path.Combine(PipelineDir, "OutputGuardrailMiddleware.cs"));
            Assert.Contains("InvokeAsync", code);
        }

        [Fact]
        public void OutputGuardrailMiddleware_HasOrderBetweenToolCallAndClientInvoke()
        {
            var middleware = new OutputGuardrailMiddleware();
            Assert.InRange(middleware.Order, 600, 799);
        }

        [Fact]
        public void OutputGuardrailMiddleware_ChecksEmptyResponse()
        {
            var code = File.ReadAllText(Path.Combine(PipelineDir, "OutputGuardrailMiddleware.cs"));
            Assert.True(
                code.Contains("Content") && (code.Contains("IsNullOrEmpty") || code.Contains("NullOrWhiteSpace")),
                "OutputGuardrailMiddleware must check for empty response content");
        }

        [Fact]
        public void OutputGuardrailMiddleware_ChecksRepetitiveActions()
        {
            var code = File.ReadAllText(Path.Combine(PipelineDir, "OutputGuardrailMiddleware.cs"));
            Assert.True(
                code.Contains("Repetitive") || code.Contains("repetitive") || code.Contains("Repeat"),
                "OutputGuardrailMiddleware must check for repetitive actions");
        }
    }
}
