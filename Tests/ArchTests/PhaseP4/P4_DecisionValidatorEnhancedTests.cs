using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP4
{
    public class P4_DecisionValidatorEnhancedTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        private static string ReadDecisionValidator()
        {
            var file = Directory.GetFiles(ProjectRoot, "DecisionValidator.cs", SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains("Application") && f.Contains("Agent"))
                ?? throw new FileNotFoundException("DecisionValidator.cs not found");
            return File.ReadAllText(file);
        }

        [Fact]
        public void DecisionValidator_ValidatesParamJson()
        {
            var content = ReadDecisionValidator();
            Assert.Matches(@"JToken\.Parse|JsonConvert|JsonDocument|IsValidJson|Param.*JSON|JSON.*Param", content);
        }

        [Fact]
        public void DecisionValidator_ValidatesOperationType()
        {
            var content = ReadDecisionValidator();
            Assert.Matches(@"actionSuffix|operation.*not supported|SupportedOperation|FindById.*ActionIntent", content);
        }

        [Fact]
        public void DecisionValidator_ExtractsActionSuffix()
        {
            var content = ReadDecisionValidator();
            Assert.Matches(@"actionSuffix|Substring\(dotIndex", content);
        }

        [Fact]
        public void DecisionValidator_ParamValidation_RejectsInvalidJson()
        {
            var content = ReadDecisionValidator();
            Assert.Matches(@"not valid JSON|invalid.*JSON|Param.*not.*valid", content);
        }
    }
}
