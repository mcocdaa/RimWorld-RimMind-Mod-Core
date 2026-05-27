namespace RimMind.Domain.Llm
{
    public sealed class LlmResponse
    {
        public string RequestId { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string? ToolCallsJson { get; init; }
        public string? ReasoningContent { get; init; }
        public int TokensUsed { get; init; }
        public int PromptTokens { get; init; }
        public int CompletionTokens { get; init; }
        public int CachedTokens { get; init; }
        public AIRequestState State { get; init; }
        public AIRequestPriority Priority { get; init; }
        public int AttemptCount { get; init; }
        public long QueueWaitMs { get; init; }
        public long ProcessingMs { get; init; }
        public long HttpStatusCode { get; init; }
    }
}
