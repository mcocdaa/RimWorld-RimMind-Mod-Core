using System.Threading;

namespace RimMind.Application.Common.Models.Mechanisms
{
    public sealed record MechanismReadArgs
    {
        public int? PawnId { get; init; }
        public string? DefName { get; init; }
        public string? Key { get; init; }
        public CancellationToken Ct { get; init; }
    }
}
