using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP3
{
    public class P3_DecisionValidatorTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static readonly string AgentDir = Path.Combine(
            ProjectRoot, "Source", "Application", "Features", "Agent");

        [Fact]
        public void IDecisionValidator_Interface_Exists()
        {
            var path = Path.Combine(AgentDir, "IDecisionValidator.cs");
            Assert.True(File.Exists(path), "IDecisionValidator.cs does not exist");
            var code = File.ReadAllText(path);
            Assert.Contains("interface IDecisionValidator", code);
            Assert.Contains("Validate", code);
        }

        [Fact]
        public void DecisionValidator_Class_Exists()
        {
            var path = Path.Combine(AgentDir, "DecisionValidator.cs");
            Assert.True(File.Exists(path), "DecisionValidator.cs does not exist");
            var code = File.ReadAllText(path);
            Assert.Contains("class DecisionValidator", code);
            Assert.Contains("IDecisionValidator", code);
        }

        [Fact]
        public void DecisionValidator_UsesIToolRegistry()
        {
            var code = File.ReadAllText(Path.Combine(AgentDir, "DecisionValidator.cs"));
            Assert.Contains("IToolRegistry", code);
        }

        [Fact]
        public void DecisionProcessor_CallsIDecisionValidator()
        {
            var code = File.ReadAllText(Path.Combine(AgentDir, "DecisionProcessor.cs"));
            Assert.Contains("IDecisionValidator", code);
            Assert.Contains("Validate", code);
        }

        [Fact]
        public void DecisionValidator_ReturnsValidationResult()
        {
            var code = File.ReadAllText(Path.Combine(AgentDir, "IDecisionValidator.cs"));
            Assert.Contains("ValidationResult", code);
        }

        [Fact]
        public void ValidationResult_HasIsValidAndReason()
        {
            var code = File.ReadAllText(Path.Combine(AgentDir, "IDecisionValidator.cs"));
            Assert.True(
                code.Contains("IsValid") || code.Contains("IsOk"),
                "ValidationResult must have IsValid or IsOk property");
            Assert.Contains("Reason", code);
        }
    }
}
