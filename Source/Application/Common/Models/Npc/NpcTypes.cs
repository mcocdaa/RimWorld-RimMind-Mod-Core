using System;
using System.Collections.Generic;

namespace RimMind.Application.Common.Models.Npc
{
    public class NpcCommand
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string[] Parameters { get; set; } = Array.Empty<string>();
        public bool NeverRespondWithMessage { get; set; }

        public NpcCommand() { }

        public NpcCommand(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public class TtsConfig
    {
        public string[] VoiceIds { get; set; } = Array.Empty<string>();
        public float Speed { get; set; } = 1.0f;
        public string AudioFormat { get; set; } = "mp3";
    }

    public class NpcProfile
    {
        public string NpcId { get; set; } = "";
        public int PawnId { get; set; }
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Backstory { get; set; } = "";
        public string CharacterDescription { get; set; } = "";
        public string SystemPrompt { get; set; } = "";
        public string Personality { get; set; } = "";
        public string SpeakingStyle { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public List<NpcCommand> Commands { get; set; } = new List<NpcCommand>();
        public TtsConfig? TtsConfig { get; set; }
        public Dictionary<string, string> Extra { get; set; } = new Dictionary<string, string>();

        public NpcProfile() { }

        public NpcProfile(string npcId, int pawnId, string displayName, string backstory = "")
        {
            NpcId = npcId;
            PawnId = pawnId;
            DisplayName = displayName;
            Backstory = backstory;
        }
    }

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
