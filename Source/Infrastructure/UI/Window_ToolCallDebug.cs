using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Common.Models.Tools;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_ToolCallDebug : Window
    {
        private Vector2 _scrollPos = Vector2.zero;
        private Vector2 _detailScrollPos = Vector2.zero;
        private const float Padding = 6f;
        private const float EntryH = 24f;
        private const float TruncateLen = 500f;
        private const float LeftRatio = 0.35f;

        private string? _selectedToolId;
        private string _jsonInput = "{}";
        private string _executionResult = "";
        private bool _isExecuting;

        public override Vector2 InitialSize => new Vector2(640f, 520f);

        public Window_ToolCallDebug()
        {
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            float headerH = 30f;

            Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, headerH);
            GUI.color = new Color(0.7f, 0.8f, 1f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "RimMind.UI.ToolCallDebug.Title".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect bodyRect = new Rect(inRect.x, inRect.y + headerH + Padding,
                inRect.width, inRect.height - headerH - Padding);

            var registry = RimMindAPI.Tools;
            if (registry == null)
            {
                DrawEmptyState(bodyRect);
                return;
            }

            var defs = registry.GetAllDefinitions();
            if (defs.Count == 0)
            {
                DrawEmptyState(bodyRect);
                return;
            }

            float leftW = bodyRect.width * LeftRatio;
            float rightW = bodyRect.width - leftW - Padding;

            Rect leftRect = new Rect(bodyRect.x, bodyRect.y, leftW, bodyRect.height);
            Rect rightRect = new Rect(bodyRect.x + leftW + Padding, bodyRect.y, rightW, bodyRect.height);

            DrawToolList(leftRect, defs);
            DrawToolDetail(rightRect, defs);
        }

        private void DrawEmptyState(Rect rect)
        {
            float centerY = rect.y + rect.height / 2f;

            GUI.color = Color.grey;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, centerY - 30f, rect.width, 22f),
                "RimMind.UI.ToolCallDebug.Empty".Translate());

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            string hint = "RimMind.UI.ToolCallDebug.EmptyHint".Translate();
            float hintH = Text.CalcHeight(hint, rect.width - 24f);
            Widgets.Label(new Rect(rect.x + 12f, centerY, rect.width - 24f, hintH), hint);
            Text.Font = GameFont.Small;

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private void DrawToolList(Rect rect, IReadOnlyList<ToolDefinition> defs)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.12f, 0.5f));

            var grouped = defs.GroupBy(d => d.Category).OrderBy(g => g.Key).ToList();

            float contentH = 0f;
            foreach (var group in grouped)
            {
                contentH += EntryH;
                foreach (var _ in group)
                    contentH += EntryH;
            }

            Rect viewRect = new Rect(rect.x, rect.y, rect.width - 16f, contentH);
            Widgets.BeginScrollView(rect, ref _scrollPos, viewRect);

            float y = viewRect.y;
            foreach (var group in grouped)
            {
                GUI.color = new Color(0.6f, 0.75f, 0.9f);
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(viewRect.x + Padding, y, viewRect.width - Padding * 2, EntryH),
                    "RimMind.UI.ToolCallDebug.Category".Translate(group.Key));
                GUI.color = Color.white;
                y += EntryH;

                foreach (var def in group)
                {
                    Rect entryRect = new Rect(viewRect.x, y, viewRect.width, EntryH);
                    bool selected = _selectedToolId == def.Id;
                    if (selected)
                        Widgets.DrawBoxSolid(entryRect, new Color(0.25f, 0.35f, 0.55f, 0.6f));

                    if (Widgets.ButtonInvisible(entryRect))
                        _selectedToolId = def.Id;

                    GUI.color = selected ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                    Widgets.Label(new Rect(entryRect.x + Padding * 2, entryRect.y + 2f,
                        entryRect.width - Padding * 3, EntryH), def.Id);
                    GUI.color = Color.white;
                    y += EntryH;
                }
            }

            Widgets.EndScrollView();
        }

        private void DrawToolDetail(Rect rect, IReadOnlyList<ToolDefinition> defs)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.08f, 0.12f, 0.3f));

            if (_selectedToolId == null)
            {
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimMind.UI.ToolCallDebug.SelectTool".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            var selected = defs.FirstOrDefault(d => d.Id == _selectedToolId);
            if (selected == null)
            {
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "RimMind.UI.ToolCallDebug.SelectTool".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            float y = rect.y + Padding;

            // ID
            GUI.color = new Color(0.85f, 0.9f, 1f);
            Widgets.Label(new Rect(rect.x + Padding, y, rect.width - Padding * 2, EntryH),
                "RimMind.UI.ToolCallDebug.Id".Translate(selected.Id));
            GUI.color = Color.white;
            y += EntryH + Padding;

            // Description
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            float descH = Text.CalcHeight(selected.Description, rect.width - Padding * 4);
            Widgets.Label(new Rect(rect.x + Padding * 2, y, rect.width - Padding * 4, descH),
                "RimMind.UI.ToolCallDebug.Description".Translate(selected.Description));
            GUI.color = Color.white;
            y += descH + Padding;

            // ParametersSchema
            GUI.color = new Color(0.6f, 0.8f, 0.6f);
            Widgets.Label(new Rect(rect.x + Padding, y, rect.width - Padding * 2, EntryH),
                "RimMind.UI.ToolCallDebug.Schema".Translate());
            GUI.color = Color.white;
            y += EntryH;

            string schemaText = Truncate(selected.ParametersSchema, (int)TruncateLen);
            float schemaH = Text.CalcHeight(schemaText, rect.width - Padding * 4);
            GUI.color = new Color(0.65f, 0.65f, 0.65f);
            Widgets.Label(new Rect(rect.x + Padding * 2, y, rect.width - Padding * 4, schemaH), schemaText);
            GUI.color = Color.white;
            y += schemaH + Padding;

            // JSON input
            GUI.color = new Color(0.8f, 0.8f, 1f);
            Widgets.Label(new Rect(rect.x + Padding, y, rect.width - Padding * 2, EntryH),
                "RimMind.UI.ToolCallDebug.Input".Translate());
            GUI.color = Color.white;
            y += EntryH;

            float inputH = 60f;
            Rect inputRect = new Rect(rect.x + Padding, y, rect.width - Padding * 2, inputH);
            _jsonInput = Widgets.TextArea(inputRect, _jsonInput);
            y += inputH + Padding;

            // Execute button
            float btnW = 120f;
            float btnH = 28f;
            Rect btnRect = new Rect(rect.x + Padding, y, btnW, btnH);
            if (_isExecuting)
            {
                GUI.color = Color.grey;
                Widgets.ButtonText(btnRect, "RimMind.UI.ToolCallDebug.Executing".Translate());
                GUI.color = Color.white;
            }
            else
            {
                if (Widgets.ButtonText(btnRect, "RimMind.UI.ToolCallDebug.Execute".Translate()))
                    ExecuteTool(selected.Id);
            }
            y += btnH + Padding;

            // Result
            if (!_executionResult.NullOrEmpty())
            {
                GUI.color = new Color(0.8f, 0.8f, 1f);
                Widgets.Label(new Rect(rect.x + Padding, y, rect.width - Padding * 2, EntryH),
                    "RimMind.UI.ToolCallDebug.Result".Translate());
                GUI.color = Color.white;
                y += EntryH;

                float resultH = rect.yMax - y - Padding;
                if (resultH < EntryH) resultH = EntryH;

                Rect resultViewRect = new Rect(rect.x, y, rect.width - 16f,
                    Text.CalcHeight(_executionResult, rect.width - Padding * 4));
                Rect resultOuterRect = new Rect(rect.x, y, rect.width, resultH);
                Widgets.BeginScrollView(resultOuterRect, ref _detailScrollPos, resultViewRect);

                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                Widgets.Label(new Rect(resultViewRect.x + Padding, resultViewRect.y,
                    resultViewRect.width - Padding * 2, resultViewRect.height), _executionResult);
                GUI.color = Color.white;

                Widgets.EndScrollView();
            }
        }

        private void ExecuteTool(string toolId)
        {
            if (_isExecuting) return;

            var registry = RimMindAPI.Tools;
            if (registry == null) return;

            var handler = registry.FindById(toolId);
            if (handler == null)
            {
                _executionResult = "RimMind.UI.ToolCallDebug.Failed".Translate(0) + "\n" + "RimMind.UI.ToolCallDebug.Id".Translate(toolId);
                return;
            }

            _isExecuting = true;
            _executionResult = "RimMind.UI.ToolCallDebug.Executing".Translate();
            string jsonInput = _jsonInput;

            var sw = Stopwatch.StartNew();

            Task.Run(async () =>
            {
                try
                {
                    var args = new ToolCallArgs
                    {
                        ToolCallId = "debug-1",
                        ToolName = toolId,
                        ArgumentsJson = jsonInput,
                        PawnId = null,
                        NpcId = null,
                        Ct = CancellationToken.None,
                        TraceId = "debug"
                    };

                    var result = await handler.ExecuteAsync(args, CancellationToken.None);
                    sw.Stop();
                    long elapsed = sw.ElapsedMilliseconds;

                    string resultText;
                    if (result.IsOk)
                    {
                        var toolResult = result.Value;
                        string statusKey = toolResult.IsError
                            ? "RimMind.UI.ToolCallDebug.Failed"
                            : "RimMind.UI.ToolCallDebug.Success";
                        string status = statusKey.Translate(elapsed);
                        string content = Truncate(toolResult.Content, (int)TruncateLen);
                        resultText = status + "\n" + content;
                    }
                    else
                    {
                        string status = "RimMind.UI.ToolCallDebug.Failed".Translate(elapsed);
                        resultText = status + "\n" + result.Error.Message;
                    }

                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        _executionResult = resultText;
                        _isExecuting = false;
                    });
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    long elapsed = sw.ElapsedMilliseconds;
                    string status = "RimMind.UI.ToolCallDebug.Failed".Translate(elapsed);

                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        _executionResult = status + "\n" + ex.Message;
                        _isExecuting = false;
                    });
                }
            });
        }

        private static string Truncate(string value, int maxLen)
        {
            if (value == null) return "";
            if (value.Length <= maxLen) return value;
            return value.Substring(0, maxLen) + "...";
        }
    }
}
