using System.Threading.Tasks;
using RimMind.Contracts;

namespace RimMind.Contracts.Client
{
    public interface IAIClient
    {
        [ThreadAffinity(ThreadAffinityKind.BackgroundOnly)]
        Task<AIResponse> SendAsync(AIRequest request);

        bool IsLocalEndpoint { get; }
    }
}
