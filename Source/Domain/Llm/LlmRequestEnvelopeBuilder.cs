using System;
using System.Collections.Generic;
using System.Threading;

namespace RimMind.Domain.Llm
{
    public sealed class LlmRequestEnvelopeBuilder
    {
        private string? _scenarioId;
        private string? _modId;
        private string? _npcId;
        private string? _gameStateInfo;
        private string? _jsonSchema;
        private List<StructuredTool>? _tools;
        private List<ChatMessage>? _messages;
        private AIRequestPriority _priority = AIRequestPriority.Normal;
        private bool _isStreaming;
        private Action<LlmChunk>? _onStreamChunk;
        private CancellationToken _ct = default;
        private int _maxTokens = 800;
        private float _temperature = 0.7f;
        private int? _expireAtTicks;
        private int? _maxRetryCount;

        public static LlmRequestEnvelopeBuilder ForScenario(string scenarioId)
        {
            var builder = new LlmRequestEnvelopeBuilder();
            builder._scenarioId = scenarioId;
            return builder;
        }

        public static LlmRequestEnvelopeBuilder ForNpc(string npcId, string? gameStateInfo = null)
        {
            var builder = new LlmRequestEnvelopeBuilder();
            builder._npcId = npcId;
            builder._gameStateInfo = gameStateInfo;
            return builder;
        }

        public LlmRequestEnvelopeBuilder ForScenarioId(string scenarioId)
        {
            _scenarioId = scenarioId;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithModId(string modId)
        {
            _modId = modId;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithNpcId(string npcId)
        {
            _npcId = npcId;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithGameStateInfo(string? gameStateInfo)
        {
            _gameStateInfo = gameStateInfo;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithSchema(string jsonSchema)
        {
            _jsonSchema = jsonSchema;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithTools(IEnumerable<StructuredTool> tools)
        {
            _tools = new List<StructuredTool>(tools);
            return this;
        }

        public LlmRequestEnvelopeBuilder WithMessages(IEnumerable<ChatMessage> messages)
        {
            _messages = new List<ChatMessage>(messages);
            return this;
        }

        public LlmRequestEnvelopeBuilder WithPriority(AIRequestPriority priority)
        {
            _priority = priority;
            return this;
        }

        public LlmRequestEnvelopeBuilder Streaming(Action<LlmChunk> onChunk)
        {
            _isStreaming = true;
            _onStreamChunk = onChunk;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithCancellation(CancellationToken ct)
        {
            _ct = ct;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithMaxTokens(int max)
        {
            _maxTokens = max;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithTemperature(float t)
        {
            _temperature = t;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithExpireAtTicks(int? ticks)
        {
            _expireAtTicks = ticks;
            return this;
        }

        public LlmRequestEnvelopeBuilder WithMaxRetryCount(int? count)
        {
            _maxRetryCount = count;
            return this;
        }

        public LlmRequestEnvelope Build()
        {
            if (string.IsNullOrEmpty(_scenarioId))
                throw new InvalidOperationException("ScenarioId is required. Use ForScenario() or ForScenarioId().");

            return new LlmRequestEnvelope
            {
                RequestId = Guid.NewGuid().ToString("N"),
                ScenarioId = _scenarioId!,
                ModId = _modId ?? _scenarioId!,
                Messages = _messages ?? new List<ChatMessage>(),
                JsonSchema = _jsonSchema,
                Tools = _tools,
                MaxTokens = _maxTokens,
                Temperature = _temperature,
                Priority = _priority,
                ExpireAtTicks = _expireAtTicks,
                MaxRetryCount = _maxRetryCount,
                IsStreaming = _isStreaming,
                OnStreamChunk = _onStreamChunk,
                Ct = _ct,
                NpcId = _npcId,
                GameStateInfo = _gameStateInfo,
            };
        }
    }
}
