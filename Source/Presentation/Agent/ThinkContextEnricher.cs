using System.Collections.Generic;
using System.Linq;
using RimMind.Application.Common.Interfaces.Agent;
using RimMind.Application.Common.Interfaces.Agent.Psychology;
using RimMind.Application.Common.Interfaces.Internal;
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
        private InnerVoiceHandler? _innerVoiceHandler;
        private IPsychologyWatcher? _psychologyWatcher;

        private InnerVoiceHandler? GetInnerVoiceHandler()
            => _innerVoiceHandler ??= RimMindServiceLocator.Get<InnerVoiceHandler>();

        private IPsychologyWatcher? GetPsychologyWatcher()
            => _psychologyWatcher ??= RimMindServiceLocator.Get<IPsychologyWatcher>();

        public string? ConsumeInnerVoice(string npcId)
        {
            var handler = GetInnerVoiceHandler();
            var text = handler?.GetPendingVoiceText(npcId);
            if (!string.IsNullOrEmpty(text))
                handler?.ClearVoice(npcId);
            return text;
        }

        public void CheckPsychology(IPawnAgent agent, int pawnId)
        {
            GetPsychologyWatcher()?.CheckAndPublish(agent, pawnId);
        }

        public void EnrichEnvelope(LlmRequestEnvelope envelope, string npcId, string? voiceText)
        {
            if (envelope == null) return;

            var prefix = "";
            if (!string.IsNullOrEmpty(voiceText))
                prefix += $"[Inner Voice: {voiceText}]\n";

            if (GetPsychologyWatcher()?.HasUrgentEvent(npcId) == true)
                prefix += "[Psychology Alert: Urgent psychological event pending]\n";

            if (!string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(envelope.GameStateInfo))
                envelope.GameStateInfo = prefix + envelope.GameStateInfo;
            else if (!string.IsNullOrEmpty(prefix))
                envelope.GameStateInfo = prefix.TrimEnd('\n');
        }

        /// <summary>
        /// Formats ToolCall execution results as a context section for the follow-up envelope.
        /// </summary>
        public string FormatToolCallResults(IReadOnlyList<ToolResult> results, int round)
            => ToolCallResultFormatter.Format(results, round);

        /// <summary>
        /// Enriches the envelope with ToolCall results context for agentic loop follow-up.
        /// </summary>
        public void EnrichWithToolCallResults(LlmRequestEnvelope envelope, IReadOnlyList<ToolResult> results, int round)
        {
            if (envelope == null || results == null || results.Count == 0) return;

            var toolCallSection = FormatToolCallResults(results, round);
            if (string.IsNullOrEmpty(toolCallSection)) return;

            if (!string.IsNullOrEmpty(envelope.GameStateInfo))
                envelope.GameStateInfo = toolCallSection + "\n" + envelope.GameStateInfo;
            else
                envelope.GameStateInfo = toolCallSection;
        }

        /// <summary>
        /// Formats recent behavior history as a context section for the envelope.
        /// </summary>
        public string FormatBehaviorHistory(IReadOnlyList<BehaviorRecord> history, float successRate)
        {
            if (history == null || history.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Recent Behavior History]");
            foreach (var record in history)
            {
                var status = record.Success ? "Success" : "Fail";
                sb.AppendLine($"- {record.Action} → {status}: {record.Reason}");
            }

            if (successRate < 0.3f)
            {
                sb.AppendLine("[Warning: Recent behavior success rate is low. Consider more cautious decisions.]");
            }

            return sb.ToString();
        }
    }
}
