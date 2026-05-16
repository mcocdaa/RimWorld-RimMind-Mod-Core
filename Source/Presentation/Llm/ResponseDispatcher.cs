using System;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models.Npc;
using RimMind.Domain.Events;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Pipeline.Npc;

namespace RimMind.Presentation.Llm
{
    public class ResponseDispatcher : IResponseDispatcher
    {
        private readonly IEventBus _eventBus;

        public ResponseDispatcher(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public void DispatchChatResponse(string npcId, string requestId)
        {
            if (string.IsNullOrEmpty(npcId)) return;
            int pawnId = 0;
            var idSpan = npcId.AsSpan();
            if (idSpan.StartsWith("NPC-") && int.TryParse(idSpan.Slice(4), out var pid))
                pawnId = pid;
            _eventBus.Publish(new ActionEvent(
                npcId,
                pawnId,
                "chat_response",
                true,
                "",
                requestId));
        }

        public void Dispatch(NpcChatContext context, AIResponse response)
        {
            if (context == null || response == null) return;

            var npcId = context.Request?.NpcId;
            DispatchChatResponse(npcId ?? "", response.RequestId);
        }
    }
}
