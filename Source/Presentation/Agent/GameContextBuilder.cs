using System.Collections.Generic;
using System.Text;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Npc;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Prompt;
using RimMind.Application.Features.Context;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Agent;
using RimWorld;
using Verse;

namespace RimMind.Presentation.Agent
{
    /// <summary>
    /// Thin facade that delegates to PawnContextBuilder and MapContextBuilder.
    /// Implements IGameContextBuilder (CollectBasicGameState) and IContextKeyProvider (Extract* methods).
    /// </summary>
    public class GameContextBuilder : IGameContextBuilder, IContextKeyProvider
    {
        private readonly PawnContextBuilder _pawnBuilder;
        private readonly MapContextBuilder _mapBuilder;
        private readonly INpcManager? _npcManager;

        public GameContextBuilder(IContextSettings? contextSettings = null, INpcManager? npcManager = null)
        {
            _pawnBuilder = new PawnContextBuilder(contextSettings);
            _mapBuilder = new MapContextBuilder(contextSettings);
            _npcManager = npcManager;
        }

        // --- IGameContextBuilder ---

        public string CollectBasicGameState(string npcId)
        {
            var sb = new StringBuilder();
            var pawnObj = _npcManager?.FindPawnByNpcId(npcId);
            var pawn = pawnObj as Pawn;

            if (pawn != null)
            {
                if (pawn.Map != null)
                    sb.AppendLine(_mapBuilder.BuildMapContext(pawn.Map));
                sb.AppendLine(_pawnBuilder.BuildPawnContext(pawn));
            }
            else
            {
                var map = Find.CurrentMap;
                if (map != null)
                    sb.AppendLine(_mapBuilder.BuildMapContext(map));
            }

            return sb.ToString().TrimEnd();
        }

        // --- Static facade methods (backward compatibility) ---

        private static GameContextBuilder? _default;

        internal static void SetDefault(GameContextBuilder builder) => _default = builder;

        private static GameContextBuilder Default
        {
            get
            {
                if (_default == null)
                    _default = RimMindServiceLocator.Get<IGameContextBuilder>() as GameContextBuilder;
                return _default!;
            }
        }

        public static string BuildMapContext(Map map, bool brief = false) => Default._mapBuilder.BuildMapContext(map, brief);

        public static List<ContextEntry> BuildMapContextEntries(Map map, bool brief = false) => Default._mapBuilder.BuildMapContextEntries(map, brief);

        public static string BuildPawnContext(Pawn pawn) => Default._pawnBuilder.BuildPawnContext(pawn);

        public static string BuildCompactPawnContext(Pawn pawn) => Default._pawnBuilder.BuildCompactPawnContext(pawn);

        public static PromptSection BuildMapContextSection(Map map, bool brief = false) => Default._mapBuilder.BuildMapContextSection(map, brief);

        public static PromptSection BuildPawnContextSection(Pawn pawn) => Default._pawnBuilder.BuildPawnContextSection(pawn);

        public static PromptSection BuildCompactPawnContextSection(Pawn pawn) => Default._pawnBuilder.BuildCompactPawnContextSection(pawn);

        // --- IContextKeyProvider ---

        public List<ContextEntry> BuildMapContextEntries(object map)
        {
            if (map is Map m) return _mapBuilder.BuildMapContextEntries(m);
            return new List<ContextEntry>();
        }

        public string ExtractPawnBaseInfo(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractPawnBaseInfo(p) : "";
        public string ExtractFixedRelations(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractFixedRelations(p) : "";
        public string ExtractIdeology(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractIdeology(p) : "";
        public string ExtractSkillsSummary(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractSkillsSummary(p) : "";
        public string ExtractCurrentArea(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractCurrentArea(p) : "";
        public string ExtractWeather(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractWeather(p) : "";
        public string ExtractTimeOfDay(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractTimeOfDay(p) : "";
        public string ExtractNearbyPawns(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractNearbyPawns(p) : "";
        public string ExtractSeason(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractSeason(p) : "";
        public string ExtractColonyStatus(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractColonyStatus(p) : "";
        public string ExtractHealth(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractHealth(p) : "";
        public string ExtractMood(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractMood(p) : "";
        public string ExtractCurrentJob(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractCurrentJob(p) : "";
        public string ExtractCombatStatus(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractCombatStatus(p) : "";
        public string ExtractTargetInfo(object pawn) => pawn is Pawn p ? _pawnBuilder.ExtractTargetInfo(p) : "";
        public string ExtractTaskProgress(object pawn) => "";

        // --- Cache reset ---

        internal static void ResetCache() { _default = null; }
    }
}
