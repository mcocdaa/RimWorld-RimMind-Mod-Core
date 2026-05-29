using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_OutputGuardrailIntegrationTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        [Fact]
        public void OutputGuardrail_IsRegistered_InDefaultPipeline()
        {
            var factoryFile = Directory.GetFiles(ProjectRoot, "UnifiedRequestPipelineFactory.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Pipeline") && f.Contains("Unified"))
                ?? throw new FileNotFoundException("UnifiedRequestPipelineFactory.cs not found");

            var content = File.ReadAllText(factoryFile);
            Assert.Contains("OutputGuardrailMiddleware", content);
        }

        [Fact]
        public void OutputGuardrail_Order_Is650()
        {
            var middlewareFile = Directory.GetFiles(ProjectRoot, "OutputGuardrailMiddleware.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Pipeline") && f.Contains("Unified"))
                ?? throw new FileNotFoundException("OutputGuardrailMiddleware.cs not found");

            var content = File.ReadAllText(middlewareFile);
            Assert.Contains("Order => 650", content);
        }

        [Fact]
        public void OutputGuardrail_IsAfter_ClientInvoke()
        {
            var factoryFile = Directory.GetFiles(ProjectRoot, "UnifiedRequestPipelineFactory.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Pipeline") && f.Contains("Unified"))
                ?? throw new FileNotFoundException("UnifiedRequestPipelineFactory.cs not found");

            var content = File.ReadAllText(factoryFile);
            var clientInvokePos = content.IndexOf("ClientInvokeMiddleware");
            var guardrailPos = content.IndexOf("OutputGuardrailMiddleware");
            Assert.True(guardrailPos > clientInvokePos, "OutputGuardrailMiddleware should appear after ClientInvokeMiddleware in the middleware list");
        }
    }
}
