using System.Collections.Generic;
using RimMind.Core.Context;
using Xunit;

namespace RimMind.Core.Tests
{
    [CollectionDefinition("RelevanceTable", DisableParallelization = true)]
    public class RelevanceTableCollectionDefinition { }

    [Collection("RelevanceTable")]
    public class RelevanceTableTests
    {
        public RelevanceTableTests()
        {
            RelevanceTable.Clear();
        }

        [Fact]
        public void Register_AndGetRelevance_ReturnsValue()
        {
            RelevanceTable.Register("test_scenario", "health", 0.9f);
            Assert.Equal(0.9f, RelevanceTable.GetRelevance("test_scenario", "health"));
        }

        [Fact]
        public void GetRelevance_ReturnsDefault_WhenNotFound()
        {
            Assert.Equal(0.5f, RelevanceTable.GetRelevance("nonexistent", "nonexistent"));
        }

        [Fact]
        public void RegisterBatch_RegistersMultiple()
        {
            RelevanceTable.RegisterBatch("batch_scenario", new Dictionary<string, float>
            {
                {"health", 0.8f},
                {"mood", 0.7f}
            });
            Assert.Equal(0.8f, RelevanceTable.GetRelevance("batch_scenario", "health"));
            Assert.Equal(0.7f, RelevanceTable.GetRelevance("batch_scenario", "mood"));
        }

        [Fact]
        public void Unregister_RemovesEntry()
        {
            RelevanceTable.Register("rem_scenario", "health", 0.9f);
            Assert.True(RelevanceTable.Unregister("rem_scenario", "health"));
            Assert.Equal(0.5f, RelevanceTable.GetRelevance("rem_scenario", "health"));
        }

        [Fact]
        public void UnregisterScenario_RemovesAllForScenario()
        {
            RelevanceTable.Register("del_scenario", "health", 0.9f);
            RelevanceTable.Register("del_scenario", "mood", 0.8f);
            Assert.True(RelevanceTable.UnregisterScenario("del_scenario"));
            Assert.Equal(0.5f, RelevanceTable.GetRelevance("del_scenario", "health"));
            Assert.Equal(0.5f, RelevanceTable.GetRelevance("del_scenario", "mood"));
        }

        [Fact]
        public void RegisterCoreRelevance_IsIdempotent()
        {
            RelevanceTable.RegisterCoreRelevance();
            float firstHealth = RelevanceTable.GetRelevance(ScenarioIds.Dialogue, "health");
            int countBefore = CountAllEntries();

            RelevanceTable.RegisterCoreRelevance();
            float secondHealth = RelevanceTable.GetRelevance(ScenarioIds.Dialogue, "health");
            int countAfter = CountAllEntries();

            Assert.Equal(firstHealth, secondHealth);
            Assert.Equal(countBefore, countAfter);
        }

        [Fact]
        public void Clear_ResetsCoreRegistered()
        {
            RelevanceTable.RegisterCoreRelevance();
            Assert.NotEqual(0.5f, RelevanceTable.GetRelevance(ScenarioIds.Dialogue, "health"));

            RelevanceTable.Clear();
            Assert.Equal(0.5f, RelevanceTable.GetRelevance(ScenarioIds.Dialogue, "health"));

            RelevanceTable.RegisterCoreRelevance();
            Assert.NotEqual(0.5f, RelevanceTable.GetRelevance(ScenarioIds.Dialogue, "health"));
        }

        private static int CountAllEntries()
        {
            int count = 0;
            foreach (var scenario in new[] { ScenarioIds.Dialogue, ScenarioIds.Decision,
                ScenarioIds.Personality, ScenarioIds.Storyteller })
            {
                foreach (var key in new[] { "health", "mood", "current_job", "combat_status",
                    "target_info", "task_progress", "nearby_pawns", "colony_status",
                    "current_area", "weather", "time_of_day", "season", "map_structure",
                    "pawn_base_info", "fixed_relations", "ideology", "skills_summary",
                    "memory_pawn", "working_memory", "memory_narrator" })
                {
                    if (RelevanceTable.GetRelevance(scenario, key) != 0.5f)
                        count++;
                }
            }
            return count;
        }
    }
}
