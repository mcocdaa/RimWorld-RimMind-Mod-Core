using System.Collections.Generic;
using RimMind.Kernel.Bus;
using RimMind.Core.Comps;
using RimMind.Core.Internal;
using RimMind.Core.Npc;
using Verse;

namespace RimMind.Core.Perception
{
    public static class PerceptionBridge
    {
        public static void PublishPerception(int pawnId, string perceptionType, string content, float importance, IEventBus eventBus)
        {
            string npcId = $"NPC-{pawnId}";
            eventBus.Publish(new PerceptionEvent(npcId, pawnId, perceptionType, content, importance));
            ForwardAsSignal(perceptionType, content, importance, pawnId);
        }

        public static void PublishPerceptionForPawn(Pawn pawn, string perceptionType, string content, float importance, IEventBus eventBus)
        {
            if (pawn == null) return;
            if (pawn.Map == null) return;
            if (!CompPawnAgent.IsAgentActive(pawn)) return;
            PublishPerception(pawn.thingIDNumber, perceptionType, content, importance, eventBus);
        }

        public static void PublishBroadcast(string perceptionType, string content, float importance, IEventBus eventBus, Map? map = null)
        {
            var activeIds = RimMindServiceLocator.Get<INpcManager>()?.GetActiveAgentPawnIds();
            if (activeIds == null || activeIds.Count == 0) return;

            var maps = map != null ? new List<Map> { map } : (Find.Maps ?? new List<Map>());
            foreach (var m in maps)
            {
                if (m?.mapPawns == null) continue;
                foreach (var pawn in m.mapPawns.FreeColonistsAndPrisoners)
                {
                    if (activeIds.Contains(pawn.thingIDNumber))
                        PublishPerception(pawn.thingIDNumber, perceptionType, content, importance, eventBus);
                }
            }
        }

        private static void ForwardAsSignal(string perceptionType, string content, float importance, int pawnId)
        {
            try
            {
                string tag = $"RimMind.Perception.{perceptionType}";
                var args = new SignalArgs();
                args.args.Add("pawnId", pawnId);
                args.args.Add("perceptionType", perceptionType);
                args.args.Add("content", content);
                args.args.Add("importance", importance);
                Find.SignalManager?.SendSignal(new Signal(tag, args));
            }
            catch { }
        }

        public static void ForwardDecisionAsSignal(string action, string reason, int pawnId)
        {
            try
            {
                string tag = $"RimMind.Decision.{action}";
                var args = new SignalArgs();
                args.args.Add("pawnId", pawnId);
                args.args.Add("action", action);
                args.args.Add("reason", reason);
                Find.SignalManager?.SendSignal(new Signal(tag, args));
            }
            catch { }
        }
    }
}
