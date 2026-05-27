using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Helpers;
using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models.Client;
using RimMind.Application.Common.Models;
using RimMind.Domain.Enums;
using RimMind.Domain.Llm;
using DomainChatMessage = RimMind.Domain.Llm.ChatMessage;
using RimMind.Presentation.Runtime;
using RimMind.Presentation.Settings;
using UnityEngine;
using Verse;

namespace RimMind.Presentation.UI
{
    internal static partial class ApiTabDrawer
    {
        private static void RunConnectionTest(ISettingsProvider s)
        {
            if (!AIProviderRegistry.RequiresApiKey(s.Provider))
            {
                _testStatus = "RimMind.Settings.Status.Testing".Translate();
                _testStatusColor = Color.yellow;

                Task.Run(async () =>
                {
                    try
                    {
                        var client = GetClientManager()?.GetPlayer2Client();
                        if (client == null)
                        {
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _testStatus = "RimMind.Settings.Player2.NotAvailable".Translate();
                                _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                            });
                            return;
                        }

                        var envelope = new LlmRequestEnvelope
                        {
                            RequestId = "test",
                            ScenarioId = "RimMind.Test",
                            ModId = "RimMind.Test",
                            Messages = new List<DomainChatMessage> { new DomainChatMessage { Role = "user", Content = "RimMind.Settings.TestMessage".Translate() } },
                            MaxTokens = RimMindDefaults.TestConnectionMaxTokens,
                            Temperature = 0.7f,
                        };
                        var result = await client.SendAsync(envelope);
                        if (result.TryGetValue(out var response))
                        {
                            var content = response.Content.Trim();
                            var tok = response.TokensUsed;
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _testStatus = $"OK {content} ({tok} tok)";
                                _testStatusColor = new Color(0.4f, 0.9f, 0.4f);
                            });
                        }
                        else
                        {
                            var error = result.Error.Message;
                            LongEventHandler.ExecuteWhenFinished(() =>
                            {
                                _testStatus = $"FAIL {error}";
                                _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message;
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = $"FAIL {msg}";
                            _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                        });
                    }
                });
                return;
            }

            if (!s.IsOpenAIConfigured())
            {
                _testStatus = "RimMind.Settings.Status.NotConfigured".Translate();
                _testStatusColor = Color.yellow;
                return;
            }

            _testStatus = "RimMind.Settings.Status.Testing".Translate();
            _testStatusColor = Color.yellow;

            Task.Run(async () =>
            {
                try
                {
                    var openAISettings = GetOpenAISettings();
                    if (openAISettings == null)
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = "RimMind.Settings.Status.NotConfigured".Translate();
                            _testStatusColor = Color.yellow;
                        });
                        return;
                    }

                    var client = GetClientManager()?.GetClient();
                    if (client == null)
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = "RimMind.Settings.Status.NotConfigured".Translate();
                            _testStatusColor = Color.yellow;
                        });
                        return;
                    }

                    var envelope2 = new LlmRequestEnvelope
                    {
                        RequestId = "test",
                        ScenarioId = "RimMind.Test",
                        ModId = "RimMind.Test",
                        Messages = new List<DomainChatMessage> { new DomainChatMessage { Role = "user", Content = "RimMind.Settings.TestMessage".Translate() } },
                        MaxTokens = 60,
                        Temperature = 0.7f,
                    };
                    var result2 = await client.SendAsync(envelope2);
                    if (result2.TryGetValue(out var response2))
                    {
                        var content = response2.Content.Trim();
                        var tok = response2.TokensUsed;
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = $"OK {content} ({tok} tok)";
                            _testStatusColor = new Color(0.4f, 0.9f, 0.4f);
                        });
                    }
                    else
                    {
                        var error = result2.Error.Message;
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _testStatus = $"FAIL {error}";
                            _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                        });
                    }
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        _testStatus = $"FAIL {msg}";
                        _testStatusColor = new Color(0.9f, 0.4f, 0.4f);
                    });
                }
            });
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

        internal static string GetProviderLabel(string p)
        {
            string key = $"RimMind.Settings.Provider.{p}";
            var translation = key.Translate();
            return translation == key ? p : translation;
        }

        private static string GetAutoApplyModeLabel(FlywheelAutoApplyMode mode)
        {
            return mode switch
            {
                FlywheelAutoApplyMode.Off => "RimMind.UI.FlywheelAutoApply.Off".Translate(),
                FlywheelAutoApplyMode.LogOnly => "RimMind.UI.FlywheelAutoApply.LogOnly".Translate(),
                FlywheelAutoApplyMode.ApplyWithLog => "RimMind.UI.FlywheelAutoApply.Apply".Translate(),
                _ => mode.ToString()
            };
        }
    }
}
