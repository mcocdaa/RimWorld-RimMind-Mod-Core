using System.Threading.Tasks;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.Common;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Client
{
    public interface IAIClient
    {
        [ThreadAffinity(ThreadAffinityKind.BackgroundOnly)]
        Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request);

        bool IsLocalEndpoint { get; }
    }
}
