using System.Collections.Generic;
using RimMind.Kernel.Bus;
using RimMind.Adapters.Verse;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Npc;
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
                var signalType = System.Type.GetType("Verse.Signal, Assembly-CSharp");
                var signalArgsType = System.Type.GetType("Verse.SignalArgs, Assembly-CSharp");
                if (signalType == null || signalArgsType == null) return;
                dynamic args = System.Activator.CreateInstance(signalArgsType);
                args.args["pawnId"] = pawnId;
                args.args["perceptionType"] = perceptionType;
                args.args["content"] = content;
                args.args["importance"] = importance;
                dynamic signal = System.Activator.CreateInstance(signalType, tag, args);
                Find.SignalManager?.SendSignal(signal);
            }
            catch { }
        }

        public static void ForwardDecisionAsSignal(string action, string reason, int pawnId)
        {
            try
            {
                string tag = $"RimMind.Decision.{action}";
                var signalType = System.Type.GetType("Verse.Signal, Assembly-CSharp");
                var signalArgsType = System.Type.GetType("Verse.SignalArgs, Assembly-CSharp");
                if (signalType == null || signalArgsType == null) return;
                dynamic args = System.Activator.CreateInstance(signalArgsType);
                args.args["pawnId"] = pawnId;
                args.args["action"] = action;
                args.args["reason"] = reason;
                dynamic signal = System.Activator.CreateInstance(signalType, tag, args);
                Find.SignalManager?.SendSignal(signal);
            }
            catch { }
        }
    }
}
