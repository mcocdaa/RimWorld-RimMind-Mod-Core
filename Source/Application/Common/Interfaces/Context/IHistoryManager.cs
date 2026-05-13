using System.Collections.Generic;
using RimMind.Application.Common.Models.Npc;

namespace RimMind.Application.Common.Interfaces.Context
{
    public interface IHistoryManager
    {
        void AddTurn(string npcId, string userMessage, string assistantMessage, string? scenario = null);
        List<(string role, string content)> GetHistory(string npcId, int maxRounds, string? scenario = null);
        int GetHistoryCount(string npcId);
        void ClearHistory(string npcId);
        void CompressIfNeeded(string npcId);
        void ReplaceLastAssistantTurn(string npcId, string content);
        string GetAllForSave();
        Dictionary<string, List<HistoryEntry>> GetAllForSaveDict();
    }
}
