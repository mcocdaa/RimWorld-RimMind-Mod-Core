using System.Collections.Generic;

namespace RimMind.Core.Client
{
    public class AIRequest
    {
        public string SystemPrompt { get; set; } = string.Empty;

        public string UserPrompt { get; set; } = string.Empty;

        public List<ChatMessage>? Messages { get; set; }

        public int MaxTokens { get; set; } = 800;
        public float Temperature { get; set; } = 0.7f;

        public string RequestId { get; set; } = string.Empty;

        public string ModId { get; set; } = string.Empty;

        public int ExpireAtTicks { get; set; }

        public bool UseJsonMode { get; set; } = true;

        public string? JsonSchema { get; set; }

        public List<StructuredTool>? Tools { get; set; }

        public AIRequestPriority Priority { get; set; } = AIRequestPriority.Normal;

        public int? MaxRetryCount { get; set; } = null;

        public void Reset()
        {
            SystemPrompt = string.Empty;
            UserPrompt = string.Empty;
            Messages = null;
            MaxTokens = 800;
            Temperature = 0.7f;
            RequestId = string.Empty;
            ModId = string.Empty;
            ExpireAtTicks = 0;
            UseJsonMode = true;
            JsonSchema = null;
            Tools = null;
            Priority = AIRequestPriority.Normal;
            MaxRetryCount = null;
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ReasoningContent { get; set; }
        public string? ToolCallId { get; set; }
        public List<ChatToolCall>? ToolCalls { get; set; }
        public string? LayerTag { get; set; }
    }

    public class ChatToolCall
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
    }
}
