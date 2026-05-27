using System.Collections.Generic;
using HarmonyLib;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Agent;
using RimMind.Infrastructure.UI;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimMind.Infrastructure.Patches
{
    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    internal static class FloatMenu_InnerVoice
    {
        [HarmonyPostfix]
        internal static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            // Only show for pawns that have a PawnAgent
            var identityProvider = RimMindServiceLocator.Get<IAgentIdentityProvider>();
            var identity = identityProvider?.GetAgentIdentity(pawn);
            if (identity == null) return;

            var label = "RimMind.InnerVoice.Inject".Translate(pawn.LabelShort);
            var option = new FloatMenuOption(label, () =>
            {
                var dialog = new Dialog_RimMindInnerVoice(pawn, identity);
                Find.WindowStack.Add(dialog);
            }, MenuOptionPriority.Default);

            opts.Add(option);
        }
    }
}
