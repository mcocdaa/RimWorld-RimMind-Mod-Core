namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IContextSettings
    {
        float ContextBudget { get; }
        int ContextBriefLimit { get; }
        int EnvironmentScanRadius { get; }
        int EnvironmentMaxItems { get; }
        float ThreatThresholdHigh { get; }
        float ThreatThresholdMedium { get; }
        float ThreatThresholdLow { get; }
        int MaxCacheEntries { get; }
        float MoodDiffThreshold { get; }
        float TemperatureDiffThreshold { get; }

        bool IncludeRace { get; }
        bool IncludeAge { get; }
        bool IncludeGender { get; }
        bool IncludeBackstory { get; }
        bool IncludeIdeology { get; }
        bool IncludeTraits { get; }
        bool IncludeSkills { get; }
        int MinSkillLevel { get; }
        bool IncludeHealth { get; }
        bool IncludeCapacities { get; }
        bool IncludeMood { get; }
        bool IncludeMoodThoughts { get; }
        bool IncludeCurrentJob { get; }
        bool IncludeWorkPriorities { get; }
        bool IncludeEquipment { get; }
        bool IncludeInventory { get; }
        bool IncludeLocation { get; }
        bool IncludeRelations { get; }
        bool IncludeGenes { get; }
        bool IncludeSurroundings { get; }
        bool IncludeCombatStatus { get; }
        bool IncludeGameTime { get; }
        bool IncludeColonistCount { get; }
        bool IncludeColonistNames { get; }
        bool IncludeWealth { get; }
        bool IncludeFood { get; }
        bool IncludeSeason { get; }
        bool IncludeWeather { get; }
        bool IncludeThreats { get; }
    }
}
