using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Application.Common.Models.Debug;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Application.Features.Context;
using RimMind.Domain.Common;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Infrastructure.Mechanisms;
using RimMind.Infrastructure.UI.DebugTables;
using Xunit;

namespace RimMind.Tests.Presentation.UI.DebugTables;

public sealed class DebugTableModelBuilderTests
{
    [Fact]
    public void AIRequestsBuilder_MapsRunningAndFailedRows()
    {
        var entries = new List<AIRequestTraceEntry>
        {
            new()
            {
                RequestId = "req-running",
                Source = "Advisor",
                Model = "model-a",
                UserPrompt = "draft plan",
                State = AIRequestTraceState.Running
            },
            new()
            {
                RequestId = "req-failed",
                Source = "Dialogue",
                Model = "model-b",
                Response = "partial response",
                Error = "HTTP timeout after retry",
                ElapsedMs = 42,
                State = AIRequestTraceState.Failed
            }
        };

        DebugTableModel model = AIRequestsDebugTableModelBuilder.Build(entries);

        DebugTableRow running = model.Rows.Single(row => row.Id == "req-running");
        DebugTableRow failed = model.Rows.Single(row => row.Id == "req-failed");
        Assert.Equal(DebugTableStatus.Streaming, running.Status);
        Assert.Equal(DebugTableStatus.Failed, failed.Status);
        Assert.Equal("HTTP timeout after retry", failed.Summary);
        Assert.Equal("42 ms", failed.Duration);
    }

    [Fact]
    public void ToolCallsBuilder_FlattensRequestToolCalls()
    {
        var entries = new List<AIRequestTraceEntry>
        {
            new()
            {
                RequestId = "req-001",
                Source = "Advisor",
                Model = "model-a"
            },
            new()
            {
                RequestId = "req-002",
                Source = "Dialogue",
                Model = "model-b"
            }
        };
        entries[0].ToolCalls.Add(new AIRequestToolCallTrace("tool-ok", "move_to", true, null));
        entries[1].ToolCalls.Add(new AIRequestToolCallTrace("tool-failed", "reserve_target", false, "Target reservation denied"));

        DebugTableModel model = ToolCallsDebugTableModelBuilder.Build(entries);

        Assert.Equal(2, model.Rows.Count);
        DebugTableRow completed = model.Rows.Single(row => row.Id == "tool-ok");
        DebugTableRow failed = model.Rows.Single(row => row.Id == "tool-failed");
        Assert.Equal(DebugTableStatus.Completed, completed.Status);
        Assert.Equal("Advisor", completed.Scope);
        Assert.Equal("move_to", completed.Channel);
        Assert.Equal("model-a", completed.Model);
        Assert.Equal(DebugTableStatus.Failed, failed.Status);
        Assert.Equal("Target reservation denied", failed.Summary);
        Assert.Equal("Dialogue", failed.Scope);
    }

    [Fact]
    public void ToolCallsBuilder_GeneratesStableFallbackIdForEmptyToolCallId()
    {
        var entry = new AIRequestTraceEntry
        {
            RequestId = "req-001",
            Source = "Advisor",
            Model = "model-a"
        };
        entry.ToolCalls.Add(new AIRequestToolCallTrace(string.Empty, "move_to", true, null));
        entry.ToolCalls.Add(new AIRequestToolCallTrace("   ", "reserve_target", false, "denied"));

        DebugTableModel model = ToolCallsDebugTableModelBuilder.Build(new[] { entry });

        Assert.Equal("req-001:tool:0", model.Rows[0].Id);
        Assert.Equal("req-001:tool:1", model.Rows[1].Id);
    }

    [Fact]
    public void MechanismsBuilder_MapsRegisteredMechanisms()
    {
        var registry = new GameMechanismRegistry();
        var mechanism = new StubMechanism(
            "pawn.need",
            MechanismScope.Pawn,
            MechanismRisk.Moderate,
            "Reads and adjusts pawn needs.");
        registry.Register(mechanism);

        DebugTableModel model = new MechanismsDebugTableModelBuilder(registry).Build();

        DebugTableRow row = model.Rows.Single();
        Assert.Equal(mechanism.MechanismId, row.Id);
        Assert.Equal(mechanism.Scope.ToString(), row.Scope);
        Assert.Contains(mechanism.Risk.ToString(), row.Model);
        Assert.Equal(mechanism.Docs.Summary, row.Summary);
    }

    [Fact]
    public void ContextKeysBuilder_MapsRegisteredKeyMeta()
    {
        var registry = new ContextKeyRegistryImpl();
        var key = new KeyMeta(
            "pawn.identity",
            ContextLayer.L1_Baseline,
            0.75f,
            _ => new List<ContextEntry>(),
            "RimMind-Core",
            cacheScope: CacheScope.Pawn)
        {
            UpdateCount = 7
        };
        registry.Register(key);

        DebugTableModel model = new ContextKeysDebugTableModelBuilder(registry).Build();

        DebugTableRow row = model.Rows.Single();
        Assert.Equal(key.Key, row.Id);
        Assert.Equal(key.Layer.ToString(), row.Scope);
        Assert.Equal(key.OwnerMod, row.Actor);
        Assert.Equal(key.CacheScope.ToString(), row.Channel);
        Assert.Contains("Priority", row.Summary);
        Assert.Contains("UpdateCount", row.Summary);
        Assert.Contains(key.Priority.ToString("0.###"), row.Summary);
        Assert.Contains(key.UpdateCount.ToString(), row.Summary);
    }

    private sealed class StubMechanism : IGameMechanism
    {
        public StubMechanism(string mechanismId, MechanismScope scope, MechanismRisk risk, string summary)
        {
            MechanismId = mechanismId;
            Scope = scope;
            Risk = risk;
            Docs = new MechanismDocs { Summary = summary };
        }

        string IExtension.Id => MechanismId;
        string IExtension.OwnerModId => "Test";
        public string MechanismId { get; }
        public MechanismScope Scope { get; }
        public MechanismRisk Risk { get; }
        public IReadOnlyList<MechanismOperationType> SupportedOperations { get; } = new[] { MechanismOperationType.Query };
        public MechanismDocs Docs { get; }
        public IReadOnlyList<MechanismActionInfo>? GetWriteActions() => null;
        public MechanismRisk GetRiskForOperation(MechanismOperationType operation) => Risk;

        public Task<Result<string, RimMindError>> ExecuteQueryAsync(MechanismReadArgs args, CancellationToken ct)
            => Task.FromResult(Result<string, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "query")));

        public Task<Result<IReadOnlyList<MechanismEnumResult>, RimMindError>> ExecuteListAsync(int? pawnId, CancellationToken ct)
            => Task.FromResult(Result<IReadOnlyList<MechanismEnumResult>, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, "list")));

        public Task<Result<bool, RimMindError>> ExecuteSetAsync(MechanismWriteArgs args, CancellationToken ct)
            => UnsupportedWrite("set");

        public Task<Result<bool, RimMindError>> ExecuteAddAsync(MechanismWriteArgs args, CancellationToken ct)
            => UnsupportedWrite("add");

        public Task<Result<bool, RimMindError>> ExecuteRemoveAsync(MechanismWriteArgs args, CancellationToken ct)
            => UnsupportedWrite("remove");

        public Task<Result<bool, RimMindError>> ExecuteToggleAsync(MechanismWriteArgs args, CancellationToken ct)
            => UnsupportedWrite("toggle");

        public Task<Result<bool, RimMindError>> ExecuteTriggerAsync(MechanismWriteArgs args, CancellationToken ct)
            => UnsupportedWrite("trigger");

        public Task<Result<bool, RimMindError>> ExecuteWatchAsync(MechanismWriteArgs args, CancellationToken ct)
            => UnsupportedWrite("watch");

        private Task<Result<bool, RimMindError>> UnsupportedWrite(string operation)
            => Task.FromResult(Result<bool, RimMindError>.Err(RimMindErrors.MechanismOperationNotSupported(MechanismId, operation)));
    }
}
