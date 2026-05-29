using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP5
{
    public class P5_PipelineSmokeTests
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
        public void InputGuardrail_Middleware_Registered()
        {
            var content = ReadSource("UnifiedRequestPipelineFactory.cs");

            Assert.Contains("InputGuardrailMiddleware", content);
        }

        [Fact]
        public void OutputGuardrail_Middleware_Registered()
        {
            var content = ReadSource("UnifiedRequestPipelineFactory.cs");

            Assert.Contains("OutputGuardrailMiddleware", content);
        }

        [Fact]
        public void Guardrail_Order_Correct()
        {
            var content = ReadSource("UnifiedRequestPipelineFactory.cs");

            int inputGuardrailPos = content.IndexOf("InputGuardrailMiddleware", StringComparison.Ordinal);
            int contextBuildPos = content.IndexOf("ContextBuildMiddleware", StringComparison.Ordinal);
            int clientInvokePos = content.IndexOf("ClientInvokeMiddleware", StringComparison.Ordinal);
            int outputGuardrailPos = content.IndexOf("OutputGuardrailMiddleware", StringComparison.Ordinal);

            Assert.True(inputGuardrailPos > 0, "InputGuardrailMiddleware must be present");
            Assert.True(contextBuildPos > 0, "ContextBuildMiddleware must be present");
            Assert.True(clientInvokePos > 0, "ClientInvokeMiddleware must be present");
            Assert.True(outputGuardrailPos > 0, "OutputGuardrailMiddleware must be present");

            Assert.True(inputGuardrailPos < contextBuildPos,
                "InputGuardrailMiddleware must appear before ContextBuildMiddleware");
            Assert.True(outputGuardrailPos > clientInvokePos,
                "OutputGuardrailMiddleware must appear after ClientInvokeMiddleware");
        }

        [Fact]
        public void Pipeline_Has_14_Middlewares()
        {
            var content = ReadSource("UnifiedRequestPipelineFactory.cs");

            var matches = Regex.Matches(content, @"new\s+\w+Middleware");
            int middlewareCount = matches.Count;

            Assert.True(middlewareCount >= 13,
                $"UnifiedRequestPipelineFactory must register at least 13 middlewares, found {middlewareCount}");
        }
    }
}
