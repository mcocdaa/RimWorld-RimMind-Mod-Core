using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Helpers;
using RimMind.Domain.Enums;
using RimMind.Infrastructure.Services.Clients.OpenAI;
using RimMind.Infrastructure.Services.Clients.Player2;
using RimMind.Application.Common.Interfaces.Flywheel;
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

        private static string _curTab = "api";
        private static float _cachedTabBarHeight = TabBarHeight;

        private static bool _showApiKey;
        private static string _testStatus = "";
        private static Color _testStatusColor = Color.white;
        private static Vector2 _apiScroll;

        private static ContextPreset _selectedPreset = ContextPreset.Custom;
        private static Vector2 _contextScroll;

        private static Vector2 _promptsScroll;

        private static Vector2 _queueScroll;

        public static void Draw(Rect inRect, ISettingsProvider settings)
        {
            var tabs = CollectTabs();
            _cachedTabBarHeight = CalcTabBarHeight(inRect.width, tabs.Count);

            DrawTabBar(new Rect(inRect.x, inRect.y, inRect.width, _cachedTabBarHeight), tabs);

            Rect content = new Rect(inRect.x, inRect.y + _cachedTabBarHeight + TabBarGap,
                                    inRect.width, inRect.height - _cachedTabBarHeight - TabBarGap);

            switch (_curTab)
            {
                case "api": DrawApiTab(content, settings); break;
                case "queue": DrawQueueTab(content, settings); break;
                case "context": DrawContextTab(content, settings); break;
                case "prompts": DrawPromptsTab(content, settings); break;
                default:
                    var settingsTabRegistry = RimMindServiceLocator.Get<IExtensionRegistry<ISettingsTab>>();
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
                ("api",     "RimMind.Infrastructure.Settings.Tab.Api".Translate()),
                ("queue",   "RimMind.Infrastructure.Settings.Tab.Queue".Translate()),
                ("prompts", "RimMind.Infrastructure.Settings.Tab.Prompts".Translate()),
                ("context", "RimMind.Infrastructure.Settings.Tab.Context".Translate()),
            };
            var settingsTabRegistry = RimMindServiceLocator.Get<IExtensionRegistry<ISettingsTab>>();
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

        private static void DrawApiTab(Rect inRect, ISettingsProvider s)
        {
            float contentH = EstimateApiHeight();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentH);
            Widgets.BeginScrollView(inRect, ref _apiScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Tab.Api".Translate());

            listing.Label("RimMind.Infrastructure.Settings.Provider".Translate());
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.Provider.Desc".Translate());
            GUI.color = Color.white;
            {
                Rect row = listing.GetRect(28f);
                if (Widgets.ButtonText(row, GetProviderLabel(s.Provider)))
                {
                    var options = new List<FloatMenuOption>();
                    var allProviders = ProviderHelper.GetAllProviderIds();
                    foreach (var p in allProviders)
                    {
                        var label = GetProviderLabel(p);
                        options.Add(new FloatMenuOption(label, () =>
                        {
                            var prev = s.Provider;
                            s.Provider = p;
                            if (!ProviderHelper.RequiresApiKey(p))
                                Player2Client.CheckPlayer2StatusAndNotify();
                            if (prev != p)
                                RimMindServiceLocator.Get<IClientManager>()?.InvalidateCache();
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
            }

            listing.Gap(6f);

            if (ProviderHelper.RequiresApiKey(s.Provider))
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
                        s.ApiKey = Widgets.TextField(field, s.ApiKey);
                    else
                    {
                        GUI.enabled = false;
                        Widgets.TextField(field, new string('*', s.ApiKey?.Length ?? 0));
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
                s.ApiEndpoint = listing.TextEntry(s.ApiEndpoint);

                listing.Gap(4f);
                listing.Label("RimMind.Infrastructure.Settings.ModelName".Translate());
                GUI.color = Color.gray;
                listing.Label("  " + "RimMind.Infrastructure.Settings.ModelName.Desc".Translate());
                GUI.color = Color.white;
                s.ModelName = listing.TextEntry(s.ModelName);
            }

            if (!ProviderHelper.RequiresApiKey(s.Provider))
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
                        s.ApiKey = Widgets.TextField(field, s.ApiKey);
                    else
                    {
                        GUI.enabled = false;
                        Widgets.TextField(field, new string('*', s.ApiKey?.Length ?? 0));
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
                s.Player2RemoteUrl = listing.TextEntry(s.Player2RemoteUrl);

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
            var forceJsonMode = s.ForceJsonMode;
            listing.CheckboxLabeled(
                "RimMind.Infrastructure.Settings.ForceJsonMode".Translate(),
                ref forceJsonMode,
                "RimMind.Infrastructure.Settings.ForceJsonModeDesc".Translate());
            s.ForceJsonMode = forceJsonMode;

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Section.Request".Translate());
            listing.Label($"{"RimMind.Infrastructure.Settings.MaxTokens".Translate()}: {s.MaxTokens}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.MaxTokens.Desc".Translate());
            GUI.color = Color.white;
            s.MaxTokens = (int)listing.Slider(s.MaxTokens, 200f, 2000f);

            listing.Label($"{"RimMind.Infrastructure.Settings.Temperature".Translate()}: {s.DefaultTemperature:F2}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.Temperature.Desc".Translate());
            GUI.color = Color.white;
            s.DefaultTemperature = listing.Slider(s.DefaultTemperature, 0f, 2f);

            listing.Label($"{"RimMind.Infrastructure.Settings.MaxConcurrent".Translate()}: {s.MaxConcurrentRequests}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.MaxConcurrent.Desc".Translate());
            GUI.color = Color.white;
            s.MaxConcurrentRequests = (int)listing.Slider(s.MaxConcurrentRequests, 1f, 10f);

            listing.Label($"{"RimMind.Infrastructure.Settings.MaxRetry".Translate()}: {s.MaxRetryCount}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.MaxRetry.Desc".Translate());
            GUI.color = Color.white;
            s.MaxRetryCount = (int)listing.Slider(s.MaxRetryCount, 0f, 5f);

            listing.Label($"{"RimMind.Infrastructure.Settings.RequestTimeout".Translate()}: {s.RequestTimeoutMs / 1000}s");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.RequestTimeout.Desc".Translate());
            GUI.color = Color.white;
            s.RequestTimeoutMs = (int)listing.Slider(s.RequestTimeoutMs / 1000f, 10f, 300f) * 1000;

            listing.Label($"{"RimMind.Infrastructure.Settings.RequestExpireTicks".Translate()}: {s.RequestExpireTicks / 60f:F0}s ({s.RequestExpireTicks} ticks)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.RequestExpireTicks.Desc".Translate());
            GUI.color = Color.white;
            s.RequestExpireTicks = (int)listing.Slider(s.RequestExpireTicks, 6000f, 120000f);

            listing.Label($"{"RimMind.Infrastructure.Settings.BehaviorHistoryMax".Translate()}: {s.BehaviorHistoryMax}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.BehaviorHistoryMax.Desc".Translate());
            GUI.color = Color.white;
            s.BehaviorHistoryMax = (int)listing.Slider(s.BehaviorHistoryMax, 10f, 500f);

            listing.Label($"{"RimMind.Infrastructure.Settings.QueueProcessInterval".Translate()}: {s.QueueProcessInterval} ticks ({s.QueueProcessInterval / 60f:F1}s)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.QueueProcessInterval.Desc".Translate());
            GUI.color = Color.white;
            s.QueueProcessInterval = (int)listing.Slider(s.QueueProcessInterval, 10f, 300f);

            listing.Label($"{"RimMind.Infrastructure.Settings.DefaultModCooldown".Translate()}: {s.DefaultModCooldownTicks / 60f:F0}s ({s.DefaultModCooldownTicks} ticks)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.DefaultModCooldown.Desc".Translate());
            GUI.color = Color.white;
            s.DefaultModCooldownTicks = (int)listing.Slider(s.DefaultModCooldownTicks, 600f, 36000f);

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
            var debugLogging = s.DebugLogging;
            listing.CheckboxLabeled("RimMind.Infrastructure.Settings.DebugLogging".Translate(), ref debugLogging,
                "RimMind.Infrastructure.Settings.DebugLogging.Desc".Translate());
            s.DebugLogging = debugLogging;

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.UI.FlywheelAutoApply".Translate());
            {
                Rect row = listing.GetRect(28f);
                if (Widgets.ButtonText(row, GetAutoApplyModeLabel(s.AutoApplyMode)))
                {
                    var modes = new List<FloatMenuOption>();
                    foreach (FlywheelAutoApplyMode mode in Enum.GetValues(typeof(FlywheelAutoApplyMode)))
                    {
                        var label = GetAutoApplyModeLabel(mode);
                        modes.Add(new FloatMenuOption(label, () => s.AutoApplyMode = mode));
                    }
                    Find.WindowStack.Add(new FloatMenu(modes));
                }
            }

            listing.Label("RimMind.Infrastructure.UI.FlywheelConfidence".Translate(s.AutoApplyConfidenceThreshold));
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.UI.FlywheelConfidence.Desc".Translate());
            GUI.color = Color.white;
            s.AutoApplyConfidenceThreshold = listing.Slider(s.AutoApplyConfidenceThreshold, 0.5f, 1.0f);

            listing.End();
            Widgets.EndScrollView();
        }

        private static void RunConnectionTest(ISettingsProvider s)
        {
            if (!ProviderHelper.RequiresApiKey(s.Provider))
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
                    var openAISettings = RimMindServiceLocator.Get<IOpenAISettings>();
                    if (openAISettings == null)
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = "RimMind.Infrastructure.Settings.Status.NotConfigured".Translate();
                            _testStatusColor = Color.yellow;
                        });
                        return;
                    }

                    var client = new OpenAIClient(openAISettings);
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

        private static void DrawQueueTab(Rect inRect, ISettingsProvider settings)
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
            allModIds.UnionWith(RimMindServiceLocator.Get<IExtensionRegistry<IModCooldown>>()?.All.Select(c => c.Id) ?? Enumerable.Empty<string>());

            int modCount = allModIds.Count;
            int activeCount = queue.ActiveRequestCount;
            int queuedCount = queue.TotalQueuedCount;
            float contentH = 60f + 28f + modCount * 26f + 28f + activeCount * 24f + 28f + queuedCount * 24f + 80f;
            contentH = Mathf.Max(contentH, inRect.height + 10f);

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentH);
            Widgets.BeginScrollView(inRect, ref _queueScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Infrastructure.Settings.Queue.Status".Translate());

            string pauseLabel = queue.IsPaused
                ? "RimMind.Infrastructure.Settings.QueuePaused".Translate()
                : "RimMind.Infrastructure.Settings.QueueRunning".Translate();
            GUI.color = queue.IsPaused ? Color.yellow : new Color(0.4f, 0.9f, 0.4f);
            listing.Label(pauseLabel);
            GUI.color = Color.white;

            listing.Label($"{"RimMind.Infrastructure.Settings.Queue.Active".Translate()}: {activeCount} / {settings.MaxConcurrentRequests}");
            listing.Label($"{"RimMind.Infrastructure.Settings.Queue.Queued".Translate()}: {queuedCount}");
            GUI.color = queue.IsLocalModelBusy ? new Color(0.9f, 0.6f, 0.3f) : new Color(0.4f, 0.9f, 0.4f);
            listing.Label($"{"RimMind.Infrastructure.Settings.Queue.LocalModel".Translate()}: {(queue.IsLocalModelBusy ? "RimMind.Infrastructure.Settings.Queue.Busy".Translate() : "RimMind.Infrastructure.Settings.Queue.Idle".Translate())}");
            GUI.color = Color.white;

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

        private static void DrawPromptsTab(Rect inRect, ISettingsProvider s)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 460f);
            Widgets.BeginScrollView(inRect, ref _promptsScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            GUI.color = Color.gray;
            listing.Label("RimMind.Presentation.Prompts.Desc".Translate());
            GUI.color = Color.white;
            listing.Gap(8f);

            var customPawnPrompt = s.CustomPawnPrompt;
            SettingsUIHelper.DrawCustomPromptSection(listing,
                "RimMind.Presentation.Prompts.PawnPromptLabel".Translate(),
                ref customPawnPrompt, 100f);
            s.CustomPawnPrompt = customPawnPrompt;

            listing.Gap(12f);

            var customMapPrompt = s.CustomMapPrompt;
            SettingsUIHelper.DrawCustomPromptSection(listing,
                "RimMind.Presentation.Prompts.MapPromptLabel".Translate(),
                ref customMapPrompt, 100f);
            s.CustomMapPrompt = customMapPrompt;

            listing.End();
            Widgets.EndScrollView();
        }

        private static void DrawContextTab(Rect inRect, ISettingsProvider s)
        {
            var ctx = s.Context;

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 980f);
            Widgets.BeginScrollView(inRect, ref _contextScroll, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            GUI.color = Color.gray;
            listing.Label("RimMind.Presentation.Context.Desc".Translate());
            GUI.color = Color.white;
            listing.Gap(8f);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Presentation.Context.Presets".Translate());
            DrawPresetCards(listing, ctx);
            listing.Gap(12f);

            float colW = (listing.ColumnWidth - 20f) / 2f;
            Rect anchor = listing.GetRect(0f);

            var left = new Listing_Standard();
            left.Begin(new Rect(anchor.x, anchor.y, colW, 9999f));
            GUI.color = new Color(0.6f, 0.78f, 1f);
            left.Label("RimMind.Presentation.Context.PawnInfo".Translate());
            GUI.color = Color.white;
            left.Gap(4f);
            var includeRace = ctx.IncludeRace;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeRace".Translate(), ref includeRace, "RimMind.Presentation.Context.IncludeRace.Desc".Translate());
            ctx.IncludeRace = includeRace;
            var includeAge = ctx.IncludeAge;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeAge".Translate(), ref includeAge, "RimMind.Presentation.Context.IncludeAge.Desc".Translate());
            ctx.IncludeAge = includeAge;
            var includeGender = ctx.IncludeGender;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeGender".Translate(), ref includeGender, "RimMind.Presentation.Context.IncludeGender.Desc".Translate());
            ctx.IncludeGender = includeGender;
            var includeBackstory = ctx.IncludeBackstory;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeBackstory".Translate(), ref includeBackstory, "RimMind.Presentation.Context.IncludeBackstory.Desc".Translate());
            ctx.IncludeBackstory = includeBackstory;
            var includeIdeology = ctx.IncludeIdeology;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeIdeology".Translate(), ref includeIdeology, "RimMind.Presentation.Context.IncludeIdeology.Desc".Translate());
            ctx.IncludeIdeology = includeIdeology;
            var includeTraits = ctx.IncludeTraits;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeTraits".Translate(), ref includeTraits, "RimMind.Presentation.Context.IncludeTraits.Desc".Translate());
            ctx.IncludeTraits = includeTraits;
            var includeSkills = ctx.IncludeSkills;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeSkills".Translate(), ref includeSkills, "RimMind.Presentation.Context.IncludeSkills.Desc".Translate());
            ctx.IncludeSkills = includeSkills;
            if (ctx.IncludeSkills)
            {
                left.Label($"  {"RimMind.Presentation.Context.MinSkillLevel".Translate()}: {ctx.MinSkillLevel}");
                ctx.MinSkillLevel = (int)left.Slider(ctx.MinSkillLevel, 1f, 15f);
            }
            var includeHealth = ctx.IncludeHealth;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeHealth".Translate(), ref includeHealth, "RimMind.Presentation.Context.IncludeHealth.Desc".Translate());
            ctx.IncludeHealth = includeHealth;
            var includeCapacities = ctx.IncludeCapacities;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeCapacities".Translate(), ref includeCapacities, "RimMind.Presentation.Context.IncludeCapacities.Desc".Translate());
            ctx.IncludeCapacities = includeCapacities;
            var includeMood = ctx.IncludeMood;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeMood".Translate(), ref includeMood, "RimMind.Presentation.Context.IncludeMood.Desc".Translate());
            ctx.IncludeMood = includeMood;
            var includeMoodThoughts = ctx.IncludeMoodThoughts;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeMoodThoughts".Translate(), ref includeMoodThoughts, "RimMind.Presentation.Context.IncludeMoodThoughts.Desc".Translate());
            ctx.IncludeMoodThoughts = includeMoodThoughts;
            var includeCurrentJob = ctx.IncludeCurrentJob;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeCurrentJob".Translate(), ref includeCurrentJob, "RimMind.Presentation.Context.IncludeCurrentJob.Desc".Translate());
            ctx.IncludeCurrentJob = includeCurrentJob;
            var includeWorkPriorities = ctx.IncludeWorkPriorities;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeWorkPriorities".Translate(), ref includeWorkPriorities, "RimMind.Presentation.Context.IncludeWorkPriorities.Desc".Translate());
            ctx.IncludeWorkPriorities = includeWorkPriorities;
            var includeEquipment = ctx.IncludeEquipment;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeEquipment".Translate(), ref includeEquipment, "RimMind.Presentation.Context.IncludeEquipment.Desc".Translate());
            ctx.IncludeEquipment = includeEquipment;
            var includeInventory = ctx.IncludeInventory;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeInventory".Translate(), ref includeInventory, "RimMind.Presentation.Context.IncludeInventory.Desc".Translate());
            ctx.IncludeInventory = includeInventory;
            var includeLocation = ctx.IncludeLocation;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeLocation".Translate(), ref includeLocation, "RimMind.Presentation.Context.IncludeLocation.Desc".Translate());
            ctx.IncludeLocation = includeLocation;
            var includeRelations = ctx.IncludeRelations;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeRelations".Translate(), ref includeRelations, "RimMind.Presentation.Context.IncludeRelations.Desc".Translate());
            ctx.IncludeRelations = includeRelations;
            var includeGenes = ctx.IncludeGenes;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeGenes".Translate(), ref includeGenes, "RimMind.Presentation.Context.IncludeGenes.Desc".Translate());
            ctx.IncludeGenes = includeGenes;
            var includeCombatStatus = ctx.IncludeCombatStatus;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeCombatStatus".Translate(), ref includeCombatStatus, "RimMind.Presentation.Context.IncludeCombatStatus.Desc".Translate());
            ctx.IncludeCombatStatus = includeCombatStatus;
            var includeSurroundings = ctx.IncludeSurroundings;
            left.CheckboxLabeled("RimMind.Presentation.Context.IncludeSurroundings".Translate(), ref includeSurroundings, "RimMind.Presentation.Context.IncludeSurroundings.Desc".Translate());
            ctx.IncludeSurroundings = includeSurroundings;
            float leftH = left.CurHeight;
            left.End();

            var right = new Listing_Standard();
            right.Begin(new Rect(anchor.x + colW + 20f, anchor.y, colW, 9999f));
            GUI.color = new Color(0.6f, 0.78f, 1f);
            right.Label("RimMind.Presentation.Context.Environment".Translate());
            GUI.color = Color.white;
            right.Gap(4f);
            var includeGameTime = ctx.IncludeGameTime;
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeGameTime".Translate(), ref includeGameTime, "RimMind.Presentation.Context.IncludeGameTime.Desc".Translate());
            ctx.IncludeGameTime = includeGameTime;
            var includeColonistCount = ctx.IncludeColonistCount;
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeColonistCount".Translate(), ref includeColonistCount, "RimMind.Presentation.Context.IncludeColonistCount.Desc".Translate());
            ctx.IncludeColonistCount = includeColonistCount;
            var includeColonistNames = ctx.IncludeColonistNames;
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeColonistNames".Translate(), ref includeColonistNames, "RimMind.Presentation.Context.IncludeColonistNames.Desc".Translate());
            ctx.IncludeColonistNames = includeColonistNames;
            var includeWealth = ctx.IncludeWealth;
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeWealth".Translate(), ref includeWealth, "RimMind.Presentation.Context.IncludeWealth.Desc".Translate());
            ctx.IncludeWealth = includeWealth;
            var includeFood = ctx.IncludeFood;
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeFood".Translate(), ref includeFood, "RimMind.Presentation.Context.IncludeFood.Desc".Translate());
            ctx.IncludeFood = includeFood;
            var includeSeason = ctx.IncludeSeason;
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeSeason".Translate(), ref includeSeason, "RimMind.Presentation.Context.IncludeSeason.Desc".Translate());
            ctx.IncludeSeason = includeSeason;
            var includeWeather = ctx.IncludeWeather;
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeWeather".Translate(), ref includeWeather, "RimMind.Presentation.Context.IncludeWeather.Desc".Translate());
            ctx.IncludeWeather = includeWeather;
            var includeThreats = ctx.IncludeThreats;
            right.CheckboxLabeled("RimMind.Presentation.Context.IncludeThreats".Translate(), ref includeThreats, "RimMind.Presentation.Context.IncludeThreats.Desc".Translate());
            ctx.IncludeThreats = includeThreats;
            float rightH = right.CurHeight;
            right.End();

            listing.Gap(Mathf.Max(leftH, rightH) + 8f);

            SettingsUIHelper.DrawSectionHeader(listing, "RimMind.Presentation.Context.Budget".Translate());
            listing.Label($"{"RimMind.Presentation.Context.ContextBudget".Translate()}: {ctx.ContextBudget:F1}");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Presentation.Context.ContextBudget.Desc".Translate());
            GUI.color = Color.white;
            ctx.ContextBudget = listing.Slider(ctx.ContextBudget, 0.1f, 2.0f);

#pragma warning disable CS0618
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
#pragma warning restore CS0618

            listing.Gap(8f);

            listing.Label($"{"RimMind.Infrastructure.Settings.ContextDiffLifetime".Translate()}: {s.ContextDiffLifetimeTicks / 60f:F0}s ({s.ContextDiffLifetimeTicks} ticks)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.ContextDiffLifetime.Desc".Translate());
            GUI.color = Color.white;
            s.ContextDiffLifetimeTicks = (int)listing.Slider(s.ContextDiffLifetimeTicks, 300f, 3000f);

            listing.Gap(6f);
            var calibrateSec = s.ContextCalibrateInterval / 60f;
            listing.Label($"{"RimMind.Infrastructure.Settings.CalibrateInterval".Translate()}: {calibrateSec:F0}s ({s.ContextCalibrateInterval} ticks)");
            GUI.color = Color.gray;
            listing.Label("  " + "RimMind.Infrastructure.Settings.CalibrateInterval.Desc".Translate());
            GUI.color = Color.white;
            s.ContextCalibrateInterval = (int)listing.Slider(s.ContextCalibrateInterval, 5000f, 60000f);

            if (listing.ButtonText("RimMind.Presentation.Context.ResetDefault".Translate()))
            {
                s.Context.ResetToDefault();
                _selectedPreset = ContextPreset.Standard;
            }

            listing.End();
            Widgets.EndScrollView();
        }

        private static void DrawPresetCards(Listing_Standard listing, IContextSettings ctx)
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

        private static string GetProviderLabel(string p)
        {
            string key = $"RimMind.Infrastructure.Settings.Provider.{p}";
            var translation = key.Translate();
            return translation == key ? p : translation;
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
