using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Debug;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public sealed class AIRequestTraceLog : IAIRequestTraceLog
    {
        private const int MaxEntries = RimMindDefaults.DebugMaxEntries;
        private readonly object _lock = new();
        private readonly List<AIRequestTraceEntry> _entries = new();

        public IReadOnlyList<AIRequestTraceEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries.ToList();
                }
            }
        }

        public void StartRequest(string requestId, string source, string model, string systemPrompt, string userPrompt, string assistantPrompt)
        {
            lock (_lock)
            {
                var existing = _entries.FirstOrDefault(e => e.RequestId == requestId);
                if (existing != null)
                {
                    existing.Source = source;
                    existing.Model = model;
                    existing.SystemPrompt = systemPrompt;
                    existing.UserPrompt = userPrompt;
                    existing.AssistantPrompt = assistantPrompt;
                    existing.Response = string.Empty;
                    existing.Error = null;
                    existing.TokensUsed = 0;
                    existing.ElapsedMs = 0;
                    existing.State = AIRequestTraceState.Running;
                    existing.ToolCalls.Clear();
                    return;
                }

                TrimIfNeeded();
                _entries.Add(new AIRequestTraceEntry
                {
                    RequestId = requestId,
                    Source = source,
                    Model = model,
                    SystemPrompt = systemPrompt,
                    UserPrompt = userPrompt,
                    AssistantPrompt = assistantPrompt,
                    State = AIRequestTraceState.Running
                });
            }
        }

        public void CompleteRequest(string requestId, string response, int tokensUsed, int elapsedMs)
        {
            lock (_lock)
            {
                var entry = FindOrCreate(requestId);
                entry.Response = response;
                entry.TokensUsed = tokensUsed;
                entry.ElapsedMs = elapsedMs;
                entry.State = AIRequestTraceState.Completed;
            }
        }

        public void FailRequest(string requestId, string error)
        {
            lock (_lock)
            {
                var entry = FindOrCreate(requestId);
                entry.Error = error;
                entry.State = AIRequestTraceState.Failed;
            }
        }

        public void AddToolCall(string requestId, string toolCallId, string toolName, bool succeeded, string? error)
        {
            lock (_lock)
            {
                var entry = FindOrCreate(requestId);
                entry.ToolCalls.Add(new AIRequestToolCallTrace(
                    toolCallId, toolName, succeeded, error));
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _entries.Clear();
            }
        }

        private AIRequestTraceEntry FindOrCreate(string requestId)
        {
            var entry = _entries.FirstOrDefault(e => e.RequestId == requestId);
            if (entry != null) return entry;

            Log.Warning($"[RimMind-Core] AIRequestTraceLog.FindOrCreate: no prior StartRequest for '{requestId}', creating orphan entry");
            TrimIfNeeded();
            entry = new AIRequestTraceEntry { RequestId = requestId };
            _entries.Add(entry);
            return entry;
        }

        private void TrimIfNeeded()
        {
            if (_entries.Count < MaxEntries) return;
            int excess = _entries.Count - MaxEntries + 1;
            _entries.RemoveRange(0, excess);
        }
    }
}
