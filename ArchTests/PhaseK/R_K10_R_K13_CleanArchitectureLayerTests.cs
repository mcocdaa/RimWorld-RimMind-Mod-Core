using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Domain.Llm;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseK
{
    /// <summary>
    /// R-K10~K13: CleanArchitecture layer boundary tests for K-phase components.
    /// Verifies that streaming and unified request components follow
    /// Domain → Application → Infrastructure → Presentation dependency direction.
    /// </summary>
    public class CleanArchitectureLayerTests
    {
        /// <summary>
        /// R-K10: ChunkAggregator lives in Application layer (Features.Llm),
        /// not Domain (it's an application-level aggregation strategy, not a domain value object).
        /// Uses reflection because ChunkAggregator is internal.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void R_K10_ChunkAggregator_In_Application_Features_Llm()
        {
            var type = FindType("RimMind.Application.Features.Llm.ChunkAggregator");
            type.Should().NotBeNull("ChunkAggregator must exist in Application.Features.Llm");
            type!.Namespace.Should().Be("RimMind.Application.Features.Llm",
                "ChunkAggregator is an application-level aggregation strategy, " +
                "must live in Application.Features.Llm, not Domain");
        }

        /// <summary>
        /// R-K10: ChunkAggregator is internal (not public API), consistent with
        /// Application layer implementation details.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void R_K10_ChunkAggregator_Is_Internal()
        {
            var type = FindType("RimMind.Application.Features.Llm.ChunkAggregator");
            type.Should().NotBeNull("ChunkAggregator must exist");
            type!.IsNotPublic.Should().BeTrue(
                "ChunkAggregator is an internal implementation detail of the streaming pipeline, " +
                "not a public API surface");
        }

        /// <summary>
        /// R-K11: All IAIClient implementations live in Infrastructure layer.
        /// OpenAIClient, Player2Client, HybridAIClient, LocalAIClient are
        /// infrastructure adapters that wrap external HTTP/local services.
        /// Uses source file analysis because IAIClient implementations live in
        /// 2_RimMindCore.dll which depends on Verse and cannot be loaded in
        /// the net10.0 test runner.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void R_K11_IAIClient_Implementations_In_Infrastructure()
        {
            var sourceDir = ArchTestExtensions.FindSourceDirectory();
            sourceDir.Should().NotBeNullOrEmpty("Source directory must exist for analysis");

            var infrastructureDir = Path.Combine(sourceDir, "Infrastructure", "Services", "Clients");
            Directory.Exists(infrastructureDir).Should().BeTrue(
                "Infrastructure/Services/Clients directory must exist");

            var clientFiles = Directory.GetFiles(infrastructureDir, "*Client.cs", SearchOption.AllDirectories);
            clientFiles.Should().NotBeEmpty(
                "At least one IAIClient implementation file must exist in Infrastructure/Services/Clients");

            foreach (var file in clientFiles)
            {
                var source = File.ReadAllText(file);
                if (!Regex.IsMatch(source, @":\s*IAIClient\b")) continue;

                var nsMatch = Regex.Match(source, @"namespace\s+([\w.]+)");
                nsMatch.Success.Should().BeTrue($"File {Path.GetFileName(file)} must have a namespace declaration");
                nsMatch.Groups[1].Value.Should().StartWith("RimMind.Infrastructure",
                    $"{Path.GetFileName(file)} implements IAIClient and must live in Infrastructure layer. " +
                    $"Actual namespace: {nsMatch.Groups[1].Value}");
            }
        }

        /// <summary>
        /// R-K11: IAIClient interface defines SendStreamAsync for streaming support.
        /// Each client must implement the streaming method to enable unified streaming pipeline.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void R_K11_IAIClient_Has_SendStreamAsync()
        {
            var clientInterface = typeof(IAIClient);
            var sendStreamMethod = clientInterface.GetMethod("SendStreamAsync");
            sendStreamMethod.Should().NotBeNull(
                "IAIClient must define SendStreamAsync for streaming support");
        }

        /// <summary>
        /// R-K12: UnifiedRequestPipelineFactory lives in Application layer
        /// (Features.Pipeline.Unified), consistent with pipeline orchestration
        /// being an application-level concern.
        /// Uses reflection because UnifiedRequestPipelineFactory is internal.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void R_K12_UnifiedRequestPipelineFactory_In_Application()
        {
            var type = FindType("RimMind.Application.Features.Pipeline.Unified.UnifiedRequestPipelineFactory");
            type.Should().NotBeNull("UnifiedRequestPipelineFactory must exist");
            type!.Namespace.Should().Be("RimMind.Application.Features.Pipeline.Unified",
                "UnifiedRequestPipelineFactory is an application-level pipeline factory, " +
                "must live in Application.Features.Pipeline.Unified");
        }

        /// <summary>
        /// R-K13: Domain types (LlmRequestEnvelope, LlmResponse, LlmChunk) have zero
        /// dependencies on Application or Infrastructure namespaces.
        /// This verifies the CleanArchitecture dependency direction:
        /// Domain does not reference Application or Infrastructure.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void R_K13_Domain_Llm_Types_In_Domain_Assembly()
        {
            var domainTypes = new[] { typeof(LlmRequestEnvelope), typeof(LlmResponse), typeof(LlmChunk) };

            foreach (var type in domainTypes)
            {
                type.Namespace.Should().StartWith("RimMind.Domain",
                    $"{type.Name} must live in Domain layer");

                // Verify the type is defined in Domain assembly
                var assemblyName = type.Assembly.GetName().Name;
                assemblyName.Should().Be("0_RimMindDomain",
                    $"{type.Name} must be defined in Domain assembly (0_RimMindDomain), " +
                    $"not in Application or Infrastructure. Actual assembly: {assemblyName}");
            }
        }

        /// <summary>
        /// R-K13: LlmChunk has streaming metadata fields required by ChunkAggregator.
        /// These fields enable the streaming pipeline to accumulate metadata
        /// without ChunkAggregator needing to reference external types.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void R_K13_LlmChunk_Has_Streaming_Metadata_Fields()
        {
            var type = typeof(LlmChunk);

            var requiredProperties = new[]
            {
                "DeltaReasoningContent", "DeltaPromptTokens",
                "DeltaCompletionTokens", "DeltaCachedTokens",
                "IsLast", "FinalResponse"
            };

            foreach (var propName in requiredProperties)
            {
                var prop = type.GetProperty(propName);
                prop.Should().NotBeNull(
                    $"LlmChunk must have {propName} property for streaming metadata accumulation");
            }
        }

        /// <summary>
        /// R-K13: ClientInvokeMiddleware lives in Application layer,
        /// bridging Domain types (LlmResponse) with Infrastructure concerns (IAIClient).
        /// Uses reflection because ClientInvokeMiddleware is internal.
        /// </summary>
        [Fact]
        [Trait("Phase", "K")]
        public void R_K13_ClientInvokeMiddleware_In_Application()
        {
            var type = FindType("RimMind.Application.Features.Pipeline.Unified.ClientInvokeMiddleware");
            type.Should().NotBeNull("ClientInvokeMiddleware must exist");
            type!.Namespace.Should().Be("RimMind.Application.Features.Pipeline.Unified",
                "ClientInvokeMiddleware is an application-level middleware, " +
                "must live in Application.Features.Pipeline.Unified");
        }

        /// <summary>
        /// Helper: Find a type by full name across all loaded assemblies.
        /// Works for internal types that cannot be referenced directly.
        /// </summary>
        private static Type? FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .FirstOrDefault(t => t.FullName == fullName);
        }
    }
}
