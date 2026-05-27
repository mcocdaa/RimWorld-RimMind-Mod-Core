using System;
using System.Collections.Generic;

namespace RimMind.Application.Common.Models.Npc
{
    [Obsolete("Use LlmResponse instead. Will be removed in a future phase.")]
    public class NpcChatResult
    {
        public string NpcId { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Emotion { get; set; }
        public string? AudioUrl { get; set; }
        public List<NpcCommandResult> Commands { get; set; } = new List<NpcCommandResult>();
        public List<string>? Actions { get; set; }
        public string? ToolCallsJson { get; set; }
        public string? ReasoningContent { get; set; }

        public NpcChatResult() { }

        public NpcChatResult(string npcId, string message, string emotion = "")
        {
            NpcId = npcId;
            Message = message;
            Emotion = emotion;
        }
    }

    [Obsolete("Use LlmChunk instead. Will be removed in a future phase.")]
    public class NpcChatChunk
    {
        public string NpcId { get; set; } = "";
        public string Chunk { get; set; } = "";
        public string? Emotion { get; set; }
        public string? AudioUrl { get; set; }
        public bool IsFinal { get; set; }
        public bool IsDone { get; set; }
        public string? ToolCallsJson { get; set; }
        public string? ReasoningContent { get; set; }

        public NpcChatChunk() { }

        public NpcChatChunk(string npcId, string chunk, string? emotion = null, bool isFinal = false)
        {
            NpcId = npcId;
            Chunk = chunk;
            Emotion = emotion;
            IsFinal = isFinal;
        }
    }

    [Obsolete("Will be removed with NpcChatResult in a future phase.")]
    public class NpcCommandResult
    {
        public string Name { get; set; } = "";
        public string[] Arguments { get; set; } = Array.Empty<string>();
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "";

        public NpcCommandResult() { }

        public NpcCommandResult(string name, string[]? arguments = null, bool success = true, string message = "")
        {
            Name = name;
            Arguments = arguments ?? Array.Empty<string>();
            Success = success;
            Message = message;
        }
    }
}
