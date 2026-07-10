using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Mechanisms;
using Verse;

namespace RimMind.Infrastructure.UI.DebugTables
{
    public sealed class MechanismsDebugTableModelBuilder : IDebugTableModelBuilder
    {
        private readonly IGameMechanismRegistry? _registry;

        public MechanismsDebugTableModelBuilder(IGameMechanismRegistry? registry)
        {
            _registry = registry;
        }

        public DebugTableModel Build()
            => Build(_registry?.All ?? System.Array.Empty<IGameMechanism>());

        public static DebugTableModel Build(IReadOnlyList<IGameMechanism> mechanisms)
        {
            return new DebugTableModel(
                "RimMind.UI.Hub.Tab.Mechanisms".Translate(),
                mechanisms.Select(ToRow));
        }

        private static DebugTableRow ToRow(IGameMechanism mechanism)
        {
            string operations = mechanism.SupportedOperations.Count > 0
                ? string.Join(", ", mechanism.SupportedOperations.Select(op => op.ToString()))
                : "RimMind.UI.DebugTable.Mechanisms.NoOperations".Translate().ToString();

            return DebugTableRow.Create(
                mechanism.MechanismId,
                DebugTableStatus.Completed,
                string.Empty,
                mechanism.Scope.ToString(),
                mechanism.OwnerModId ?? string.Empty,
                operations,
                mechanism.Risk.ToString(),
                mechanism.Docs.Summary,
                "RimMind.UI.DebugTable.Mechanisms.OperationCount".Translate(mechanism.SupportedOperations.Count).ToString());
        }
    }
}
