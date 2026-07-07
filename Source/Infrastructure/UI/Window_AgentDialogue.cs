using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Interfaces.Storage;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Features.Llm;
using RimMind.Domain.Llm;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Presentation.UI.Layout;
using RimMind.Infrastructure.Verse;

using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_AgentDialogue : RimMindWindowBase
    {
        private readonly Pawn _pawn;
        private readonly IAgentControl? _agent;
        private readonly string _npcId;
        private string _inputText = "";
        private Vector2 _scrollPosition;
        private float _lastContentHeight;
        private const int MaxHistoryRounds = RimMindDefaults.MaxHistoryRounds;

        private IHistoryManager? _cachedHistoryManager;
        private ISettingsProvider? _cachedSettingsProvider;
        private IContextBuilder? _cachedContextEngine;
        private IRemoteSyncService? _cachedSyncService;

        // Route through ServiceLocator (Application layer) instead of RimMindRuntime (Presentation layer)
        private IHistoryManager? GetHistoryManager()
            => _cachedHistoryManager ??= RimMindServiceLocator.Get<IHistoryManager>();

        private ISettingsProvider? GetSettingsProvider()
            => _cachedSettingsProvider ??= RimMindServiceLocator.Get<ISettingsProvider>();

        private IContextBuilder? GetContextEngine()
            => _cachedContextEngine ??= RimMindServiceLocator.Get<IContextBuilder>();

        private IRemoteSyncService? GetSyncService()
            => _cachedSyncService ??= RimMindServiceLocator.Get<IRemoteSyncService>();

        private string _streamingText = "";
        private bool _isStreaming;
        private string _thinkingText = "";

        public override Vector2 InitialSize => new Vector2(500f, 500f);

        public Window_AgentDialogue(Pawn pawn) : base()
        {
            _pawn = pawn;
            _agent = CompPawnAgent.GetComp(pawn)?.Agent;
            _npcId = $"NPC-{pawn.thingIDNumber}";
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = false;
        }

        protected override void DrawContents(Rect inRect, RimMindLayoutScope scope)
        {
            Text.Font = GameFont.Medium;
            string title = $"{_pawn.LabelShortCap} - {"RimMind.UI.AgentDialogue.Title".Translate()}";
            Rect titleRect = new Rect(0f, 0f, inRect.width, 30f);
            scope.Record(titleRect, "Header:Title");
            Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;

            // NPC sync actions area (below title, above history)
            float syncAreaHeight = 34f;
            var syncRect = new Rect(0f, 35f, inRect.width, syncAreaHeight);
            scope.Record(syncRect, "Sync:Actions");
            NpcSyncActions.DrawNpcSyncActions(syncRect, _npcId, GetSyncService());

            float historyTop = 35f + syncAreaHeight + 4f;
            float historyHeight = inRect.height - 70f - syncAreaHeight - 4f;
            var historyRect = new Rect(0f, historyTop, inRect.width, historyHeight);
            scope.Record(historyRect, "History:List");

            DrawHistory(historyRect);

            float inputY = inRect.height - 30f;
            var inputRect = new Rect(0f, inputY, inRect.width - 100f, 30f);
            var sendRect = new Rect(inRect.width - 95f, inputY, 95f, 30f);
            scope.Record(inputRect, "Input:TextField");
            scope.Record(sendRect, "Button:Send");

            string prevText = _inputText;
            _inputText = Widgets.TextField(inputRect, _inputText);
            bool inputFocused = GUI.GetNameOfFocusedControl() == "AgentDialogueInput";

            if (Widgets.ButtonText(sendRect, "RimMind.UI.AgentDialogue.Send".Translate()))
            {
                SendMessage();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && inputFocused)
            {
                SendMessage();
                Event.current.Use();
            }
        }

        private void DrawHistory(Rect rect)
        {
            var history = GetHistoryManager()?.GetHistory(_npcId, MaxHistoryRounds);

            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.8f));

            float contentHeight = 0f;
            float lineSpacing = 24f;

            if (history != null)
                contentHeight = history.Count * lineSpacing + 10f;

            if (contentHeight < rect.height) contentHeight = rect.height;

            var contentRect = new Rect(0f, 0f, rect.width - 16f, contentHeight);
            var viewRect = new Rect(rect.x, rect.y, rect.width, rect.height);

            Widgets.BeginScrollView(viewRect, ref _scrollPosition, contentRect);

            float y = 5f;
            if (history != null)
            {
                foreach (var (role, content) in history)
                {
                    string prefix = role == "user"
                        ? "RimMind.UI.AgentDialogue.PlayerLabel".Translate() + ": "
                        : "RimMind.UI.AgentDialogue.AgentLabel".Translate() + ": ";
                    string displayContent = content;
                    if (role == "assistant" && content == _thinkingText)
                    {
                        // Show streaming text while waiting for final response
                        displayContent = _isStreaming && !string.IsNullOrEmpty(_streamingText)
                            ? _streamingText
                            : content;
                    }
                    string line = prefix + displayContent;
                    float height = Text.CalcHeight(line, contentRect.width - 10f) + 4f;
                    var lineRect = new Rect(5f, y, contentRect.width - 10f, height);
                    Widgets.Label(lineRect, line);
                    y += height;
                }
            }

            contentRect.height = Mathf.Max(y + 10f, rect.height);
            _lastContentHeight = contentRect.height;

            Widgets.EndScrollView();

            if (_lastContentHeight > rect.height)
            {
                _scrollPosition.y = _lastContentHeight - rect.height;
            }
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_inputText)) return;
            if (_agent == null || !_agent.IsActive) return;

            string message = _inputText.Trim();
            _inputText = "";

            _thinkingText = "RimMind.UI.AgentDialogue.Thinking".Translate();
            _streamingText = "";
            _isStreaming = true;
            GetHistoryManager()?.AddTurn(_npcId, message, _thinkingText, "Dialogue");

            var npcId = _npcId;
            var thinkingText = _thinkingText;
            _agent.ForceThink();

            var settings = GetSettingsProvider();

            var envelope = LlmRequestEnvelopeBuilder
                .ForNpc(_agent?.NpcId ?? $"NPC-{_pawn.thingIDNumber}", gameStateInfo: new GameStateInfo().AddSection("dialogue_input", message))
                .ForScenarioId(ScenarioIds.Dialogue)
                .WithModId("RimMind.Dialogue")
                .WithMaxTokens(settings?.MaxTokens ?? RimMindDefaults.MaxTokens)
                .WithTemperature(settings?.DefaultTemperature ?? RimMindDefaults.DefaultTemperature)
                .Streaming(chunk =>
                {
                    if (!string.IsNullOrEmpty(chunk.DeltaContent))
                    {
                        LongEventHandler.ExecuteWhenFinished(() =>
                        {
                            _streamingText += chunk.DeltaContent;
                        });
                    }
                })
                .Build();

            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var result = await RimMind.Presentation.Api.RimMindAPI.SendAsync(envelope);
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        _isStreaming = false;
                        _streamingText = "";
                        var hm = GetHistoryManager();
                        var currentHistory = hm.GetHistory(npcId, MaxHistoryRounds);
                        if (currentHistory != null)
                        {
                            for (int i = currentHistory.Count - 1; i >= 0; i--)
                            {
                                if (currentHistory[i].role == "assistant" && currentHistory[i].content == thinkingText)
                                {
                                    if (result.IsOk)
                                        hm.ReplaceLastAssistantTurn(npcId, result.Value.Content ?? "");
                                    break;
                                }
                            }
                        }
                    });
                }
                catch (System.Exception ex)
                {
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        _isStreaming = false;
                        _streamingText = "";
                    });
                    RimMindErrors.Warn($"[RimMind-Core] AgentDialogue chat failed: {ex.Message}");
                }
            });
        }
    }
}
