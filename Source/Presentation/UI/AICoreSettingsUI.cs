using System.Collections.Generic;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;
using UnityEngine;
using Verse;

namespace RimMind.Presentation.UI
{
    public static class RimMindCoreSettingsUI
    {
        private const float TabBarHeight = 32f;
        private const float TabBarGap = 6f;
        private const float TabMinWidth = 120f;
        private const float TabGap = 4f;

        private static IExtensionRegistry<ISettingsTab>? _cachedSettingsTabRegistry;

        private static IExtensionRegistry<ISettingsTab>? GetSettingsTabRegistry()
            => _cachedSettingsTabRegistry ??= RimMindRuntime.Instance.GetService<IExtensionRegistry<ISettingsTab>>();

        private static string _curTab = "api";
        private static float _cachedTabBarHeight = TabBarHeight;

        public static void Draw(Rect inRect, ISettingsProvider settings)
        {
            var tabs = CollectTabs();
            _cachedTabBarHeight = CalcTabBarHeight(inRect.width, tabs.Count);

            DrawTabBar(new Rect(inRect.x, inRect.y, inRect.width, _cachedTabBarHeight), tabs);

            Rect content = new Rect(inRect.x, inRect.y + _cachedTabBarHeight + TabBarGap,
                                    inRect.width, inRect.height - _cachedTabBarHeight - TabBarGap);

            switch (_curTab)
            {
                case "api": ApiTabDrawer.Draw(content, settings); break;
                case "queue": QueueTabDrawer.Draw(content, settings); break;
                case "context": ContextTabDrawer.Draw(content, settings); break;
                case "prompts": PromptsTabDrawer.Draw(content, settings); break;
                default:
                    var settingsTabRegistry = GetSettingsTabRegistry();
                    if (settingsTabRegistry != null)
                        foreach (var tab in settingsTabRegistry.All)
                            if (tab.Id == _curTab) { tab.Draw(content); break; }
                    break;
            }
        }

        private static List<(string id, string label)> CollectTabs()
        {
            var tabs = new List<(string id, string label)>
            {
                ("api",     "RimMind.Settings.Tab.Api".Translate()),
                ("queue",   "RimMind.Settings.Tab.Queue".Translate()),
                ("prompts", "RimMind.Settings.Tab.Prompts".Translate()),
                ("context", "RimMind.Settings.Tab.Context".Translate()),
            };
            var settingsTabRegistry = GetSettingsTabRegistry();
            if (settingsTabRegistry != null)
                foreach (var tab in settingsTabRegistry.All)
                    tabs.Add((tab.Id, tab.Label));
            return tabs;
        }

        private static int CalcMaxPerRow(float availableWidth, int tabCount)
        {
            if (tabCount <= 0) return 1;
            int perRow = Mathf.FloorToInt((availableWidth + TabGap) / (TabMinWidth + TabGap));
            return Mathf.Clamp(perRow, 1, tabCount);
        }

        private static float CalcTabBarHeight(float availableWidth, int tabCount)
        {
            if (tabCount <= 0) return TabBarHeight;
            int perRow = CalcMaxPerRow(availableWidth, tabCount);
            int rows = Mathf.CeilToInt((float)tabCount / perRow);
            return rows * TabBarHeight + (rows - 1) * TabGap;
        }

        private static void DrawTabBar(Rect r, List<(string id, string label)> tabs)
        {
            int count = tabs.Count;
            if (count == 0) return;

            int perRow = CalcMaxPerRow(r.width, count);
            int rows = Mathf.CeilToInt((float)count / perRow);

            for (int i = 0; i < count; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                int colsInRow = (row == rows - 1) ? (count - row * perRow) : perRow;

                float w = (r.width - TabGap * (colsInRow - 1)) / colsInRow;
                float x = r.x + col * (w + TabGap);
                float y = r.y + row * (TabBarHeight + TabGap);

                var (id, label) = tabs[i];
                Rect btn = new Rect(x, y, w, TabBarHeight);
                bool selected = _curTab == id;

                GUI.color = selected ? Color.white : Color.gray;
                if (Widgets.ButtonText(btn, label))
                    _curTab = id;
            }
            GUI.color = Color.white;
        }
    }
}
