using System;
using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Models.Context;
using RimMind.Presentation.Agent;
using RimMind.Presentation.Settings;
using Verse;

namespace RimMind.Presentation.Context
{
    public class ContextOrchestrator : IContextEngine
    {
        private readonly IHistoryManager _historyManager;
        private readonly ContextSettings _settings;
        private IBudgetScheduler? _scheduler;
        private EmbeddingSnapshotStore? _embeddingSnapshotStore;

        public ContextOrchestrator(IHistoryManager historyManager)
        {
            _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
            _settings = RimMindCoreMod.Settings?.Context ?? new ContextSettings();
        }

        public ContextSnapshot BuildSnapshot(ContextRequest request)
        {
            if (request == null) return new ContextSnapshot();

            var snapshot = new ContextSnapshot
            {
                MaxTokens = RimMindCoreMod.Settings?.maxTokens ?? 800,
                Temperature = RimMindCoreMod.Settings?.defaultTemperature ?? 0.7f,
            };

            var messages = new List<ChatMessage>();
            var systemPrompt = BuildSystemPrompt(request);
            if (!string.IsNullOrEmpty(systemPrompt))
                messages.Add(new ChatMessage { Role = "system", Content = systemPrompt });

            var pawnContext = BuildPawnContext(request);
            if (!string.IsNullOrEmpty(pawnContext))
                messages.Add(new ChatMessage { Role = "user", Content = pawnContext });

            snapshot.Messages = messages;
            return snapshot;
        }

        private string BuildSystemPrompt(ContextRequest request)
        {
            return "";
        }

        private string BuildPawnContext(ContextRequest request)
        {
            return "";
        }

        public IBudgetScheduler? GetScheduler() => _scheduler;
        public EmbeddingSnapshotStore? GetEmbeddingSnapshotStore() => _embeddingSnapshotStore;
    }
}
