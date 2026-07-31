using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimMind.Domain.Llm;

namespace RimMind.Application.Features.Context
{
    // TODO: Integrate into ContextOrchestrator conversation history management. Currently unused.
    public sealed class SummaryConversationCompactor : IConversationCompactor
    {
        public List<ChatMessage> Compact(List<ChatMessage> messages, int maxMessages = 10)
        {
            if (messages == null) return new List<ChatMessage>();
            if (messages.Count <= maxMessages) return messages;

            var keepCount = maxMessages - 1;
            var oldMessages = messages.Take(messages.Count - keepCount).ToList();
            var recentMessages = messages.Skip(messages.Count - keepCount).ToList();

            var summary = BuildSummary(oldMessages);
            var summaryMessage = new ChatMessage
            {
                Role = "system",
                Content = $"<conversation_summary>{oldMessages.Count} messages compacted: {summary}</conversation_summary>"
            };

            var result = new List<ChatMessage> { summaryMessage };
            result.AddRange(recentMessages);
            return result;
        }

        private static string BuildSummary(List<ChatMessage> oldMessages)
        {
            var sb = new StringBuilder();
            foreach (var msg in oldMessages)
            {
                if (msg.Role == "assistant" && !string.IsNullOrEmpty(msg.Content))
                {
                    var content = msg.Content.Length > 100 ? msg.Content.Substring(0, 100) + "..." : msg.Content;
                    sb.AppendLine($"- {content}");
                }
            }
            return sb.Length > 500 ? sb.ToString().Substring(0, 500) + "..." : sb.ToString();
        }
    }
}
