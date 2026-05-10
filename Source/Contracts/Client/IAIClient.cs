using System.Threading.Tasks;
using RimMind.Contracts;
using RimMind.Contracts.Result;

namespace RimMind.Contracts.Client
{
    public interface IAIClient
    {
        [ThreadAffinity(ThreadAffinityKind.BackgroundOnly)]
        Task<Result<AIResponse, RimMindError>> SendAsync(AIRequest request);

        bool IsLocalEndpoint { get; }
    }
}
