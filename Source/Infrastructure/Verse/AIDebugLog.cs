using System.Collections.Concurrent;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using RimMind.Domain.Llm;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimMind.Infrastructure.Verse
{
    public class AIDebugLog : GameComponent, IAIDebugLog
    {
        private const int MaxEntries = RimMindDefaults.DebugMaxEntries;

        private readonly Queue<AIDebugEntry> _entries = new Queue<AIDebugEntry>(MaxEntries);
        private readonly ConcurrentQueue<AIDebugEntry> _pendingEntries = new ConcurrentQueue<AIDebugEntry>();
        private IAIModelSettings? _cachedModelSettings;

        // [Framework-Forced SL] Verse GameComponent requires parameterless constructor.
        private IAIModelSettings? GetModelSettings()
            => _cachedModelSettings ??= RimMindServiceLocator.Get<IAIModelSettings>();

        // NOTE: AIDebugLog.Instance static accessor removed (v8 audit: zero external callers).
        // Use IAIDebugLog via constructor injection or RimMindServiceLocator.Get<IAIDebugLog>().

        public AIDebugLog(Game game)
        {
            RimMindServiceLocator.Register<IAIDebugLog>(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                RimMindServiceLocator.Register<IAIDebugLog>(this);
        }

        public IReadOnlyList<AIDebugEntry> Entries => _entries.ToList();

        public override void GameComponentTick()
        {
            while (_pendingEntries.TryDequeue(out var entry))
            {
                entry.GameTick = Find.TickManager.TicksGame;
                if (_entries.Count >= MaxEntries)
                    _entries.Dequeue();
                _entries.Enqueue(entry);
            }
        }

        public void Clear() => _entries.Clear();

        private static string BuildLayeredText(List<ChatMessage> messages, params string[] roles)
        {
            var sb = new StringBuilder();
            foreach (var m in messages)
            {
                if (roles == null || roles.Length == 0 || roles.Contains(m.Role))
                {
                    if (sb.Length > 0)
                        sb.AppendLine().AppendLine();
                    string tag = !string.IsNullOrEmpty(m.LayerTag) ? $"[{m.LayerTag}] " : "";
                    sb.AppendLine($"{tag}{m.Content}");
                }
            }
            return sb.ToString();
        }

        public void Record(LlmRequestEnvelope envelope, LlmResponse response, int elapsedMs)
        {
            _pendingEntries.Enqueue(new AIDebugEntry
            {
                Source = envelope.RequestId ?? "",
                ModelName = GetModelSettings()?.ModelName ?? "",
                FullSystemPrompt = envelope.Messages != null
                    ? BuildLayeredText(envelope.Messages, "system")
                    : "",
                FullUserPrompt = envelope.Messages != null
                    ? BuildLayeredText(envelope.Messages, "user")
                    : "",
                FullAssistantPrompt = envelope.Messages != null
                    ? BuildLayeredText(envelope.Messages, "assistant")
                    : "",
                FullResponse = response.Content ?? "",
                ElapsedMs = elapsedMs,
                TokensUsed = response.TokensUsed,
                IsError = false,
                ErrorMsg = "",
                Priority = (AIRequestPriority)(int)envelope.Priority,
                State = AIRequestState.Completed,
                AttemptCount = response.AttemptCount,
                QueueWaitMs = response.QueueWaitMs,
                ProcessingMs = response.ProcessingMs,
                HttpStatusCode = response.HttpStatusCode,
                RequestPayloadBytes = 0,
            });
        }

    }
}
