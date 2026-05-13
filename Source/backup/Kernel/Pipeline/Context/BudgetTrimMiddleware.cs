using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Kernel.Pipeline.Context;
using RimMind.Contracts.Client;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Prompt;
using RimMind.Contracts.Context;
using RimMind.Kernel.Logging;
using RimMind.Kernel.Context;
using RimMind.Contracts.Flywheel;
using RimMind.Kernel.Flywheel;
using RimMind.Kernel.Prompt;
using RimMind.Core;

namespace RimMind.Kernel.Pipeline.Context
{
    internal sealed class BudgetTrimMiddleware : IMiddleware<ContextBuildContext>
    {
        public string Id => Name;
        public string Name => nameof(BudgetTrimMiddleware);
        public int Order => 2;

        public Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            var snapshot = context.Snapshot;
            if (snapshot == null || snapshot.Messages.Count == 0)
                return next(context);

            int totalBudget = RimMindServiceLocator.Get<IFlywheelParameterStore>()?.TotalBudget ?? 4000;
            int reserveForOutput = RimMindCoreMod.Settings?.maxTokens > 0
                ? RimMindCoreMod.Settings.maxTokens
                : 800;
            float budgetRatio = RimMindCoreMod.Settings?.Context?.ContextBudget ?? 0.6f;
            int available = (int)(totalBudget * budgetRatio) - reserveForOutput;
            if (available <= 0) available = totalBudget - reserveForOutput;

            if (snapshot.EstimatedTokens <= available)
                return next(context);

            var sections = new List<PromptSection>();
            foreach (var msg in snapshot.Messages)
            {
                int priority = msg.Role switch
                {
                    "system" when msg.LayerTag == "L0" => PromptSection.PriorityCore,
                    "system" => PromptSection.PriorityKeyState,
                    "user" => PromptSection.PriorityCurrentInput,
                    "assistant" => PromptSection.PriorityAuxiliary,
                    _ => PromptSection.PriorityAuxiliary
                };

                var section = new PromptSection(msg.Role ?? "unknown", msg.Content ?? "", priority)
                {
                    LayerTag = msg.LayerTag
                };

                if (msg.Role == "system" && (msg.LayerTag == "L2" || msg.LayerTag == "L3" || msg.LayerTag == "L5"))
                    section.Compress = CompressToBrief;

                sections.Add(section);
            }

            var budget = new PromptBudget(totalBudget, reserveForOutput);
            var trimmed = budget.Compose(sections) ?? new List<PromptSection>();

            snapshot.ClearMessages();
            foreach (var sec in trimmed)
            {
                snapshot.AddMessage(new ChatMessage
                {
                    Role = sec.Tag,
                    Content = sec.Content,
                    LayerTag = sec.LayerTag
                });
            }
            snapshot.EstimatedTokens = trimmed.Sum(s => s.EstimatedTokens);
            snapshot.Meta.TotalTokens = snapshot.EstimatedTokens;

            return next(context);
        }

        private static string CompressToBrief(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            int maxLen = 200;
            return content.Length <= maxLen ? content : content.Substring(0, maxLen) + "...";
        }
    }
}
