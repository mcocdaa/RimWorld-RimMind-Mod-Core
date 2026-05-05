using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Core.Context;
using Xunit;

namespace RimMind.Core.Tests
{
    public class ContextRequestTests
    {
        [Fact]
        public void MaxTokens_Default_Is800()
        {
            var req = new ContextRequest();
            Assert.Equal(800, req.MaxTokens);
        }

        [Fact]
        public void MaxTokens_CanBeOverridden()
        {
            var req = new ContextRequest { MaxTokens = 1600 };
            Assert.Equal(1600, req.MaxTokens);
        }
    }

    public class AICoreSettingsDefaultsTests
    {
        [Fact]
        public void Default_maxTokens_Is800()
        {
            var settings = new AICoreSettings();
            Assert.Equal(800, settings.maxTokens);
        }

        [Fact]
        public void Default_thinkCooldownTicks_Is30000()
        {
            var settings = new AICoreSettings();
            Assert.Equal(30000, settings.thinkCooldownTicks);
        }

        [Fact]
        public void Default_agentTickInterval_Is150()
        {
            var settings = new AICoreSettings();
            Assert.Equal(150, settings.agentTickInterval);
        }

        [Fact]
        public void Default_maxToolCallDepth_Is3()
        {
            var settings = new AICoreSettings();
            Assert.Equal(3, settings.maxToolCallDepth);
        }

        [Fact]
        public void Default_defaultTemperature_Is07()
        {
            var settings = new AICoreSettings();
            Assert.Equal(0.7f, settings.defaultTemperature);
        }
    }

    public class ConcurrentDictionarySafetyTests
    {
        [Fact]
        public void ConcurrentDictionary_IntString_ConcurrentWrite_NoCorruption()
        {
            var dict = new ConcurrentDictionary<string, string>();
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
                        dict[$"key_{idx}"] = $"value_{idx}";
                        var val = dict.TryGetValue($"key_{idx}", out var v) ? v : null;
                        if (val != $"value_{idx}")
                            lock (exceptions) exceptions.Add(new Exception($"Mismatch at {idx}"));
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) exceptions.Add(ex);
                    }
                });
            }

            Task.WaitAll(tasks);
            Assert.Empty(exceptions);
            Assert.Equal(count, dict.Count);
        }

        [Fact]
        public void ConcurrentDictionary_IntString_ConcurrentReadWrite_NoCorruption()
        {
            var dict = new ConcurrentDictionary<string, string>();
            const int writers = 50;
            const int readers = 50;
            var exceptions = new List<Exception>();

            for (int i = 0; i < writers; i++)
                dict[$"rw_{i}"] = $"initial_{i}";

            var tasks = new Task[writers + readers];

            for (int i = 0; i < writers; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() =>
                {
                    try { dict[$"rw_{idx}"] = $"updated_{idx}"; }
                    catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
                });
            }

            for (int i = 0; i < readers; i++)
            {
                int idx = i % writers;
                tasks[writers + i] = Task.Run(() =>
                {
                    try { dict.TryGetValue($"rw_{idx}", out _); }
                    catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
                });
            }

            Task.WaitAll(tasks);
            Assert.Empty(exceptions);
        }

        [Fact]
        public void ConcurrentDictionary_TryRemove_Concurrent_NoCorruption()
        {
            var dict = new ConcurrentDictionary<string, string>();
            const int count = 50;
            var exceptions = new List<Exception>();

            for (int i = 0; i < count; i++)
                dict[$"del_{i}"] = $"val_{i}";

            var tasks = new Task[count];
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                tasks[i] = Task.Run(() =>
                {
                    try { dict.TryRemove($"del_{idx}", out _); }
                    catch (Exception ex) { lock (exceptions) exceptions.Add(ex); }
                });
            }

            Task.WaitAll(tasks);
            Assert.Empty(exceptions);
            Assert.Empty(dict);
        }
    }
}
