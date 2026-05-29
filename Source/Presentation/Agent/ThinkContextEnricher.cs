using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Tools;
using RimMind.Application.Features.Agent.InnerVoice;
using RimMind.Domain.Llm;

namespace RimMind.Presentation.Agent
{
    /// <summary>
    /// Enriches LlmRequestEnvelope with InnerVoice and Psychology alert context.
    /// Extracted from PawnThinker to keep it focused on mode coordination.
    /// </summary>
    internal sealed class ThinkContextEnricher
    {
        private readonly InnerVoiceHandler? _innerVoiceHandler;
        private readonly IPsychologyWatcher? _psychologyWatcher;

        public ThinkContextEnricher(
            InnerVoiceHandler? voiceHandler = null,
            IPsychologyWatcher? psychologyWatcher = null)
        {
            _innerVoiceHandler = voiceHandler;
            _psychologyWatcher = psychologyWatcher;
        }

        public string? ConsumeInnerVoice(string npcId)
        {
            var text = _innerVoiceHandler?.GetPendingVoiceText(npcId);
            if (!string.IsNullOrEmpty(text))
                _innerVoiceHandler?.ClearVoice(npcId);
            return text;
        }

        public void CheckPsychology(IPawnAgent agent, int pawnId)
        {
            _psychologyWatcher?.CheckAndPublish(agent, pawnId);
        }

        public void EnrichEnvelope(LlmRequestEnvelope envelope, string npcId, string? voiceText)
        {
            if (envelope == null) return;

            envelope.GameStateInfo ??= new GameStateInfo();

            if (!string.IsNullOrEmpty(voiceText))
                envelope.GameStateInfo.AddSection("inner_voice", voiceText);

            if (_psychologyWatcher?.HasUrgentEvent(npcId) == true)
                envelope.GameStateInfo.AddSection("psychology_alert", "Urgent psychological event pending");
        }

        /// <summary>
        /// Formats ToolCall execution results as a context section for the follow-up envelope.
        /// </summary>
        public string FormatToolCallResults(IReadOnlyList<ToolResult> results, int round)
        {
            var inner = ToolCallResultFormatter.Format(results, round);
            if (string.IsNullOrEmpty(inner)) return "";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<tool_call_results round=\"{round}\">");
            sb.AppendLine(inner);
            sb.AppendLine("</tool_call_results>");
            return sb.ToString();
        }

        /// <summary>
        /// Enriches the envelope with ToolCall results context for agentic loop follow-up.
        /// </summary>
        public void EnrichWithToolCallResults(LlmRequestEnvelope envelope, IReadOnlyList<ToolResult> results, int round)
        {
            if (envelope == null || results == null || results.Count == 0) return;

            var toolCallSection = FormatToolCallResults(results, round);
            if (string.IsNullOrEmpty(toolCallSection)) return;

            envelope.GameStateInfo ??= new GameStateInfo();
            envelope.GameStateInfo.AddSection($"tool_call_results", toolCallSection);
        }

        /// <summary>
        /// Formats recent behavior history as a context section for the envelope.
        /// </summary>
        public string FormatBehaviorHistory(IReadOnlyList<BehaviorRecord> history, float successRate)
        {
            if (history == null || history.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<behavior_history>");
            foreach (var record in history)
            {
                var status = record.Success ? "Success" : "Fail";
                sb.AppendLine($"- {record.Action} → {status}: {record.Reason}");
            }

            if (successRate < 0.4f)
            {
                sb.AppendLine("[Warning: Recent behavior success rate is low. Consider more cautious decisions.]");
            }

            sb.AppendLine("</behavior_history>");
            return sb.ToString();
        }
    }
}
