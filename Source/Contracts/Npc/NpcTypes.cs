using System.Collections.Generic;

namespace RimMind.Contracts.Npc
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
    }
}
