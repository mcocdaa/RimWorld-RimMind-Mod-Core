namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IContextIncludeSettings
    {
        bool IncludeRace { get; set; }
        bool IncludeAge { get; set; }
        bool IncludeGender { get; set; }
        bool IncludeBackstory { get; set; }
        bool IncludeIdeology { get; set; }
        bool IncludeTraits { get; set; }
        bool IncludeSkills { get; set; }
        int MinSkillLevel { get; set; }
        bool IncludeHealth { get; set; }
        bool IncludeCapacities { get; set; }
        bool IncludeMood { get; set; }
        bool IncludeMoodThoughts { get; set; }
        bool IncludeCurrentJob { get; set; }
        bool IncludeWorkPriorities { get; set; }
        bool IncludeEquipment { get; set; }
        bool IncludeInventory { get; set; }
        bool IncludeLocation { get; set; }
        bool IncludeRelations { get; set; }
        bool IncludeGenes { get; set; }
        bool IncludeSurroundings { get; set; }
        bool IncludeCombatStatus { get; set; }
        bool IncludeGameTime { get; set; }
        bool IncludeColonistCount { get; set; }
        bool IncludeColonistNames { get; set; }
        bool IncludeWealth { get; set; }
        bool IncludeFood { get; set; }
        bool IncludeSeason { get; set; }
        bool IncludeWeather { get; set; }
        bool IncludeThreats { get; set; }
    }
}
