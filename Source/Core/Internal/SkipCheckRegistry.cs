using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimMind.Core.Internal
{
    public class SkipCheckRegistry
    {
        private readonly ConcurrentDictionary<string, Func<Pawn, string, bool>> _dialogueSkipChecks
            = new ConcurrentDictionary<string, Func<Pawn, string, bool>>();

        private readonly ConcurrentDictionary<string, Func<bool>> _floatMenuSkipChecks
            = new ConcurrentDictionary<string, Func<bool>>();

        private readonly ConcurrentDictionary<string, Func<string, bool>> _actionSkipChecks
            = new ConcurrentDictionary<string, Func<string, bool>>();

        private readonly ConcurrentDictionary<string, Func<bool>> _storytellerIncidentSkipChecks
            = new ConcurrentDictionary<string, Func<bool>>();

        public void RegisterDialogueSkipCheck(string sourceId, Func<Pawn, string, bool> skipCheck)
            => _dialogueSkipChecks[sourceId] = skipCheck;

        public void UnregisterDialogueSkipCheck(string sourceId)
            => _dialogueSkipChecks.TryRemove(sourceId, out _);

        public bool ShouldSkipDialogue(Pawn pawn, string triggerType)
        {
            foreach (var kvp in _dialogueSkipChecks.Values.ToList())
            {
                try { if (kvp(pawn, triggerType)) return true; }
                catch (Exception ex) { Log.Warning($"[RimMind-Core] DialogueSkipCheck error: {ex.Message}"); }
            }
            return false;
        }

        public void RegisterFloatMenuSkipCheck(string sourceId, Func<bool> skipCheck)
            => _floatMenuSkipChecks[sourceId] = skipCheck;

        public void UnregisterFloatMenuSkipCheck(string sourceId)
            => _floatMenuSkipChecks.TryRemove(sourceId, out _);

        public bool ShouldSkipFloatMenu()
        {
            foreach (var check in _floatMenuSkipChecks.Values.ToList())
            {
                try { if (check()) return true; }
                catch (Exception ex) { Log.Warning($"[RimMind-Core] FloatMenuSkipCheck error: {ex.Message}"); }
            }
            return false;
        }

        public void RegisterActionSkipCheck(string sourceId, Func<string, bool> skipCheck)
            => _actionSkipChecks[sourceId] = skipCheck;

        public void UnregisterActionSkipCheck(string sourceId)
            => _actionSkipChecks.TryRemove(sourceId, out _);

        public bool ShouldSkipAction(string intentId)
        {
            foreach (var check in _actionSkipChecks.Values.ToList())
            {
                try { if (check(intentId)) return true; }
                catch (Exception ex) { Log.Warning($"[RimMind-Core] ActionSkipCheck error: {ex.Message}"); }
            }
            return false;
        }

        public string RegisterStorytellerIncidentSkipCheck(Func<bool> check, ref int callbackCounter)
        {
            string key = $"sc_{System.Threading.Interlocked.Increment(ref callbackCounter)}";
            _storytellerIncidentSkipChecks[key] = check;
            return key;
        }

        public void UnregisterStorytellerIncidentSkipCheck(string key)
            => _storytellerIncidentSkipChecks.TryRemove(key, out _);

        public bool ShouldSkipStorytellerIncident()
        {
            foreach (var check in _storytellerIncidentSkipChecks.Values.ToList())
            {
                try { if (check()) return true; }
                catch (Exception ex) { Log.Warning($"[RimMind-Core] StorytellerIncidentSkipCheck error: {ex.Message}"); }
            }
            return false;
        }

        public void Reset()
        {
            _dialogueSkipChecks.Clear();
            _floatMenuSkipChecks.Clear();
            _actionSkipChecks.Clear();
            _storytellerIncidentSkipChecks.Clear();
        }
    }
}
