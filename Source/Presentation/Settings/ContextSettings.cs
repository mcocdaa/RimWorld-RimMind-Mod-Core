using System;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Domain.Enums;
using Verse;

namespace RimMind.Presentation.Settings
{
    public class ContextSettings : IExposable, IContextSettings
    {
        public bool IncludeRace = true;
        public bool IncludeAge = true;
        public bool IncludeGender = true;
        public bool IncludeBackstory = true;
        public bool IncludeIdeology = true;
        public bool IncludeTraits = true;
        public bool IncludeSkills = true;
        public int MinSkillLevel = 4;
        public bool IncludeHealth = true;
        public bool IncludeCapacities = true;
        public bool IncludeMood = true;
        public bool IncludeMoodThoughts = true;
        public bool IncludeCurrentJob = true;
        public bool IncludeWorkPriorities = true;
        public bool IncludeEquipment = true;
        public bool IncludeInventory = true;
        public bool IncludeLocation = true;
        public bool IncludeRelations = true;
        public bool IncludeGenes = true;
        public bool IncludeSurroundings = true;
        public bool IncludeCombatStatus = true;

        public bool IncludeGameTime = true;
        public bool IncludeColonistCount = true;
        public bool IncludeColonistNames = true;
        public bool IncludeWealth = true;
        public bool IncludeFood = true;
        public bool IncludeSeason = true;
        public bool IncludeWeather = true;
        public bool IncludeThreats = true;

        public float ContextBudget = 0.6f;
#pragma warning disable CS0618
        [Obsolete("Use FlywheelParameterStore w1/w2 instead. This field is only kept for savegame compatibility.")]
        public float BudgetW1 = 0.4f;
        [Obsolete("Use FlywheelParameterStore w1/w2 instead. This field is only kept for savegame compatibility.")]
        public float BudgetW2 = 0.6f;
#pragma warning restore CS0618

        public int maxCacheEntries = 100;
        public int contextBriefLimit = 200;
        public float moodDiffThreshold = 5f;
        public float temperatureDiffThreshold = 5f;
        public int environmentScanRadius = 5;
        public int environmentMaxItems = 8;
        public float threatThresholdHigh = 200000f;
        public float threatThresholdMedium = 100000f;
        public float threatThresholdLow = 50000f;

        float IContextBudgetSettings.ContextBudget
        {
            get => ContextBudget;
            set => ContextBudget = value;
        }
        int IContextBudgetSettings.ContextBriefLimit => contextBriefLimit;
        int IContextEnvironmentSettings.EnvironmentScanRadius => environmentScanRadius;
        int IContextEnvironmentSettings.EnvironmentMaxItems => environmentMaxItems;
        float IContextEnvironmentSettings.ThreatThresholdHigh => threatThresholdHigh;
        float IContextEnvironmentSettings.ThreatThresholdMedium => threatThresholdMedium;
        float IContextEnvironmentSettings.ThreatThresholdLow => threatThresholdLow;
        int IContextBudgetSettings.MaxCacheEntries => maxCacheEntries;
        float IContextEnvironmentSettings.MoodDiffThreshold => moodDiffThreshold;
        float IContextEnvironmentSettings.TemperatureDiffThreshold => temperatureDiffThreshold;

        bool IContextIncludeSettings.IncludeRace
        {
            get => IncludeRace;
            set => IncludeRace = value;
        }
        bool IContextIncludeSettings.IncludeAge
        {
            get => IncludeAge;
            set => IncludeAge = value;
        }
        bool IContextIncludeSettings.IncludeGender
        {
            get => IncludeGender;
            set => IncludeGender = value;
        }
        bool IContextIncludeSettings.IncludeBackstory
        {
            get => IncludeBackstory;
            set => IncludeBackstory = value;
        }
        bool IContextIncludeSettings.IncludeIdeology
        {
            get => IncludeIdeology;
            set => IncludeIdeology = value;
        }
        bool IContextIncludeSettings.IncludeTraits
        {
            get => IncludeTraits;
            set => IncludeTraits = value;
        }
        bool IContextIncludeSettings.IncludeSkills
        {
            get => IncludeSkills;
            set => IncludeSkills = value;
        }
        int IContextIncludeSettings.MinSkillLevel
        {
            get => MinSkillLevel;
            set => MinSkillLevel = value;
        }
        bool IContextIncludeSettings.IncludeHealth
        {
            get => IncludeHealth;
            set => IncludeHealth = value;
        }
        bool IContextIncludeSettings.IncludeCapacities
        {
            get => IncludeCapacities;
            set => IncludeCapacities = value;
        }
        bool IContextIncludeSettings.IncludeMood
        {
            get => IncludeMood;
            set => IncludeMood = value;
        }
        bool IContextIncludeSettings.IncludeMoodThoughts
        {
            get => IncludeMoodThoughts;
            set => IncludeMoodThoughts = value;
        }
        bool IContextIncludeSettings.IncludeCurrentJob
        {
            get => IncludeCurrentJob;
            set => IncludeCurrentJob = value;
        }
        bool IContextIncludeSettings.IncludeWorkPriorities
        {
            get => IncludeWorkPriorities;
            set => IncludeWorkPriorities = value;
        }
        bool IContextIncludeSettings.IncludeEquipment
        {
            get => IncludeEquipment;
            set => IncludeEquipment = value;
        }
        bool IContextIncludeSettings.IncludeInventory
        {
            get => IncludeInventory;
            set => IncludeInventory = value;
        }
        bool IContextIncludeSettings.IncludeLocation
        {
            get => IncludeLocation;
            set => IncludeLocation = value;
        }
        bool IContextIncludeSettings.IncludeRelations
        {
            get => IncludeRelations;
            set => IncludeRelations = value;
        }
        bool IContextIncludeSettings.IncludeGenes
        {
            get => IncludeGenes;
            set => IncludeGenes = value;
        }
        bool IContextIncludeSettings.IncludeSurroundings
        {
            get => IncludeSurroundings;
            set => IncludeSurroundings = value;
        }
        bool IContextIncludeSettings.IncludeCombatStatus
        {
            get => IncludeCombatStatus;
            set => IncludeCombatStatus = value;
        }
        bool IContextIncludeSettings.IncludeGameTime
        {
            get => IncludeGameTime;
            set => IncludeGameTime = value;
        }
        bool IContextIncludeSettings.IncludeColonistCount
        {
            get => IncludeColonistCount;
            set => IncludeColonistCount = value;
        }
        bool IContextIncludeSettings.IncludeColonistNames
        {
            get => IncludeColonistNames;
            set => IncludeColonistNames = value;
        }
        bool IContextIncludeSettings.IncludeWealth
        {
            get => IncludeWealth;
            set => IncludeWealth = value;
        }
        bool IContextIncludeSettings.IncludeFood
        {
            get => IncludeFood;
            set => IncludeFood = value;
        }
        bool IContextIncludeSettings.IncludeSeason
        {
            get => IncludeSeason;
            set => IncludeSeason = value;
        }
        bool IContextIncludeSettings.IncludeWeather
        {
            get => IncludeWeather;
            set => IncludeWeather = value;
        }
        bool IContextIncludeSettings.IncludeThreats
        {
            get => IncludeThreats;
            set => IncludeThreats = value;
        }

#pragma warning disable CS0618
        float IContextBudgetSettings.BudgetW1
        {
            get => BudgetW1;
            set => BudgetW1 = value;
        }
        float IContextBudgetSettings.BudgetW2
        {
            get => BudgetW2;
            set => BudgetW2 = value;
        }
#pragma warning restore CS0618

        public void ExposeData()
        {
            Scribe_Values.Look(ref IncludeRace, "IncludeRace", true);
            Scribe_Values.Look(ref IncludeAge, "IncludeAge", true);
            Scribe_Values.Look(ref IncludeGender, "IncludeGender", true);
            Scribe_Values.Look(ref IncludeBackstory, "IncludeBackstory", true);
            Scribe_Values.Look(ref IncludeIdeology, "IncludeIdeology", true);
            Scribe_Values.Look(ref IncludeTraits, "IncludeTraits", true);
            Scribe_Values.Look(ref IncludeSkills, "IncludeSkills", true);
            Scribe_Values.Look(ref MinSkillLevel, "MinSkillLevel", 4);
            Scribe_Values.Look(ref IncludeHealth, "IncludeHealth", true);
            Scribe_Values.Look(ref IncludeCapacities, "IncludeCapacities", true);
            Scribe_Values.Look(ref IncludeMood, "IncludeMood", true);
            Scribe_Values.Look(ref IncludeMoodThoughts, "IncludeMoodThoughts", true);
            Scribe_Values.Look(ref IncludeCurrentJob, "IncludeCurrentJob", true);
            Scribe_Values.Look(ref IncludeWorkPriorities, "IncludeWorkPriorities", true);
            Scribe_Values.Look(ref IncludeEquipment, "IncludeEquipment", true);
            Scribe_Values.Look(ref IncludeInventory, "IncludeInventory", true);
            Scribe_Values.Look(ref IncludeLocation, "IncludeLocation", true);
            Scribe_Values.Look(ref IncludeRelations, "IncludeRelations", true);
            Scribe_Values.Look(ref IncludeGenes, "IncludeGenes", true);
            Scribe_Values.Look(ref IncludeSurroundings, "IncludeSurroundings", true);
            Scribe_Values.Look(ref IncludeCombatStatus, "IncludeCombatStatus", true);
            Scribe_Values.Look(ref IncludeGameTime, "IncludeGameTime", true);
            Scribe_Values.Look(ref IncludeColonistCount, "IncludeColonistCount", true);
            Scribe_Values.Look(ref IncludeColonistNames, "IncludeColonistNames", true);
            Scribe_Values.Look(ref IncludeWealth, "IncludeWealth", true);
            Scribe_Values.Look(ref IncludeFood, "IncludeFood", true);
            Scribe_Values.Look(ref IncludeSeason, "IncludeSeason", true);
            Scribe_Values.Look(ref IncludeWeather, "IncludeWeather", true);
            Scribe_Values.Look(ref IncludeThreats, "IncludeThreats", true);
            Scribe_Values.Look(ref ContextBudget, "ContextBudget", 0.6f);
#pragma warning disable CS0618
            Scribe_Values.Look(ref BudgetW1, "BudgetW1", 0.4f);
            Scribe_Values.Look(ref BudgetW2, "BudgetW2", 0.6f);
#pragma warning restore CS0618
            Scribe_Values.Look(ref maxCacheEntries, "maxCacheEntries", 100);
            Scribe_Values.Look(ref contextBriefLimit, "contextBriefLimit", 200);
            Scribe_Values.Look(ref moodDiffThreshold, "moodDiffThreshold", 5f);
            Scribe_Values.Look(ref temperatureDiffThreshold, "temperatureDiffThreshold", 5f);
            Scribe_Values.Look(ref environmentScanRadius, "environmentScanRadius", 5);
            Scribe_Values.Look(ref environmentMaxItems, "environmentMaxItems", 8);
            Scribe_Values.Look(ref threatThresholdHigh, "threatThresholdHigh", 200000f);
            Scribe_Values.Look(ref threatThresholdMedium, "threatThresholdMedium", 100000f);
            Scribe_Values.Look(ref threatThresholdLow, "threatThresholdLow", 50000f);
        }

        public void ApplyPreset(ContextPreset preset)
        {
            switch (preset)
            {
                case ContextPreset.Minimal:
                    IncludeRace = true; IncludeAge = false; IncludeGender = false;
                    IncludeBackstory = false; IncludeIdeology = false;
                    IncludeTraits = false; IncludeSkills = false; MinSkillLevel = 4;
                    IncludeHealth = true; IncludeCapacities = false; IncludeMood = true; IncludeMoodThoughts = false;
                    IncludeCurrentJob = false; IncludeWorkPriorities = false;
                    IncludeEquipment = false; IncludeInventory = false; IncludeLocation = false;
                    IncludeRelations = false; IncludeGenes = false;
                    IncludeSurroundings = false; IncludeCombatStatus = true;
                    IncludeGameTime = false; IncludeColonistCount = true; IncludeColonistNames = false; IncludeWealth = false;
                    IncludeFood = false; IncludeSeason = false;
                    IncludeWeather = true; IncludeThreats = true;
                    ContextBudget = 0.3f;
                    break;
                case ContextPreset.Standard:
                    IncludeRace = true; IncludeAge = true; IncludeGender = true;
                    IncludeBackstory = true; IncludeIdeology = false;
                    IncludeTraits = true; IncludeSkills = true; MinSkillLevel = 4;
                    IncludeHealth = true; IncludeCapacities = true; IncludeMood = true; IncludeMoodThoughts = false;
                    IncludeCurrentJob = true; IncludeWorkPriorities = true;
                    IncludeEquipment = true; IncludeInventory = false; IncludeLocation = false;
                    IncludeRelations = true; IncludeGenes = true;
                    IncludeSurroundings = false; IncludeCombatStatus = true;
                    IncludeGameTime = true; IncludeColonistCount = true; IncludeColonistNames = true; IncludeWealth = false;
                    IncludeFood = true; IncludeSeason = true;
                    IncludeWeather = true; IncludeThreats = true;
                    ContextBudget = 0.6f;
                    break;
                case ContextPreset.Full:
                    IncludeRace = true; IncludeAge = true; IncludeGender = true;
                    IncludeBackstory = true; IncludeIdeology = true;
                    IncludeTraits = true; IncludeSkills = true; MinSkillLevel = 1;
                    IncludeHealth = true; IncludeCapacities = true; IncludeMood = true; IncludeMoodThoughts = true;
                    IncludeCurrentJob = true; IncludeWorkPriorities = true;
                    IncludeEquipment = true; IncludeInventory = true; IncludeLocation = true;
                    IncludeRelations = true; IncludeGenes = true;
                    IncludeSurroundings = true; IncludeCombatStatus = true;
                    IncludeGameTime = true; IncludeColonistCount = true; IncludeColonistNames = true; IncludeWealth = true;
                    IncludeFood = true; IncludeSeason = true;
                    IncludeWeather = true; IncludeThreats = true;
                    ContextBudget = 1.0f;
                    break;
                case ContextPreset.Custom:
                    break;
            }
        }

        public void ResetToDefault()
        {
            IncludeRace = true; IncludeAge = true; IncludeGender = true;
            IncludeBackstory = true; IncludeIdeology = true;
            IncludeTraits = true; IncludeSkills = true; MinSkillLevel = 4;
            IncludeHealth = true; IncludeCapacities = true; IncludeMood = true; IncludeMoodThoughts = true;
            IncludeCurrentJob = true; IncludeWorkPriorities = true;
            IncludeEquipment = true; IncludeInventory = true; IncludeLocation = true;
            IncludeRelations = true; IncludeGenes = true;
            IncludeSurroundings = true; IncludeCombatStatus = true;
            IncludeGameTime = true; IncludeColonistCount = true; IncludeColonistNames = true;
            IncludeWealth = true; IncludeFood = true; IncludeSeason = true;
            IncludeWeather = true; IncludeThreats = true;
            ContextBudget = 0.6f;
#pragma warning disable CS0618
            BudgetW1 = 0.4f; BudgetW2 = 0.6f;
#pragma warning restore CS0618
        }
    }
}
