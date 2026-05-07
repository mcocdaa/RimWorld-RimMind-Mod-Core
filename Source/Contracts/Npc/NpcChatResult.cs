using System.Collections.Generic;

namespace RimMind.Core.Npc
{
    public class NpcChatResult
    {
        public string NpcId = "";
        public string Message = "";
        public string Emotion = "";
        public string? Error;
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
