using RimMind.Application.Common.Interfaces;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Models.Npc;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Infrastructure.Verse;

using UnityEngine;
using Verse;

namespace RimMind.Infrastructure.UI
{
    public class Window_AgentDialogue : Window
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

        // Route through ServiceLocator (Application layer) instead of RimMindRuntime (Presentation layer)
        private IHistoryManager? GetHistoryManager()
            => _cachedHistoryManager ??= RimMindServiceLocator.Get<IHistoryManager>();

        private ISettingsProvider? GetSettingsProvider()
            => _cachedSettingsProvider ??= RimMindServiceLocator.Get<ISettingsProvider>();

        private IContextBuilder? GetContextEngine()
            => _cachedContextEngine ??= RimMindServiceLocator.Get<IContextBuilder>();

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

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            string title = $"{_pawn.LabelShortCap} - {"RimMind.UI.AgentDialogue.Title".Translate()}";
            Widgets.Label(new Rect(0f, 0f, inRect.width, 30f), title);
            Text.Font = GameFont.Small;

            float historyHeight = inRect.height - 70f;
            var historyRect = new Rect(0f, 35f, inRect.width, historyHeight);

            DrawHistory(historyRect);

            float inputY = inRect.height - 30f;
            var inputRect = new Rect(0f, inputY, inRect.width - 100f, 30f);
            var sendRect = new Rect(inRect.width - 95f, inputY, 95f, 30f);

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
                    if (role == "assistant" && content == "RimMind.UI.AgentDialogue.Thinking".Translate())
                        displayContent = content;
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

            string thinkingText = "RimMind.UI.AgentDialogue.Thinking".Translate();
            GetHistoryManager()?.AddTurn(_npcId, message, thinkingText, "Dialogue");

            var npcId = _npcId;
            _agent.ForceThink();

            var settings = GetSettingsProvider();
            var request = new ContextRequest
            {
                NpcId = _agent?.NpcId ?? $"NPC-{_pawn.thingIDNumber}",
                Scenario = ScenarioIds.Dialogue,
                Budget = RimMindDefaults.DefaultContextBudget,
                CurrentQuery = message,
                MaxTokens = settings?.MaxTokens ?? RimMindDefaults.MaxTokens,
                Temperature = settings?.DefaultTemperature ?? RimMindDefaults.DefaultTemperature,
            };

            var engine = GetContextEngine();
            var snapshot = engine.BuildSnapshot(request);
            var driver = RimMind.Infrastructure.Persistence.StorageDriverFactory.GetDriver();

            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var result = await driver.ChatAsync(snapshot.NpcId, snapshot.CurrentQuery ?? "", null);
                    LongEventHandler.ExecuteWhenFinished(() =>
                    {
                        var hm = GetHistoryManager();
                        var currentHistory = hm.GetHistory(npcId, MaxHistoryRounds);
                        if (currentHistory != null)
                        {
                            for (int i = currentHistory.Count - 1; i >= 0; i--)
                            {
                                if (currentHistory[i].role == "assistant" && currentHistory[i].content == thinkingText)
                                {
                                    hm.ReplaceLastAssistantTurn(npcId, result.Value.Message ?? "");
                                    break;
                                }
                            }
                        }
                    });
                }
                catch (System.Exception ex)
                {
                    RimMindErrors.Warn($"[RimMind-Core] AgentDialogue chat failed: {ex.Message}");
                }
            });
        }
    }
}
