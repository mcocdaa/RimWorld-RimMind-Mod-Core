using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseK
{
    /// <summary>
    /// R-K14~K18: Deep refinement architecture tests.
    /// Verifies structural invariants that were established during
    /// the K-phase clean architecture deep refinement pass.
    /// </summary>
    public class DeepRefinementTests
    {
        /// <summary>
        /// R-K14: StructuredTool and StructuredToolCall must only exist in Domain/Llm.
        /// These are domain value objects and must not leak into Application or Infrastructure.
        /// Uses source file analysis because the net10.0 test runner cannot load
        /// the net48 main assembly directly.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void StructuredTool_OnlyInDomainLayer()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var allCsFiles = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}backup{Path.DirectorySeparatorChar}"))
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}backup{Path.AltDirectorySeparatorChar}"))
                .ToList();

            var pattern = new Regex(@"class\s+StructuredTool\b|class\s+StructuredToolCall\b");
            var matchingFiles = allCsFiles
                .Where(f => pattern.IsMatch(File.ReadAllText(f)))
                .Select(f => f.Substring(sourceDir.Length + 1))
                .ToList();

            matchingFiles.Should().NotBeEmpty(
                "At least one file must define StructuredTool or StructuredToolCall");

            matchingFiles.Should().OnlyContain(
                f => f.StartsWith($"Domain{Path.DirectorySeparatorChar}Llm{Path.DirectorySeparatorChar}") ||
                     f.StartsWith($"Domain/Llm/"),
                "StructuredTool and StructuredToolCall must only exist in Domain/Llm. " +
                $"Found in: {string.Join(", ", matchingFiles)}");
        }

        /// <summary>
        /// R-K15: StorageDriverFailed must not exist in RimMindErrorCode or RimMindErrors.
        /// This error code was removed during refinement because storage driver failures
        /// are handled through existing error categories (ClientPermanent, Internal).
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void StorageDriverFailed_NotInErrorCode()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var errorCodePath = Path.Combine(sourceDir, "Domain", "ValueObjects", "RimMindErrorCode.cs");
            File.Exists(errorCodePath).Should().BeTrue("RimMindErrorCode.cs must exist");
            var errorCodeContent = File.ReadAllText(errorCodePath);
            errorCodeContent.Should().NotContain("StorageDriverFailed",
                "StorageDriverFailed must not appear in RimMindErrorCode enum");

            var errorsPath = Path.Combine(sourceDir, "Domain", "ValueObjects", "RimMindErrors.cs");
            File.Exists(errorsPath).Should().BeTrue("RimMindErrors.cs must exist");
            var errorsContent = File.ReadAllText(errorsPath);
            errorsContent.Should().NotContain("StorageDriverFailed",
                "StorageDriverFailed must not appear in RimMindErrors static factory class");
        }

        /// <summary>
        /// R-K16: IAIClient.SendStreamAsync must return Task&lt;Result&lt;LlmResponse, RimMindError&gt;&gt;.
        /// The streaming method must use the Result monad for error handling consistency
        /// with SendAsync, not throw exceptions or return raw LlmResponse.
        /// Uses source file analysis because the interface file is directly readable.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void SendStreamAsync_ReturnsResult()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var interfacePath = Path.Combine(sourceDir, "Application", "Common", "Interfaces", "Client", "IAIClient.cs");
            File.Exists(interfacePath).Should().BeTrue("IAIClient.cs must exist");
            var source = File.ReadAllText(interfacePath);

            var pattern = new Regex(
                @"Task\s*<\s*Result\s*<\s*LlmResponse\s*,\s*RimMindError\s*>\s*>\s+SendStreamAsync");
            pattern.IsMatch(source).Should().BeTrue(
                "SendStreamAsync must return Task<Result<LlmResponse, RimMindError>>, " +
                "consistent with SendAsync error handling via Result monad");
        }

        /// <summary>
        /// R-K17: ChunkAggregator.BuildFinalResponse must return Result&lt;LlmResponse, RimMindError&gt;.
        /// The aggregation result uses the Result monad so callers handle errors
        /// explicitly rather than catching exceptions.
        /// Uses source file analysis because ChunkAggregator is internal.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void ChunkAggregator_BuildFinalResponse_ReturnsResult()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var aggregatorPath = Path.Combine(sourceDir, "Application", "Features", "Llm", "ChunkAggregator.cs");
            File.Exists(aggregatorPath).Should().BeTrue("ChunkAggregator.cs must exist");
            var source = File.ReadAllText(aggregatorPath);

            var pattern = new Regex(
                @"Result\s*<\s*LlmResponse\s*,\s*RimMindError\s*>\s+BuildFinalResponse");
            pattern.IsMatch(source).Should().BeTrue(
                "BuildFinalResponse must return Result<LlmResponse, RimMindError>, " +
                "not raw LlmResponse, to enforce explicit error handling in the streaming pipeline");
        }

        /// <summary>
        /// R-K18: No placeholder Obsolete methods remain in the source.
        /// [Obsolete("Placeholder...")] markers indicate incomplete implementation
        /// that should have been resolved before merging. Savegame compatibility
        /// markers use different wording and are excluded from this check.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void NoPlaceholderObsoleteMethods()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var allCsFiles = Directory.GetFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}backup{Path.DirectorySeparatorChar}"))
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}backup{Path.AltDirectorySeparatorChar}"))
                .ToList();

            var pattern = new Regex(@"\[Obsolete\(""Placeholder");
            var offendingFiles = allCsFiles
                .Where(f => pattern.IsMatch(File.ReadAllText(f)))
                .Select(f => f.Substring(sourceDir.Length + 1))
                .ToList();

            offendingFiles.Should().BeEmpty(
                "No files should contain [Obsolete(\"Placeholder...)] markers. " +
                "These indicate incomplete implementation that must be resolved. " +
                $"Found in: {string.Join(", ", offendingFiles)}");
        }
    }
}
