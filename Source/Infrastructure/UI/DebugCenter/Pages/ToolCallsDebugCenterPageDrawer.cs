using System;
using RimMind.Application.Common.Models.UI;
using RimMind.Infrastructure.UI.DebugTables;
using RimMind.Infrastructure.UI.Framework;
using RimMind.Presentation.UI.Framework;
using RimMind.Presentation.UI.Layout;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI.DebugCenter.Pages
{
    public sealed class ToolCallsDebugCenterPageDrawer : IDebugCenterPageDrawer
    {
        private const int DebugTableColumnCount = 8;
        private readonly RimMindTableDrawer _tableDrawer = new();
        private Vector2 _scrollPosition;

        public DebugCenterPageDescriptor Descriptor { get; } = new(
            "tool_calls",
            "RimMind.UI.Hub.Tab.ToolCalls",
            30,
            IsDefault: false);

        public void Draw(Rect rect, DebugCenterPageContext context, RimMindLayoutScope scope)
        {
            DebugTableModel model = new DebugTableModel(
                "RimMind.UI.Hub.Tab.ToolCalls".Translate(),
                Array.Empty<DebugTableRow>());
            _ = TablePageLayout.Calculate(rect, model.Rows.Count, columnCount: DebugTableColumnCount);
            _tableDrawer.Draw(rect, model, ref _scrollPosition, scope);
        }
    }
}
