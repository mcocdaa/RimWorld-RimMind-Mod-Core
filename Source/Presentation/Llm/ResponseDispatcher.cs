using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Npc;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Llm
{
    public class ResponseDispatcher
    {
        private readonly IEventBus _eventBus;

        public ResponseDispatcher(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void Dispatch(NpcChatContext context, AIResponse response)
        {
            if (context == null || response == null) return;

            if (context.Pawn != null)
            {
                _eventBus.Publish(new ActionEvent(
                    $"NPC-{context.Pawn.thingIDNumber}",
                    context.Pawn.thingIDNumber,
                    "chat_response",
                    "",
                    true));
            }
        }

        public Result<NpcChatResult, RimMindError> ToResult(AIResponse response)
        {
            if (response == null)
                return Result<NpcChatResult, RimMindError>.Err(RimMindErrors.Internal("Null response"));

            var result = new NpcChatResult
            {
                Content = response.Content ?? "",
                FinishReason = response.FinishReason ?? "",
                TokenUsage = response.Usage?.TotalTokens ?? 0,
            };

            return Result<NpcChatResult, RimMindError>.Ok(result);
        }
    }
}
