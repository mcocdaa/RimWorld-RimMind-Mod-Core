using System.Collections.Generic;

namespace RimMind.Kernel.Context
{
    public interface IHistoryManager
    {
        void AddTurn(string npcId, string userMessage, string assistantMessage, string? scenario = null);
        List<(string role, string content)> GetHistory(string npcId, int maxRounds, string? scenario = null);
        int GetHistoryCount(string npcId);
        void ClearHistory(string npcId);
        void CompressIfNeeded(string npcId);
        void ReplaceLastAssistantTurn(string npcId, string content);
    }
}
