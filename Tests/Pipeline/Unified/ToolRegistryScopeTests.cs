using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Agent;
using Xunit;

namespace RimMind.Tests.Pipeline.Unified
{
    public sealed class ToolRegistryScopeTests
    {
        private sealed class StubToolHandler : IToolHandler
        {
            public string Id => Definition.Id;
            public string OwnerModId => "Test";
            public ToolDefinition Definition { get; }

            public StubToolHandler(string id, ToolManifest manifest)
            {
                Definition = new ToolDefinition { Id = id, Manifest = manifest };
            }

            public Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
            {
                return Task.FromResult(Result<ToolResult, RimMindError>.Ok(new ToolResult { ToolCallId = args.ToolCallId }));
            }
        }

        [Fact]
        public void ToolRegistry_ScopeQueries_ReturnPawnToolOnlyForPawnScope()
        {
            var pawnOnlyManifest = ToolManifest.Default with
            {
                AllowedScopes = new[] { AgentScopeKind.Pawn },
            };

            var handler = new StubToolHandler("pawn.only", pawnOnlyManifest);
            var registry = new ToolRegistry();

            registry.Register(handler);

            Assert.Contains(handler, registry.GetHandlersForScope(AgentScopeKind.Pawn));
            Assert.DoesNotContain(handler, registry.GetHandlersForScope(AgentScopeKind.Storyteller));
            Assert.Contains(registry.GetDefinitionsForScope(AgentScopeKind.Pawn), d => d.Id == "pawn.only");
            Assert.DoesNotContain(registry.GetDefinitionsForScope(AgentScopeKind.Storyteller), d => d.Id == "pawn.only");
        }

        [Fact]
        public void ToolRegistry_ScopeQueries_FailClosedForMalformedManifest()
        {
            var nullManifestHandler = new StubToolHandler("malformed.null_manifest", null!);
            var nullScopesHandler = new StubToolHandler(
                "malformed.null_scopes",
                ToolManifest.Default with { AllowedScopes = null! });
            var registry = new ToolRegistry();

            registry.Register(nullManifestHandler);
            registry.Register(nullScopesHandler);

            var handlersException = Record.Exception(() => registry.GetHandlersForScope(AgentScopeKind.Pawn));
            var definitionsException = Record.Exception(() => registry.GetDefinitionsForScope(AgentScopeKind.Pawn));

            Assert.Null(handlersException);
            Assert.Null(definitionsException);
            Assert.DoesNotContain(nullManifestHandler, registry.GetHandlersForScope(AgentScopeKind.Pawn));
            Assert.DoesNotContain(nullScopesHandler, registry.GetHandlersForScope(AgentScopeKind.Pawn));
            Assert.DoesNotContain(registry.GetDefinitionsForScope(AgentScopeKind.Pawn), d => d.Id == "malformed.null_manifest");
            Assert.DoesNotContain(registry.GetDefinitionsForScope(AgentScopeKind.Pawn), d => d.Id == "malformed.null_scopes");
        }
    }
}
