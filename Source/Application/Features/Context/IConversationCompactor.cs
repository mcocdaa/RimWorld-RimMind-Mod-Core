using System.Collections.Generic;
using RimMind.Domain.Llm;

namespace RimMind.Application.Features.Context
{
    public interface IConversationCompactor
    {
        List<ChatMessage> Compact(List<ChatMessage> messages, int maxMessages = 10);
    }
}
