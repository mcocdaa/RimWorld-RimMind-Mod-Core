using System;
using System.Collections.Generic;

namespace RimMind.Application.Common.Models.Npc
{
    public class NpcProfile
    {
        public string NpcId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Backstory { get; set; } = "";
        public string Personality { get; set; } = "";
        public string SpeakingStyle { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public Dictionary<string, string> Extra { get; set; } = new Dictionary<string, string>();
    }

    public class NpcChatResult
    {
        public string NpcId { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Emotion { get; set; }
        public List<string>? Actions { get; set; }
        public string? ToolCallsJson { get; set; }
        public string? ReasoningContent { get; set; }
    }

    public class NpcChatChunk
    {
        public string Chunk { get; set; } = "";
        public bool IsDone { get; set; }
        public string? ToolCallsJson { get; set; }
        public string? ReasoningContent { get; set; }
    }

    public class HistoryEntry
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string? Scenario { get; set; }
        public long TimestampTicks { get; set; }
    }
}
