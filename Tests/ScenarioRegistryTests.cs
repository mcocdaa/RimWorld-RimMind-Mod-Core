using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Core.Context;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ScenarioRegistryTests
    {
        public ScenarioRegistryTests()
        {
            ScenarioRegistry.Clear();
        }

        [Fact]
        public void Register_AndGet_ReturnsMeta()
        {
            ScenarioRegistry.Register("test_scenario", 5, "Test scenario");
            var meta = ScenarioRegistry.Get("test_scenario");
            Assert.NotNull(meta);
            Assert.Equal("test_scenario", meta.Id);
            Assert.Equal(5, meta.DefaultBaseRounds);
            Assert.Equal("Test scenario", meta.Description);
        }

        [Fact]
        public void Unregister_RemovesScenario()
        {
            ScenarioRegistry.Register("to_remove", 3, "Remove me");
            Assert.NotNull(ScenarioRegistry.Get("to_remove"));
            Assert.True(ScenarioRegistry.Unregister("to_remove"));
            Assert.Null(ScenarioRegistry.Get("to_remove"));
        }

        [Fact]
        public void Unregister_Nonexistent_ReturnsFalse()
        {
            Assert.False(ScenarioRegistry.Unregister("nonexistent"));
        }

        [Fact]
        public void Get_Nonexistent_ReturnsNull()
        {
            Assert.Null(ScenarioRegistry.Get("does_not_exist"));
        }

        [Fact]
        public void Register_Overwrites_Existing()
        {
            ScenarioRegistry.Register("overwrite_test", 3, "Original");
            ScenarioRegistry.Register("overwrite_test", 7, "Updated");
            var meta = ScenarioRegistry.Get("overwrite_test");
            Assert.NotNull(meta);
            Assert.Equal(7, meta.DefaultBaseRounds);
            Assert.Equal("Updated", meta.Description);
        }

        [Fact]
        public void GetAll_ReturnsAllRegistered()
        {
            ScenarioRegistry.Register("a1", 1, "A1");
            ScenarioRegistry.Register("a2", 2, "A2");
            var all = ScenarioRegistry.GetAll();
            Assert.True(all.Count >= 2);
            Assert.Contains(all, s => s.Id == "a1");
            Assert.Contains(all, s => s.Id == "a2");
        }

        [Fact]
        public void Register_Concurrent_NoCorruption()
        {
            const int count = 100;
            var exceptions = new List<Exception>();
            var tasks = new Task[count];

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        ScenarioRegistry.Register($"concurrent_{idx}", idx, $"Desc {idx}");
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) exceptions.Add(ex);
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.Empty(exceptions);

            for (int i = 0; i < count; i++)
            {
                var meta = ScenarioRegistry.Get($"concurrent_{i}");
                Assert.NotNull(meta);
                Assert.Equal(i, meta.DefaultBaseRounds);
            }
        }

        [Fact]
        public void RegisterAndUnregister_Concurrent_NoCorruption()
        {
            const int count = 50;
            var exceptions = new List<Exception>();
            var tasks = new Task[count * 2];

            for (int i = 0; i < count; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() =>
                {
                    try { ScenarioRegistry.Register($"race_{idx}", idx, $"Desc {idx}"); }
                    catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
                });
                tasks[count + i] = Task.Run(() =>
                {
                    try { ScenarioRegistry.Unregister($"race_{idx}"); }
                    catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
                });
            }

            Task.WaitAll(tasks);
            Assert.Empty(exceptions);
        }

        [Fact]
        public void GetBaseRounds_ReturnsDefault_WhenNotFound()
        {
            Assert.Equal(6, ScenarioRegistry.GetBaseRounds("nonexistent"));
        }

        [Fact]
        public void GetBaseRounds_ReturnsRegistered_WhenFound()
        {
            ScenarioRegistry.Register("rounds_test", 42, "Rounds test");
            Assert.Equal(42, ScenarioRegistry.GetBaseRounds("rounds_test"));
        }

        [Fact]
        public void RegisterCoreScenarios_IsIdempotent()
        {
            ScenarioRegistry.RegisterCoreScenarios();
            int countAfterFirst = ScenarioRegistry.GetAll().Count;
            var dialogueFirst = ScenarioRegistry.Get(ScenarioIds.Dialogue);

            ScenarioRegistry.RegisterCoreScenarios();
            int countAfterSecond = ScenarioRegistry.GetAll().Count;
            var dialogueSecond = ScenarioRegistry.Get(ScenarioIds.Dialogue);

            Assert.Equal(countAfterFirst, countAfterSecond);
            Assert.NotNull(dialogueFirst);
            Assert.NotNull(dialogueSecond);
            Assert.Equal(dialogueFirst.DefaultBaseRounds, dialogueSecond.DefaultBaseRounds);
        }

        [Fact]
        public void Clear_ResetsCoreRegistered()
        {
            ScenarioRegistry.RegisterCoreScenarios();
            Assert.NotNull(ScenarioRegistry.Get(ScenarioIds.Dialogue));

            ScenarioRegistry.Clear();
            Assert.Null(ScenarioRegistry.Get(ScenarioIds.Dialogue));

            ScenarioRegistry.RegisterCoreScenarios();
            Assert.NotNull(ScenarioRegistry.Get(ScenarioIds.Dialogue));
        }
    }
}
