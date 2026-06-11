using System.Collections.Generic;
using RimMind.Application.Common.Models.Debug;

namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IAIRequestTraceLog
    {
        IReadOnlyList<AIRequestTraceEntry> Entries { get; }
        void StartRequest(string requestId, string source, string model, string userPrompt);
        void CompleteRequest(string requestId, string response, int tokensUsed, int elapsedMs);
        void FailRequest(string requestId, string error);
        void AddToolCall(string requestId, string toolCallId, string toolName, bool succeeded, string? error);
        void Clear();
    }
}
