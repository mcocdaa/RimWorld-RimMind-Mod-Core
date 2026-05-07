using System.Collections.Generic;
using Verse;

namespace RimMind.Core.Npc
{
    public class NpcCommand
    {
        public string Name = "";
        public string Description = "";

        public NpcCommand() { }

        public NpcCommand(string name, string description)
        {
            Name = name;
            Description = description;
        }
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
        public List<NpcCommand> Commands = new List<NpcCommand>();

        public NpcProfile() { }

        public NpcProfile(string npcId, int pawnId, string displayName, string backstory = "")
        {
            NpcId = npcId;
            PawnId = pawnId;
            DisplayName = displayName;
            Backstory = backstory;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref NpcId, "npcId");
            Scribe_Values.Look(ref PawnId, "pawnId");
            Scribe_Values.Look(ref Name, "name");
            Scribe_Values.Look(ref ShortName, "shortName");
            Scribe_Values.Look(ref DisplayName, "displayName");
            Scribe_Values.Look(ref Backstory, "backstory");
            Scribe_Values.Look(ref CharacterDescription, "characterDescription");
            Scribe_Values.Look(ref SystemPrompt, "systemPrompt");
        }
    }

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
