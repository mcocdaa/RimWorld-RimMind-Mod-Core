using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Result;

namespace RimMind.Contracts.Mechanisms
{
    public interface IGameMechanism : IExtension
    {
        string MechanismId { get; }
        MechanismScope Scope { get; }
        MechanismRisk Risk { get; }
        IReadOnlyList<MechanismOperationType> SupportedOperations { get; }
        MechanismDocs Docs { get; }

        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        Task<Result<string, RimMindError>> ExecuteQueryAsync(MechanismReadArgs args, CancellationToken ct);
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteSetAsync(MechanismWriteArgs args, CancellationToken ct);
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteAddAsync(MechanismWriteArgs args, CancellationToken ct);
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteRemoveAsync(MechanismWriteArgs args, CancellationToken ct);
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteToggleAsync(MechanismWriteArgs args, CancellationToken ct);
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteTriggerAsync(MechanismWriteArgs args, CancellationToken ct);
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        Task<Result<IReadOnlyList<MechanismEnumResult>, RimMindError>> ExecuteListAsync(int? pawnId, CancellationToken ct);
        [ThreadAffinity(ThreadAffinityKind.MainOnly)]
        Task<Result<bool, RimMindError>> ExecuteWatchAsync(MechanismWriteArgs args, CancellationToken ct);
    }
}
