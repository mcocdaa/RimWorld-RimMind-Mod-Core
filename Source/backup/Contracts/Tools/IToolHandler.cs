using System.Threading;
using System.Threading.Tasks;
using RimMind.Contracts.Extension;
using RimMind.Contracts.Result;

namespace RimMind.Contracts.Tools
{
    public interface IToolHandler : IExtension
    {
        ToolDefinition Definition { get; }

        [ThreadAffinity(ThreadAffinityKind.Any)]
        Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct);
    }
}
