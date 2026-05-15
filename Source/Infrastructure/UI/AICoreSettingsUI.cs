using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Domain.Enums;
using RimMind.Presentation;
using RimMind.Presentation.Settings;
using RimMind.Infrastructure.Services.Clients.OpenAI;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Logging;
using RimMind.Application.Common.Interfaces.Flywheel;
using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    /// <summary>
    /// 多分页设置界面。
    /// 使用 ButtonText 式导航（不占用 mod 标题区域）。
    /// 子 mod 通过 RimMindAPI.Extensions&lt;ISettingsTab&gt;().Register() 注册额外分页。
    /// </summary>
    public static class RimMindCoreSettingsUI
    {
        private const float TabBarHeight = 32f;
        private const float TabBarGap = 6f;
        private const float TabMinWidth = 120f;
        private const float TabGap = 4f;

        private static string _curTab = "api";
        private static float _cachedTabBarHeight = TabBarHeight;

        // API tab state
        private static bool _showApiKey;
        private static string _testStatus = "";
        private static Color _testStatusColor = Color.white;
        private static Vector2 _apiScroll;

        // Context tab state
        private static ContextPreset _selectedPreset = ContextPreset.Custom;
        private static Vector2 _contextScroll;

        // Prompts tab state
        private static Vector2 _promptsScroll;

        // Queue tab state
        private static Vector2 _queueScroll;

        // ── 入口 ─────────────────────────────────────────────────────────────

        public static void Draw(Rect inRect)
        {
            var tabs = CollectTabs();
            _cachedTabBarHeight = CalcTabBarHeight(inRect.width, tabs.Count);

            DrawTabBar(new Rect(inRect.x, inRect.y, inRect.width, _cachedTabBarHeight), tabs);

            Rect content = new Rect(inRect.x, inRect.y + _cachedTabBarHeight + TabBarGap,
                                    inRect.width, inRect.height - _cachedTabBarHeight - TabBarGap);

            switch (_curTab)
            {
                case "api": DrawApiTab(content); break;
                case "queue": DrawQueueTab(content); break;
                case "context": DrawContextTab(content); break;
                case "prompts": DrawPromptsTab(content); break;
                default:
                    foreach (var tab in RimMindAPI.Extensions<ISettingsTab>().All)
                        if (tab.Id == _curTab) { tab.Draw(content); break; }
                    break;
            }
        }

        private static List<(string id, string label)> CollectTabs()
        {
            var tabs = new List<(string id, string label)>
            {
                ("api",     "RimMind.Infrastructure.Settings.Tab.Api".Translate()),
                ("queue",   "RimMind.Infrastructure.Settings.Tab.Queue".Translate()),
                ("prompts", "RimMind.Infrastructure.Settings.Tab.Prompts".Translate()),
                ("context", "RimMind.Infrastructure.Settings.Tab.Context".Translate()),
            };
            foreach (var tab in RimMindAPI.Extensions<ISettingsTab>().All)
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

        // ── API 配置分页 ─────────────────────────────────────────────────────

        private static void DrawApiTab(Rect inRect)
        {
            var s = RimMindCoreMod.Settings;

            float contentH = EstimateApiHeight();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentH);
            Widgets.BeginScrollView(inRect, ref _apiScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            // ── Provider 选择 ──────────────────────────────────────────────
            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Tab.Api".Translate());

            listing.Label("RimMind.Infrastructure.Settings.Provider".Translate());
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.Provider.Desc".Translate());
            GUI.color = Color.white;
            {
                Rect row = listing.GetRect(28f);
                if (Widgets.ButtonText(row, GetProviderLabel(s.provider)))
                {
                    var options = new List<FloatMenuOption>();
                    foreach (AIProvider p in Enum.GetValues(typeof(AIProvider)))
                    {
                        var label = GetProviderLabel(p);
                        options.Add(new FloatMenuOption(label, () =>
                        {
                            var prev = s.provider;
                            s.provider = p;
                            if (p == AIProvider.Player2)
                                Player2Client.CheckPlayer2StatusAndNotify();
                            if (prev != p)
                                RimMindAPI.InvalidateClientCache();
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            listing.Gap(6f);

            // ── API 配置（OpenAI 兼容模式） ──────────────────────────────────
            if (s.provider == AIProvider.OpenAI)
            {
                listing.Label("RimMind.Infrastructure.Settings.ApiKey".Translate());
                GUI.color = Color.gray;
                listing.Label("  " + "RimMind.Infrastructure.Settings.ApiKey.Desc".Translate());
                GUI.color = Color.white;
                {
                    Rect row = listing.GetRect(26f);
                    float btnW = 52f;
                    Rect field = new Rect(row.x, row.y, row.width - btnW - 4f, row.height);
                    Rect toggle = new Rect(field.xMax + 4f, row.y, btnW, row.height);

                    if (_showApiKey)
                        s.apiKey = Widgets.TextField(field, s.apiKey);
                    else
                    {
                        GUI.enabled = false;
                        Widgets.TextField(field, new string('*', s.apiKey?.Length ?? 0));
                        GUI.enabled = true;
                    }
                    if (Widgets.ButtonText(toggle, _showApiKey ? "RimMind.Infrastructure.Settings.Hide".Translate() : "RimMind.Infrastructure.Settings.Show".Translate()))
                        _showApiKey = !_showApiKey;
                }

                listing.Gap(4f);
                listing.Label("RimMind.Infrastructure.Settings.ApiEndpoint".Translate());
                GUI.color = Color.gray;
                listing.Label("  " + "RimMind.Infrastructure.Settings.ApiEndpoint.Desc".Translate());
                GUI.color = Color.white;
                s.apiEndpoint = listing.TextEntry(s.apiEndpoint);

                listing.Gap(4f);
                listing.Label("RimMind.Infrastructure.Settings.ModelName".Translate());
                GUI.color = Color.gray;
                listing.Label("  " + "RimMind.Infrastructure.Settings.ModelName.Desc".Translate());
                GUI.color = Color.white;
                s.modelName = listing.TextEntry(s.modelName);
            }

            // ── Player2 模式 ───────────────────────────────────────────────
            if (s.provider == AIProvider.Player2)
            {
                GUI.color = Color.gray;
                listing.Label("RimMind.Infrastructure.Settings.Player2.Desc".Translate());
                GUI.color = Color.white;
                listing.Gap(4f);

                listing.Label("RimMind.Infrastructure.Settings.ApiKey".Translate() + " (" + "RimMind.Infrastructure.Settings.Player2.ApiKeyOptional".Translate() + ")");
                GUI.color = Color.gray;
                listing.Label("  " + "RimMind.Infrastructure.Settings.Player2.ApiKeyDesc".Translate());
                GUI.color = Color.white;
                {
                    Rect row = listing.GetRect(26f);
                    float btnW = 52f;
                    Rect field = new Rect(row.x, row.y, row.width - btnW - 4f, row.height);
                    Rect toggle = new Rect(field.xMax + 4f, row.y, btnW, row.height);

                    if (_showApiKey)
                        s.apiKey = Widgets.TextField(field, s.apiKey);
                    else
                    {
                        GUI.enabled = false;
                        Widgets.TextField(field, new string('*', s.apiKey?.Length ?? 0));
                        GUI.enabled = true;
                    }
                    if (Widgets.ButtonText(toggle, _showApiKey ? "RimMind.Infrastructure.Settings.Hide".Translate() : "RimMind.Infrastructure.Settings.Show".Translate()))
                        _showApiKey = !_showApiKey;
                }

                listing.Gap(4f);
                {
                    Rect checkBtnRow = listing.GetRect(28f);
                    if (Widgets.ButtonText(checkBtnRow, "RimMind.Infrastructure.Settings.Player2.CheckLocal".Translate()))
                        Player2Client.CheckPlayer2StatusAndNotify();
                }

                listing.Gap(4f);
                listing.Label("RimMind.Infrastructure.Settings.Player2.RemoteUrl".Translate());
                GUI.color = Color.gray;
                listing.Label("  " + "RimMind.Infrastructure.Settings.Player2.RemoteUrl.Desc".Translate());
                GUI.color = Color.white;
                s.player2RemoteUrl = listing.TextEntry(s.player2RemoteUrl);

                listing.Gap(4f);
                {
                    float balance = Player2Client.CachedJoulesBalance;
                    string balanceText = balance >= 0
                        ? $"Joules: {balance:F2}"
                        : "RimMind.Infrastructure.Settings.Player2.BalanceUnknown".Translate();
                    listing.Label(balanceText);

                    Rect refreshRow = listing.GetRect(28f);
                    if (Widgets.ButtonText(refreshRow, "RimMind.Infrastructure.Settings.Player2.RefreshBalance".Translate()))
                        Player2Client.RefreshJoulesBalance();
                }
            }

            listing.Gap(10f);

            // ── 测试连接 ──────────────────────────────────────────────────────
            {
                Rect row = listing.GetRect(28f);
                Rect btn = new Rect(row.x, row.y, 110f, row.height);
                Rect status = new Rect(btn.xMax + 8f, row.y + 4f, row.width - 120f, row.height);
                if (Widgets.ButtonText(btn, "RimMind.Infrastructure.Settings.TestConnection".Translate()))
                    RunConnectionTest(s);
                GUI.color = _testStatusColor;
                Widgets.Label(status, _testStatus);
                GUI.color = Color.white;
            }

            listing.Gap(6f);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Section.ModelBehavior".Translate());
            listing.CheckboxLabeled(
                "RimMind.Infrastructure.Settings.ForceJsonMode".Translate(),
                ref s.forceJsonMode,
                "RimMind.Infrastructure.Settings.ForceJsonModeDesc".Translate());

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Section.Request".Translate());
            listing.Label($"{"RimMind.Infrastructure.Settings.MaxTokens".Translate()}: {s.maxTokens}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.MaxTokens.Desc".Translate());
            GUI.color = Color.white;
            s.maxTokens = (int)listing.Slider(s.maxTokens, 200f, 2000f);

            listing.Label($"{"RimMind.Infrastructure.Settings.Temperature".Translate()}: {s.defaultTemperature:F2}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.Temperature.Desc".Translate());
            GUI.color = Color.white;
            s.defaultTemperature = listing.Slider(s.defaultTemperature, 0f, 2f);

            listing.Label($"{"RimMind.Infrastructure.Settings.MaxConcurrent".Translate()}: {s.maxConcurrentRequests}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.MaxConcurrent.Desc".Translate());
            GUI.color = Color.white;
            s.maxConcurrentRequests = (int)listing.Slider(s.maxConcurrentRequests, 1f, 10f);

            listing.Label($"{"RimMind.Infrastructure.Settings.MaxRetry".Translate()}: {s.maxRetryCount}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.MaxRetry.Desc".Translate());
            GUI.color = Color.white;
            s.maxRetryCount = (int)listing.Slider(s.maxRetryCount, 0f, 5f);

            listing.Label($"{"RimMind.Infrastructure.Settings.RequestTimeout".Translate()}: {s.requestTimeoutMs / 1000}s");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.RequestTimeout.Desc".Translate());
            GUI.color = Color.white;
            s.requestTimeoutMs = (int)listing.Slider(s.requestTimeoutMs / 1000f, 10f, 300f) * 1000;

            listing.Label($"{"RimMind.Infrastructure.Settings.RequestExpireTicks".Translate()}: {s.requestExpireTicks / 60f:F0}s ({s.requestExpireTicks} ticks)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.RequestExpireTicks.Desc".Translate());
            GUI.color = Color.white;
            s.requestExpireTicks = (int)listing.Slider(s.requestExpireTicks, 6000f, 120000f);

            listing.Label($"{"RimMind.Infrastructure.Settings.BehaviorHistoryMax".Translate()}: {s.behaviorHistoryMax}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.BehaviorHistoryMax.Desc".Translate());
            GUI.color = Color.white;
            s.behaviorHistoryMax = (int)listing.Slider(s.behaviorHistoryMax, 10f, 500f);

            listing.Label($"{"RimMind.Infrastructure.Settings.QueueProcessInterval".Translate()}: {s.queueProcessInterval} ticks ({s.queueProcessInterval / 60f:F1}s)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.QueueProcessInterval.Desc".Translate());
            GUI.color = Color.white;
            s.queueProcessInterval = (int)listing.Slider(s.queueProcessInterval, 10f, 300f);

            listing.Label($"{"RimMind.Infrastructure.Settings.DefaultModCooldown".Translate()}: {s.defaultModCooldownTicks / 60f:F0}s ({s.defaultModCooldownTicks} ticks)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.DefaultModCooldown.Desc".Translate());
            GUI.color = Color.white;
            s.defaultModCooldownTicks = (int)listing.Slider(s.defaultModCooldownTicks, 600f, 36000f);

            var queue = RimMindServiceLocator.Get<IAIRequestQueue>();
            if (queue != null)
            {
                listing.Gap(4f);
                GUI.color = Color.gray;
                listing.Label("RimMind.Infrastructure.Settings.QueueSeeTab".Translate());
                GUI.color = Color.white;
            }
            GUI.color = Color.white;

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Section.Debug".Translate());
            listing.CheckboxLabeled("RimMind.Infrastructure.Settings.DebugLogging".Translate(), ref s.debugLogging,
                "RimMind.Infrastructure.Settings.DebugLogging.Desc".Translate());

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.UI.FlywheelAutoApply".Translate());
            {
                Rect row = listing.GetRect(28f);
                if (Widgets.ButtonText(row, GetAutoApplyModeLabel(s.autoApplyMode)))
                {
                    var modes = new List<FloatMenuOption>();
                    foreach (FlywheelAutoApplyMode mode in Enum.GetValues(typeof(FlywheelAutoApplyMode)))
                    {
                        var label = GetAutoApplyModeLabel(mode);
                        modes.Add(new FloatMenuOption(label, () => s.autoApplyMode = mode));
                    }
                    Find.WindowStack.Add(new FloatMenu(modes));
                }
            }

            listing.Label("RimMind.Infrastructure.UI.FlywheelConfidence".Translate(s.autoApplyConfidenceThreshold));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.UI.FlywheelConfidence.Desc".Translate());
            GUI.color = Color.white;
            s.autoApplyConfidenceThreshold = listing.Slider(s.autoApplyConfidenceThreshold, 0.5f, 1.0f);

            listing.End();
            Widgets.EndScrollView();
        }

        /// <summary>
        /// 使用 OpenAIClient / Player2Client 发送测试请求，走正常 AI 请求管道。
        /// </summary>
        private static void RunConnectionTest(RimMindCoreSettings s)
        {
            if (s.provider == AIProvider.Player2)
            {
                _testStatus = "RimMind.Infrastructure.Settings.Status.Testing".Translate();
                _testStatusColor = Color.yellow;

                Task.Run(async () =>
                {
                    try
                    {
                        var client = await Player2Client.CreateAsync(s);
                        if (!client.IsConfigured())
                        {
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _testStatus = "RimMind.Infrastructure.Settings.Player2.NotAvailable".Translate();
                                _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                            });
                            return;
                        }

                        var request = new AIRequest
                        {
                            RequestId = "test",
                            UserPrompt = "RimMind.Infrastructure.Settings.TestMessage".Translate(),
                            MaxTokens = 60,
                            Temperature = 0.7f,
                            ModId = "RimMind.Test"
                        };
                        var result = await client.SendAsync(request);
                        if (result.TryGetValue(out var response))
                        {
                            var content = response.Content.Trim();
                            var tok = response.TokensUsed;
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _testStatus = $"✓ {content} ({tok} tok)";
                                _testStatusColor = new Color(0.4f, 0.9f, 0.4f);
                            });
                        }
                        else
                        {
                            var error = result.Error.Message;
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _testStatus = $"✗ {error}";
                                _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message;
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = $"✗ {msg}";
                            _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                        });
                    }
                });
                return;
            }

            if (!s.IsOpenAIConfigured())
            {
                _testStatus = "RimMind.Infrastructure.Settings.Status.NotConfigured".Translate();
                _testStatusColor = Color.yellow;
                return;
            }

            _testStatus = "RimMind.Infrastructure.Settings.Status.Testing".Translate();
            _testStatusColor = Color.yellow;

            Task.Run(async () =>
            {
                try
                {
                    var client = new OpenAIClient(s);
                    if (!client.IsConfigured())
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = "RimMind.Infrastructure.Settings.Status.NotConfigured".Translate();
                            _testStatusColor = Color.yellow;
                        });
                        return;
                    }

                    var request = new AIRequest
                    {
                        RequestId = "test",
                        UserPrompt = "RimMind.Infrastructure.Settings.TestMessage".Translate(),
                        MaxTokens = 60,
                        Temperature = 0.7f,
                        ModId = "RimMind.Test"
                    };
                    var result = await client.SendAsync(request);
                    if (result.TryGetValue(out var response2))
                    {
                        var content = response2.Content.Trim();
                        var tok = response2.TokensUsed;
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = $"✓ {content} ({tok} tok)";
                            _testStatusColor = new Color(0.4f, 0.9f, 0.4f);
                        });
                    }
                    else
                    {
                        var error = result.Error.Message;
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = $"✗ {error}";
                            _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                        });
                    }
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        _testStatus = $"✗ {msg}";
                        _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                    });
                }
            });
        }



        // ── 队列状态分页 ─────────────────────────────────────────────────────

        private static void DrawQueueTab(Rect inRect)
        {
            var queue = RimMindServiceLocator.Get<IAIRequestQueue>();
            if (queue == null)
            {
                var listing0 = new Listing_Standard();
                listing0.Begin(inRect);
                GUI.color = Color.yellow;
                listing0.Label("RimMind.Infrastructure.Settings.QueueNotAvailable".Translate());
                GUI.color = Color.white;
                listing0.End();
                return;
            }

            var allDepths = queue.GetAllQueueDepths();
            var allCooldowns = queue.GetAllCooldowns();
            var allModIds = new HashSet<string>(allDepths.Keys);
            allModIds.UnionWith(allCooldowns.Keys);
            allModIds.UnionWith(RimMindAPI.Extensions<IModCooldown>().All.Select(c => c.Id));

            int modCount = allModIds.Count;
            int activeCount = queue.ActiveRequestCount;
            int queuedCount = queue.TotalQueuedCount;
            float contentH = 60f + 28f + modCount * 26f + 28f + activeCount * 24f + 28f + queuedCount * 24f + 80f;
            contentH = Mathf.Max(contentH, inRect.height + 10f);

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentH);
            Widgets.BeginScrollView(inRect, ref _queueScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            // ── 总体状态 ──────────────────────────────────────────────────────
            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Queue.Status".Translate());

            string pauseLabel = queue.IsPaused
                ? "RimMind.Infrastructure.Settings.QueuePaused".Translate()
                : "RimMind.Infrastructure.Settings.QueueRunning".Translate();
            GUI.color = queue.IsPaused ? Color.yellow : new Color(0.4f, 0.9f, 0.4f);
            listing.Label(pauseLabel);
            GUI.color = Color.white;

            listing.Label($"{"RimMind.Infrastructure.Settings.Queue.Active".Translate()}: {activeCount} / {RimMindCoreMod.Settings.maxConcurrentRequests}");
            listing.Label($"{"RimMind.Infrastructure.Settings.Queue.Queued".Translate()}: {queuedCount}");
            GUI.color = queue.IsLocalModelBusy ? new Color(0.9f, 0.6f, 0.3f) : new Color(0.4f, 0.9f, 0.4f);
            listing.Label($"{"RimMind.Infrastructure.Settings.Queue.LocalModel".Translate()}: {(queue.IsLocalModelBusy ? "RimMind.Infrastructure.Settings.Queue.Busy".Translate() : "RimMind.Infrastructure.Settings.Queue.Idle".Translate())}");
            GUI.color = Color.white;

            // ── 操作按钮 ──────────────────────────────────────────────────────
            listing.Gap(4f);
            Rect btnRow = listing.GetRect(28f);
            float btnW = 110f;
            float gap = 8f;

            Rect pauseBtn = new Rect(btnRow.x, btnRow.y, btnW, btnRow.height);
            Rect clearBtn = new Rect(pauseBtn.xMax + gap, btnRow.y, btnW, btnRow.height);
            Rect clearCdBtn = new Rect(clearBtn.xMax + gap, btnRow.y, btnW + 20f, btnRow.height);

            string pauseText = queue.IsPaused
                ? "RimMind.Infrastructure.Settings.Queue.Resume".Translate()
                : "RimMind.Infrastructure.Settings.Queue.Pause".Translate();
            if (Widgets.ButtonText(pauseBtn, pauseText))
            {
                if (queue.IsPaused) queue.ResumeQueue();
                else queue.PauseQueue();
            }
            if (Widgets.ButtonText(clearBtn, "RimMind.Infrastructure.Settings.Queue.ClearQueues".Translate()))
                queue.ClearAllQueues();
            if (Widgets.ButtonText(clearCdBtn, "RimMind.Infrastructure.Settings.Queue.ClearCooldowns".Translate()))
                queue.ClearAllCooldowns();

            // ── 各 Mod 队列 ──────────────────────────────────────────────────
            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Queue.PerMod".Translate());

            if (allModIds.Count == 0)
            {
                GUI.color = Color.gray;
                listing.Label("RimMind.Infrastructure.Settings.Queue.NoMods".Translate());
                GUI.color = Color.white;
            }
            else
            {
                foreach (string modId in allModIds.OrderBy(id => id))
                {
                    int depth = allDepths.TryGetValue(modId, out var d) ? d : 0;
                    int cooldownLeft = queue.GetCooldownTicksLeft(modId);
                    float cooldownSec = cooldownLeft / 60f;

                    string cooldownStr = cooldownLeft > 0
                        ? $"{"RimMind.Infrastructure.Settings.Queue.Cooldown".Translate()}: {cooldownSec:F1}s"
                        : "RimMind.Infrastructure.Settings.Queue.Ready".Translate();
                    string depthStr = depth > 0
                        ? $"  [{"RimMind.Infrastructure.Settings.Queue.QueueCount".Translate()}: {depth}]"
                        : "";

                    GUI.color = cooldownLeft > 0 ? new Color(0.9f, 0.6f, 0.3f) : new Color(0.4f, 0.9f, 0.4f);
                    listing.Label($"{modId}  {cooldownStr}{depthStr}");
                }
            }
            GUI.color = Color.white;

            // ── 活跃请求 ──────────────────────────────────────────────────────
            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Queue.ActiveRequests".Translate());

            var activeRequests = queue.GetActiveRequests();
            if (activeRequests.Count == 0)
            {
                GUI.color = Color.gray;
                listing.Label("RimMind.Infrastructure.Settings.Queue.NoActive".Translate());
                GUI.color = Color.white;
            }
            else
            {
                foreach (var req in activeRequests)
                {
                    int elapsedTicks = Find.TickManager.TicksGame - req.StartedProcessingAtTick;
                    float elapsedSec = elapsedTicks / 60f;
                    string priority = req.Request.Priority.ToString();
                    string info = $"[{req.Request.ModId}] {req.Request.RequestId}  " +
                                  $"{"RimMind.Infrastructure.Settings.Queue.Priority".Translate()}: {priority}  " +
                                  $"{"RimMind.Infrastructure.Settings.Queue.Attempt".Translate()}: {req.AttemptCount}/{req.MaxAttempts}  " +
                                  $"{"RimMind.Infrastructure.Settings.Queue.Elapsed".Translate()}: {elapsedSec:F1}s";
                    GUI.color = new Color(0.7f, 0.85f, 1f);
                    listing.Label(info);
                }
            }
            GUI.color = Color.white;

            // ── 排队请求 ──────────────────────────────────────────────────────
            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Queue.QueuedRequests".Translate());

            var queuedRequests = queue.GetAllQueuedRequests();
            if (queuedRequests.Count == 0)
            {
                GUI.color = Color.gray;
                listing.Label("RimMind.Infrastructure.Settings.Queue.NoQueued".Translate());
                GUI.color = Color.white;
            }
            else
            {
                foreach (var req in queuedRequests)
                {
                    int waitTicks = Find.TickManager.TicksGame - req.EnqueuedAtTick;
                    float waitSec = waitTicks / 60f;
                    string priority = req.Request.Priority.ToString();
                    string info = $"[{req.Request.ModId}] {req.Request.RequestId}  " +
                                  $"{"RimMind.Infrastructure.Settings.Queue.Priority".Translate()}: {priority}  " +
                                  $"{"RimMind.Infrastructure.Settings.Queue.Attempt".Translate()}: {req.AttemptCount}/{req.MaxAttempts}  " +
                                  $"{"RimMind.Infrastructure.Settings.Queue.Waiting".Translate()}: {waitSec:F1}s";
                    GUI.color = new Color(0.85f, 0.85f, 0.7f);
                    listing.Label(info);
                }
            }
            GUI.color = Color.white;

            listing.End();
            Widgets.EndScrollView();
        }

        // ── 自定义提示词分页 ──────────────────────────────────────────────────

        private static void DrawPromptsTab(Rect inRect)
        {
            var s = RimMindCoreMod.Settings;

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 460f);
            Widgets.BeginScrollView(inRect, ref _promptsScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            GUI.color = Color.gray;
            listing.Label("RimMind.Presentation.Prompts.Desc".Translate());
            GUI.color = Color.white;
            listing.Gap(8f);

            SettingsUIHelper.DrawCustomPromptSection(listing,
                "RimMind.Presentation.Prompts.PawnPromptLabel".Translate(),
                ref s.customPawnPrompt, 100f);

            listing.Gap(12f);

            SettingsUIHelper.DrawCustomPromptSection(listing,
                "RimMind.Presentation.Prompts.MapPromptLabel".Translate(),
                ref s.customMapPrompt, 100f);

            listing.End();
            Widgets.EndScrollView();
        }

        // ── 上下文过滤分页 ────────────────────────────────────────────────────

        private static void DrawContextTab(Rect inRect)
        {
            var ctx = RimMindCoreMod.Settings.Context;

            // 估算内容高度（用 ScrollView）
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 980f);
            Widgets.BeginScrollView(inRect, ref _contextScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            GUI.color = Color.gray;
            listing.Label("RimMind.Presentation.Context.Desc".Translate());
            GUI.color = Color.white;
            listing.Gap(8f);

            // ── 预设卡片 ─────────────────────────────────────────────────────
            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Presentation.Context.Presets".Translate());
            DrawPresetCards(listing, ctx);
            listing.Gap(12f);

            // ── 两栏复选框 ───────────────────────────────────────────────────
            float colW = (listing.ColumnWidth - 20f) / 2f;
            Rect anchor = listing.GetRect(0f);

            var left = new Listing_Standard();
            left.Begin(new Rect(anchor.x, anchor.y, colW, 9999f));
            GUI.color = new Color(0.6f, 0.78f, 1f);
            left.Label("RimMind.Presentation.Context.PawnInfo".Translate());
            GUI.color = Color.white;
            left.Gap(4f);
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeRace".Translate(), ref ctx.IncludeRace, "RimMind.Presentation.Context.IncludeRace.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeAge".Translate(), ref ctx.IncludeAge, "RimMind.Presentation.Context.IncludeAge.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeGender".Translate(), ref ctx.IncludeGender, "RimMind.Presentation.Context.IncludeGender.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeBackstory".Translate(), ref ctx.IncludeBackstory, "RimMind.Presentation.Context.IncludeBackstory.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeIdeology".Translate(), ref ctx.IncludeIdeology, "RimMind.Presentation.Context.IncludeIdeology.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeTraits".Translate(), ref ctx.IncludeTraits, "RimMind.Presentation.Context.IncludeTraits.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeSkills".Translate(), ref ctx.IncludeSkills, "RimMind.Presentation.Context.IncludeSkills.Desc".Translate());
            if (ctx.IncludeSkills)
            {
                left.Label($"  {"RimMind.Presentation.Context.MinSkillLevel".Translate()}: {ctx.MinSkillLevel}");
                ctx.MinSkillLevel = (int)left.Slider(ctx.MinSkillLevel, 1f, 15f);
            }
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeHealth".Translate(), ref ctx.IncludeHealth, "RimMind.Presentation.Context.IncludeHealth.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeCapacities".Translate(), ref ctx.IncludeCapacities, "RimMind.Presentation.Context.IncludeCapacities.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeMood".Translate(), ref ctx.IncludeMood, "RimMind.Presentation.Context.IncludeMood.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeMoodThoughts".Translate(), ref ctx.IncludeMoodThoughts, "RimMind.Presentation.Context.IncludeMoodThoughts.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeCurrentJob".Translate(), ref ctx.IncludeCurrentJob, "RimMind.Presentation.Context.IncludeCurrentJob.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeWorkPriorities".Translate(), ref ctx.IncludeWorkPriorities, "RimMind.Presentation.Context.IncludeWorkPriorities.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeEquipment".Translate(), ref ctx.IncludeEquipment, "RimMind.Presentation.Context.IncludeEquipment.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeInventory".Translate(), ref ctx.IncludeInventory, "RimMind.Presentation.Context.IncludeInventory.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeLocation".Translate(), ref ctx.IncludeLocation, "RimMind.Presentation.Context.IncludeLocation.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeRelations".Translate(), ref ctx.IncludeRelations, "RimMind.Presentation.Context.IncludeRelations.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeGenes".Translate(), ref ctx.IncludeGenes, "RimMind.Presentation.Context.IncludeGenes.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeCombatStatus".Translate(), ref ctx.IncludeCombatStatus, "RimMind.Presentation.Context.IncludeCombatStatus.Desc".Translate());
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeSurroundings".Translate(), ref ctx.IncludeSurroundings, "RimMind.Presentation.Context.IncludeSurroundings.Desc".Translate());
            float leftH = left.CurHeight;
            left.End();

            var right = new Listing_Standard();
            right.Begin(new Rect(anchor.x + colW + 20f, anchor.y, colW, 9999f));
            GUI.color = new Color(0.6f, 0.78f, 1f);
            right.Label("RimMind.Presentation.Context.Environment".Translate());
            GUI.color = Color.white;
            right.Gap(4f);
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeGameTime".Translate(), ref ctx.IncludeGameTime, "RimMind.Presentation.Context.IncludeGameTime.Desc".Translate());
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeColonistCount".Translate(), ref ctx.IncludeColonistCount, "RimMind.Presentation.Context.IncludeColonistCount.Desc".Translate());
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeColonistNames".Translate(), ref ctx.IncludeColonistNames, "RimMind.Presentation.Context.IncludeColonistNames.Desc".Translate());
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeWealth".Translate(), ref ctx.IncludeWealth, "RimMind.Presentation.Context.IncludeWealth.Desc".Translate());
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeFood".Translate(), ref ctx.IncludeFood, "RimMind.Presentation.Context.IncludeFood.Desc".Translate());
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeSeason".Translate(), ref ctx.IncludeSeason, "RimMind.Presentation.Context.IncludeSeason.Desc".Translate());
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeWeather".Translate(), ref ctx.IncludeWeather, "RimMind.Presentation.Context.IncludeWeather.Desc".Translate());
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeThreats".Translate(), ref ctx.IncludeThreats, "RimMind.Presentation.Context.IncludeThreats.Desc".Translate());
            float rightH = right.CurHeight;
            right.End();

            listing.Gap(Mathf.Max(leftH, rightH) + 8f);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Presentation.Context.Budget".Translate());
            listing.Label($"{"RimMind.Presentation.Context.ContextBudget".Translate()}: {ctx.ContextBudget:F1}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Presentation.Context.ContextBudget.Desc".Translate());
            GUI.color = Color.white;
            ctx.ContextBudget = listing.Slider(ctx.ContextBudget, 0.1f, 2.0f);

            listing.Label($"{"RimMind.Presentation.Context.BudgetW1".Translate()}: {ctx.BudgetW1:F2}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Presentation.Context.BudgetW1.Desc".Translate());
            GUI.color = Color.white;
            ctx.BudgetW1 = Mathf.Round(listing.Slider(ctx.BudgetW1, 0f, 1f) * 20f) / 20f;

            listing.Label($"{"RimMind.Presentation.Context.BudgetW2".Translate()}: {ctx.BudgetW2:F2}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Presentation.Context.BudgetW2.Desc".Translate());
            GUI.color = Color.white;
            ctx.BudgetW2 = Mathf.Round(listing.Slider(ctx.BudgetW2, 0f, 1f) * 20f) / 20f;

            listing.Gap(8f);

            var s = RimMindCoreMod.Settings;
            listing.Label($"{"RimMind.Infrastructure.Settings.ContextDiffLifetime".Translate()}: {s.contextDiffLifetimeTicks / 60f:F0}s ({s.contextDiffLifetimeTicks} ticks)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.ContextDiffLifetime.Desc".Translate());
            GUI.color = Color.white;
            s.contextDiffLifetimeTicks = (int)listing.Slider(s.contextDiffLifetimeTicks, 300f, 3000f);

            listing.Gap(6f);
            var calibrateSec = s.contextCalibrateInterval / 60f;
            listing.Label($"{"RimMind.Infrastructure.Settings.CalibrateInterval".Translate()}: {calibrateSec:F0}s ({s.contextCalibrateInterval} ticks)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.CalibrateInterval.Desc".Translate());
            GUI.color = Color.white;
            s.contextCalibrateInterval = (int)listing.Slider(s.contextCalibrateInterval, 5000f, 60000f);

            if (listing.ButtonText("RimMind.Presentation.Context.ResetDefault".Translate()))
            {
                RimMindCoreMod.Settings.Context = new ContextSettings();
                _selectedPreset = ContextPreset.Standard;
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private static void DrawPresetCards(Listing_Standard listing, ContextSettings ctx)
        {
            var presets = new[] { ContextPreset.Minimal, ContextPreset.Standard, ContextPreset.Full, ContextPreset.Custom };
            const float gap = 10f;
            const float h = 62f;
            float totalW = listing.ColumnWidth;
            float w = (totalW - gap * (presets.Length - 1)) / presets.Length;
            Rect row = listing.GetRect(h);

            for (int i = 0; i < presets.Length; i++)
            {
                var preset = presets[i];
                bool selected = _selectedPreset == preset;
                Rect box = new Rect(row.x + (w + gap) * i, row.y, w, h);

                Widgets.DrawBoxSolid(box,
                    selected ? new Color(0.2f, 0.4f, 0.6f, 0.85f) : new Color(0.18f, 0.18f, 0.18f, 0.55f));
                GUI.color = selected ? new Color(0.4f, 0.7f, 1f) : new Color(0.45f, 0.45f, 0.45f);
                Widgets.DrawBox(box, 2);
                GUI.color = Color.white;

                if (Mouse.IsOver(box)) Widgets.DrawHighlight(box);
                if (Widgets.ButtonInvisible(box))
                {
                    _selectedPreset = preset;
                    if (preset != ContextPreset.Custom)
                        ctx.ApplyPreset(preset);
                }

                Rect inner = box.ContractedBy(6f);
                Text.Anchor = TextAnchor.UpperCenter;

                GUI.color = selected ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                Widgets.Label(new Rect(inner.x, inner.y, inner.width, Text.LineHeight),
                    $"RimMind.Presentation.Context.Preset.{preset}".Translate());

                Text.Font = GameFont.Tiny;
                GUI.color = selected ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.55f, 0.55f, 0.55f);
                Widgets.Label(new Rect(inner.x, inner.y + Text.LineHeight + 2f,
                                       inner.width, inner.height - Text.LineHeight - 2f),
                    $"RimMind.Presentation.Context.Preset.{preset}.Desc".Translate());

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            listing.Gap(4f);
        }

        // ── 辅助 ─────────────────────────────────────────────────────────────

        private static float EstimateApiHeight()
        {
            float h = 30f;
            h += 24f + 28f + 6f;
            h += 24f + 26f + 4f + 24f + 4f + 24f + 10f + 28f;
            h += 24f + 24f;
            h += 24f + 24f + 32f;
            h += 24f + 24f;
            h += 24f + 24f + 32f;
            h += 24f;
            h += 24f;
            h += 24f + 24f;
            h += 24f + 24f + 32f;
            return h + 40f;
        }

        private static string GetProviderLabel(AIProvider p)
        {
            return p switch
            {
                AIProvider.OpenAI => "RimMind.Infrastructure.Settings.Provider.OpenAI".Translate(),
                AIProvider.Player2 => "RimMind.Infrastructure.Settings.Provider.Player2".Translate(),
                _ => p.ToString()
            };
        }

        private static string GetAutoApplyModeLabel(FlywheelAutoApplyMode mode)
        {
            return mode switch
            {
                FlywheelAutoApplyMode.Off => "RimMind.Infrastructure.UI.FlywheelAutoApply.Off".Translate(),
                FlywheelAutoApplyMode.LogOnly => "RimMind.Infrastructure.UI.FlywheelAutoApply.LogOnly".Translate(),
                FlywheelAutoApplyMode.ApplyWithLog => "RimMind.Infrastructure.UI.FlywheelAutoApply.Apply".Translate(),
                _ => mode.ToString()
            };
        }

    }
}
