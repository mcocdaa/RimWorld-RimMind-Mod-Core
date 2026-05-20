using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Models.Agent;
using RimMind.Presentation.Context;
using RimMind.Presentation.Runtime;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RimMind.Presentation
{
    public static partial class RimMindAPI
    {
        public static class Ext
        {
            public static IExtensionRegistry<T> Get<T>() where T : class, IExtension
                => RimMindRuntime.Instance.GetExtensionRegistry<T>();

            public static bool ShouldSkipDialogue(Pawn pawn, string trigger)
                => Get<ISkipCheck>().All
                    .Where(s => s.Kind == SkipCheckKind.Dialogue)
                    .Any(s => s.ShouldSkip(new SkipCheckArgs { Pawn = pawn, Trigger = trigger }));

            public static bool ShouldSkipFloatMenu()
                => Get<ISkipCheck>().All
                    .Where(s => s.Kind == SkipCheckKind.FloatMenu)
                    .Any(s => s.ShouldSkip(default));

            public static bool ShouldSkipAction(string intentId)
                => Get<ISkipCheck>().All
                    .Where(s => s.Kind == SkipCheckKind.Action)
                    .Any(s => s.ShouldSkip(new SkipCheckArgs { IntentId = intentId }));

            public static bool ShouldSkipStorytellerIncident()
                => Get<ISkipCheck>().All
                    .Where(s => s.Kind == SkipCheckKind.StorytellerIncident)
                    .Any(s => s.ShouldSkip(default));

            public static void TriggerDialogue(Pawn pawn, string context, Pawn? recipient = null)
            {
                // IDialogueTrigger removed (dead interface, no implementations)
            }

            public static void NotifyIncidentExecuted()
            {
                // IIncidentExecutedListener removed (dead interface, no implementations)
            }

            public static bool CanTriggerDialogue
                => false;

            public static void RegisterAgentIdentityProvider(Func<Pawn, AgentIdentity?> provider)
                => RimMindRuntime.Instance.RegisterAgentIdentityProvider(provider);

            public static AgentIdentity? GetAgentIdentity(Pawn pawn)
                => RimMindRuntime.Instance.GetAgentIdentity(pawn);

            public static void RegisterAgentActionBridge(IAgentActionBridge bridge)
                => RimMindRuntime.Instance.RegisterAgentActionBridge(bridge);

            public static IAgentActionBridge GetAgentActionBridge()
                => RimMindRuntime.Instance.GetAgentActionBridge();

            public static void RegisterParameterTuner(IParameterTuner tuner)
                => RimMindRuntime.Instance.RegisterParameterTuner(tuner);

            public static IReadOnlyList<IParameterTuner> ParameterTuners
                => RimMindRuntime.Instance.ParameterTunersList;

            public static void RegisterPawnContextProvider(string key, Func<Pawn, string?> provider, int priority = 8)
            {
                ContextKeyRegistry.Register(key, ContextLayer.L4_History, priority / 10f,
                    pawnObj =>
                    {
                        var p = pawnObj as Pawn;
                        if (p == null) return new List<ContextEntry>();
                        var value = provider(p);
                        return string.IsNullOrEmpty(value) ? new List<ContextEntry>() : new List<ContextEntry> { new ContextEntry(value) };
                    }, "External");
            }
        }
    }
}
