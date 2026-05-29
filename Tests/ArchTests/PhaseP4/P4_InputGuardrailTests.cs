using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_InputGuardrailTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void IInputGuardrail_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "IInputGuardrail.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void GuardrailResult_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "GuardrailResult.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void InputGuardrailMiddleware_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "InputGuardrailMiddleware.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void InputGuardrailMiddleware_IsInPipeline() =>
            Assert.Contains("InputGuardrailMiddleware", File.ReadAllText(
                Directory.GetFiles(ProjectRoot, "UnifiedRequestPipelineFactory.cs", SearchOption.AllDirectories).First()));

        [Fact]
        public void EmptyPerceptionGuardrail_Exists() =>
            Assert.NotNull(Directory.GetFiles(ProjectRoot, "EmptyPerceptionGuardrail.cs", SearchOption.AllDirectories).FirstOrDefault());

        [Fact]
        public void IInputGuardrail_HasCheckMethod()
        {
            var content = File.ReadAllText(Directory.GetFiles(ProjectRoot, "IInputGuardrail.cs", SearchOption.AllDirectories).First());
            Assert.Contains("Check", content);
            Assert.Contains("LlmRequestEnvelope", content);
        }
    }
}
