namespace RimMind.Contracts.Client
{
    public class AIResponse
    {
        public string Content { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int CachedTokens { get; set; }
        public string RequestId { get; init; } = string.Empty;
        public AIRequestState State { get; init; } = AIRequestState.Queued;
        public AIRequestPriority Priority { get; set; } = AIRequestPriority.Normal;
        public int AttemptCount { get; set; } = 1;
        public long QueueWaitMs { get; set; }
        public long ProcessingMs { get; set; }
        public long HttpStatusCode { get; set; }
        public int RequestPayloadBytes { get; set; }
        public string? ToolCallsJson { get; set; }
        public string? ReasoningContent { get; set; }

        public static AIResponse Ok(string requestId, string content, int tokens) => new AIResponse
        {
            Content = content,
            TokensUsed = tokens,
            RequestId = requestId,
            State = AIRequestState.Completed
        };
    }
}
