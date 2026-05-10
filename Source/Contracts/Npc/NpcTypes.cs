using System.Collections.Generic;

namespace RimMind.Contracts.Npc
{
    public class NpcCommand
    {
        public string Name = "";
        public string Description = "";
        public string[] Parameters = new string[0];
        public bool NeverRespondWithMessage;

        public NpcCommand() { }

        public NpcCommand(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public class TtsConfig
    {
        public string[] VoiceIds = new string[0];
        public float Speed = 1.0f;
        public string AudioFormat = "mp3";
    }

    public class NpcProfile
    {
        public string NpcId = "";
        public int PawnId;
        public string Name = "";
        public string ShortName = "";
        public string DisplayName = "";
        public string Backstory = "";
        public string CharacterDescription = "";
        public string SystemPrompt = "";
        public string Type = "";
        public List<NpcCommand> Commands = new List<NpcCommand>();
        public TtsConfig? TtsConfig;

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
        public string NpcId = "";
        public string Message = "";
        public string Emotion = "";
        public string? AudioUrl;
        public List<NpcCommandResult> Commands = new List<NpcCommandResult>();

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
        public string NpcId = "";
        public string Chunk = "";
        public string Emotion = "";
        public string? AudioUrl;
        public bool IsFinal;

        public NpcChatChunk() { }

        public NpcChatChunk(string npcId, string chunk, string emotion = "", bool isFinal = false)
        {
            NpcId = npcId;
            Chunk = chunk;
            Emotion = emotion;
            IsFinal = isFinal;
        }
    }

    public class NpcCommandResult
    {
        public string Name = "";
        public string[] Arguments = new string[0];
        public bool Success;
        public string Message = "";

        public NpcCommandResult() { }

        public NpcCommandResult(string name, string[]? arguments = null, bool success = true, string message = "")
        {
            Name = name;
            Arguments = arguments ?? new string[0];
            Success = success;
            Message = message;
        }
    }
}
