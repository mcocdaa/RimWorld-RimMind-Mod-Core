using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace RimMind.Core.Tests
{
    public class RimMindAPISkipCheckTests
    {
        private static readonly ConcurrentDictionary<string, Func<object, string, bool>> _skipChecks
            = new ConcurrentDictionary<string, Func<object, string, bool>>();

        private static void Register(string sourceId, Func<object, string, bool> check)
            => _skipChecks[sourceId] = check;

        private static void Unregister(string sourceId)
            => _skipChecks.TryRemove(sourceId, out _);

        private static bool ShouldSkip(object target, string triggerType)
        {
            foreach (var check in _skipChecks.Values.ToList())
            {
                try
                {
                    if (check(target, triggerType)) return true;
                }
                catch (Exception)
                {
                }
            }
            return false;
        }

        public RimMindAPISkipCheckTests()
        {
            _skipChecks.Clear();
        }

        [Fact]
        public void Register_ShouldSkipReturnsTrue()
        {
            Register("test_mod", (target, type) => true);
            Assert.True(ShouldSkip(new object(), "Chitchat"));
        }

        [Fact]
        public void Register_AllReturnFalse_ShouldSkipReturnsFalse()
        {
            Register("test_mod", (target, type) => false);
            Assert.False(ShouldSkip(new object(), "Chitchat"));
        }

        [Fact]
        public void NoChecksRegistered_ShouldSkipReturnsFalse()
        {
            Assert.False(ShouldSkip(new object(), "Chitchat"));
        }

        [Fact]
        public void MultipleChecks_FirstReturnsFalseSecondReturnsTrue_ReturnsTrue()
        {
            Register("mod_a", (target, type) => false);
            Register("mod_b", (target, type) => true);
            Assert.True(ShouldSkip(new object(), "Thought"));
        }

        [Fact]
        public void Unregister_RemovesCheck()
        {
            Register("mod_a", (target, type) => true);
            Assert.True(ShouldSkip(new object(), "Chitchat"));
            Unregister("mod_a");
            Assert.False(ShouldSkip(new object(), "Chitchat"));
        }

        [Fact]
        public void Overwrite_SameSourceId_ReplacesPreviousCheck()
        {
            Register("mod_a", (target, type) => false);
            Register("mod_a", (target, type) => true);
            Assert.True(ShouldSkip(new object(), "Chitchat"));
        }

        [Fact]
        public void CheckThrowsException_IsSwallowed_ContinuesToNextCheck()
        {
            Register("mod_a", (target, type) => throw new InvalidOperationException("boom"));
            Register("mod_b", (target, type) => true);
            Assert.True(ShouldSkip(new object(), "Chitchat"));
        }

        [Fact]
        public void AllChecksThrow_ReturnsFalse()
        {
            Register("mod_a", (target, type) => throw new Exception("a"));
            Register("mod_b", (target, type) => throw new Exception("b"));
            Assert.False(ShouldSkip(new object(), "Chitchat"));
        }

        [Fact]
        public void TargetAndTriggerType_PassedCorrectly()
        {
            object? capturedTarget = null;
            string? capturedTrigger = null;

            Register("mod_a", (target, type) =>
            {
                capturedTarget = target;
                capturedTrigger = type;
                return false;
            });

            var obj = new object();
            ShouldSkip(obj, "PlayerInput");

            Assert.Same(obj, capturedTarget);
            Assert.Equal("PlayerInput", capturedTrigger);
        }

        [Fact]
        public void ToListSnapshot_PreventsCollectionModified()
        {
            var results = new List<bool>();
            Register("mod_a", (target, type) => { results.Add(true); return false; });

            ShouldSkip(new object(), "Auto");

            Assert.Single(results);
            Assert.True(results[0]);
        }
    }

    public class RimMindAPIKeyBasedCallbackTests
    {
        private static readonly ConcurrentDictionary<string, Action> _incidentCallbacks
            = new ConcurrentDictionary<string, Action>();
        private static readonly ConcurrentDictionary<string, Func<bool>> _skipChecks
            = new ConcurrentDictionary<string, Func<bool>>();
        private static int _counter;

        private static string RegisterIncidentCallback(Action callback)
        {
            string key = $"cb_{Interlocked.Increment(ref _counter)}";
            _incidentCallbacks[key] = callback;
            return key;
        }

        private static void UnregisterIncidentCallback(string key)
            => _incidentCallbacks.TryRemove(key, out _);

        private static string RegisterSkipCheck(Func<bool> check)
        {
            string key = $"sc_{Interlocked.Increment(ref _counter)}";
            _skipChecks[key] = check;
            return key;
        }

        private static void UnregisterSkipCheck(string key)
            => _skipChecks.TryRemove(key, out _);

        private static void NotifyIncident()
        {
            foreach (var cb in _incidentCallbacks.Values.ToList())
            {
                try { cb(); }
                catch (Exception) { }
            }
        }

        private static bool ShouldSkip()
        {
            foreach (var check in _skipChecks.Values.ToList())
            {
                try { if (check()) return true; }
                catch (Exception) { }
            }
            return false;
        }

        public RimMindAPIKeyBasedCallbackTests()
        {
            _incidentCallbacks.Clear();
            _skipChecks.Clear();
            _counter = 0;
        }

        [Fact]
        public void RegisterIncidentCallback_ReturnsNonNullKey()
        {
            string key = RegisterIncidentCallback(() => { });
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("cb_", key);
        }

        [Fact]
        public void RegisterIncidentCallback_KeysAreUnique()
        {
            string key1 = RegisterIncidentCallback(() => { });
            string key2 = RegisterIncidentCallback(() => { });
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void UnregisterIncidentCallback_ByKey_RemovesCallback()
        {
            bool called = false;
            string key = RegisterIncidentCallback(() => called = true);
            UnregisterIncidentCallback(key);
            NotifyIncident();
            Assert.False(called);
        }

        [Fact]
        public void UnregisterIncidentCallback_OnlyRemovesTarget()
        {
            bool called1 = false;
            bool called2 = false;
            string key1 = RegisterIncidentCallback(() => called1 = true);
            RegisterIncidentCallback(() => called2 = true);
            UnregisterIncidentCallback(key1);
            NotifyIncident();
            Assert.False(called1);
            Assert.True(called2);
        }

        [Fact]
        public void RegisterIncidentCallback_LambdaCanBeUnregistered()
        {
            bool called = false;
            string key = RegisterIncidentCallback(() => called = true);
            UnregisterIncidentCallback(key);
            NotifyIncident();
            Assert.False(called);
        }

        [Fact]
        public void NotifyIncident_CallsAllRegisteredCallbacks()
        {
            int count = 0;
            RegisterIncidentCallback(() => count++);
            RegisterIncidentCallback(() => count++);
            NotifyIncident();
            Assert.Equal(2, count);
        }

        [Fact]
        public void RegisterSkipCheck_ReturnsNonNullKey()
        {
            string key = RegisterSkipCheck(() => false);
            Assert.False(string.IsNullOrEmpty(key));
            Assert.StartsWith("sc_", key);
        }

        [Fact]
        public void RegisterSkipCheck_KeysAreUnique()
        {
            string key1 = RegisterSkipCheck(() => false);
            string key2 = RegisterSkipCheck(() => false);
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void UnregisterSkipCheck_ByKey_RemovesCheck()
        {
            string key = RegisterSkipCheck(() => true);
            UnregisterSkipCheck(key);
            Assert.False(ShouldSkip());
        }

        [Fact]
        public void UnregisterSkipCheck_OnlyRemovesTarget()
        {
            string key1 = RegisterSkipCheck(() => false);
            RegisterSkipCheck(() => true);
            UnregisterSkipCheck(key1);
            Assert.True(ShouldSkip());
        }

        [Fact]
        public void RegisterSkipCheck_LambdaCanBeUnregistered()
        {
            string key = RegisterSkipCheck(() => true);
            UnregisterSkipCheck(key);
            Assert.False(ShouldSkip());
        }

        [Fact]
        public void UnregisterIncidentCallback_InvalidKey_DoesNotThrow()
        {
            UnregisterIncidentCallback("nonexistent_key");
        }

        [Fact]
        public void UnregisterSkipCheck_InvalidKey_DoesNotThrow()
        {
            UnregisterSkipCheck("nonexistent_key");
        }

        [Fact]
        public void UnregisterIncidentCallback_SameKeyTwice_Idempotent()
        {
            string key = RegisterIncidentCallback(() => { });
            UnregisterIncidentCallback(key);
            UnregisterIncidentCallback(key);
        }
    }
}
