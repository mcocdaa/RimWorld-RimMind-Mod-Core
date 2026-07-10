using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.ValueObjects;
using Verse;

namespace RimMind.Infrastructure.UI.DebugTables
{
    public sealed class ContextKeysDebugTableModelBuilder : IDebugTableModelBuilder
    {
        private readonly IContextKeyRegistry? _registry;

        public ContextKeysDebugTableModelBuilder(IContextKeyRegistry? registry)
        {
            _registry = registry;
        }

        public DebugTableModel Build()
            => Build(_registry?.GetAll() ?? System.Array.Empty<KeyMeta>());

        public static DebugTableModel Build(IReadOnlyList<KeyMeta> keys)
        {
            return new DebugTableModel(
                "RimMind.UI.Hub.Tab.ContextKeys".Translate(),
                keys.Select(ToRow));
        }

        private static DebugTableRow ToRow(KeyMeta key)
        {
            string priority = key.Priority.ToString("0.###", CultureInfo.InvariantCulture);
            string effectivePriority = key.GetEffectivePriority().ToString("0.###", CultureInfo.InvariantCulture);
            string summary = "RimMind.UI.DebugTable.ContextKeys.Priority".Translate(priority).ToString()
                + " | " + "RimMind.UI.DebugTable.ContextKeys.Effective".Translate(effectivePriority).ToString()
                + " | " + "RimMind.UI.DebugTable.ContextKeys.UpdateCount".Translate(key.UpdateCount).ToString();

            return DebugTableRow.Create(
                key.Key,
                DebugTableStatus.Completed,
                string.Empty,
                key.Layer.ToString(),
                key.OwnerMod ?? string.Empty,
                key.CacheScope.ToString(),
                "RimMind.UI.DebugTable.ContextKeys.Model".Translate().ToString(),
                summary,
                "RimMind.UI.DebugTable.ContextKeys.Score"
                    .Translate(key.CurrentScore.ToString("0.###", CultureInfo.InvariantCulture))
                    .ToString());
        }
    }
}
