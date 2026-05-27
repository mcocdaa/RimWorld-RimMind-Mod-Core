using System.Collections.Generic;
using RimMind.Application.Features.Context;
using RimMind.Application.Common.Models.Context;
using Xunit;

namespace RimMind.Tests.Context
{
    public class RelevanceTableImplTests
    {
        private readonly RelevanceTableImpl _table;

        public RelevanceTableImplTests()
        {
            _table = new RelevanceTableImpl();
        }

        [Fact]
        public void Register_AndGetRelevance_ReturnsValue()
        {
            _table.Register("test_scenario", "health", 0.9f);

            Assert.Equal(0.9f, _table.GetRelevance("test_scenario", "health"));
        }

        [Fact]
        public void GetRelevance_ReturnsDefault_WhenNotFound()
        {
            Assert.Equal(0.5f, _table.GetRelevance("nonexistent", "nonexistent"));
        }

        [Fact]
        public void RegisterBatch_RegistersMultiple()
        {
            _table.RegisterBatch("batch_scenario", new Dictionary<string, float>
            {
                {"health", 0.8f},
                {"mood", 0.7f}
            });

            Assert.Equal(0.8f, _table.GetRelevance("batch_scenario", "health"));
            Assert.Equal(0.7f, _table.GetRelevance("batch_scenario", "mood"));
        }

        [Fact]
        public void Unregister_RemovesEntry()
        {
            _table.Register("rem_scenario", "health", 0.9f);

            bool result = _table.Unregister("rem_scenario", "health");

            Assert.True(result);
            Assert.Equal(0.5f, _table.GetRelevance("rem_scenario", "health"));
        }

        [Fact]
        public void Unregister_NonExisting_ReturnsFalse()
        {
            bool result = _table.Unregister("nonexistent", "nonexistent");

            Assert.False(result);
        }

        [Fact]
        public void RegisterCoreRelevance_IsIdempotent()
        {
            _table.RegisterCoreRelevance();
            float firstHealth = _table.GetRelevance(ScenarioIds.Dialogue, "health");
            int countBefore = CountAllEntries();

            _table.RegisterCoreRelevance();
            float secondHealth = _table.GetRelevance(ScenarioIds.Dialogue, "health");
            int countAfter = CountAllEntries();

            Assert.Equal(firstHealth, secondHealth);
            Assert.Equal(countBefore, countAfter);
        }

        [Fact]
        public void Clear_ResetsCoreRegistered()
        {
            _table.RegisterCoreRelevance();
            Assert.NotEqual(0.5f, _table.GetRelevance(ScenarioIds.Dialogue, "health"));

            _table.Clear();
            Assert.Equal(0.5f, _table.GetRelevance(ScenarioIds.Dialogue, "health"));

            _table.RegisterCoreRelevance();
            Assert.NotEqual(0.5f, _table.GetRelevance(ScenarioIds.Dialogue, "health"));
        }

        [Fact]
        public void Register_OverwritesExisting()
        {
            _table.Register("overwrite_scenario", "health", 0.9f);
            _table.Register("overwrite_scenario", "health", 0.3f);

            Assert.Equal(0.3f, _table.GetRelevance("overwrite_scenario", "health"));
        }

        [Fact]
        public void CoreRelevance_AllScenariosRegistered()
        {
            _table.RegisterCoreRelevance();

            Assert.NotEqual(0.5f, _table.GetRelevance(ScenarioIds.Decision, "health"));
            Assert.NotEqual(0.5f, _table.GetRelevance(ScenarioIds.Dialogue, "mood"));
            Assert.NotEqual(0.5f, _table.GetRelevance(ScenarioIds.Personality, "ideology"));
            Assert.NotEqual(0.5f, _table.GetRelevance(ScenarioIds.Storyteller, "colony_status"));
        }

        private int CountAllEntries()
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
                    if (_table.GetRelevance(scenario, key) != 0.5f)
                        count++;
                }
            }
            return count;
        }
    }
}
