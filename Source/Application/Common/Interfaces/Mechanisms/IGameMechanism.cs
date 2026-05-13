using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Mechanisms
{
    public interface IGameMechanism : IExtension
    {
        string MechanismId { get; }
        MechanismScope Scope { get; }
        MechanismRisk Risk { get; }
        IReadOnlyList<MechanismOperationType> SupportedOperations { get; }
        MechanismDocs Docs { get; }
        IReadOnlyList<MechanismActionInfo>? GetWriteActions();
        MechanismRisk GetRiskForOperation(MechanismOperationType operation);

        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.MainOnly)]
        Task<Result<string, RimMindError>> ExecuteQueryAsync(MechanismReadArgs args, CancellationToken ct);
        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteSetAsync(MechanismWriteArgs args, CancellationToken ct);
        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteAddAsync(MechanismWriteArgs args, CancellationToken ct);
        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteRemoveAsync(MechanismWriteArgs args, CancellationToken ct);
        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteToggleAsync(MechanismWriteArgs args, CancellationToken ct);
        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteTriggerAsync(MechanismWriteArgs args, CancellationToken ct);
        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.MainOnly)]
        Task<Result<IReadOnlyList<MechanismEnumResult>, RimMindError>> ExecuteListAsync(int? pawnId, CancellationToken ct);
        [RimMind.Domain.Common.ThreadAffinity(RimMind.Domain.Common.ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteWatchAsync(MechanismWriteArgs args, CancellationToken ct);
    }
}
