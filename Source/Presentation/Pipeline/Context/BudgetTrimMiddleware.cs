using System.Threading.Tasks;
using RimMind.Application.Common.Interfaces.Pipeline;
using RimMind.Application.Common.Models.Pipeline;
using RimMind.Application.Common.Models.Context;
using RimMind.Application.Common.Interfaces.Internal;
using RimMind.Application.Features.Pipeline.Context;

namespace RimMind.Presentation.Pipeline.Context
{
    internal sealed class BudgetTrimMiddleware : IMiddleware<ContextBuildContext>
    {
        private readonly IAIModelSettings _modelSettings;
        private readonly IContextBudgetSettings _budgetSettings;

        public BudgetTrimMiddleware(IAIModelSettings modelSettings, IContextBudgetSettings budgetSettings)
        {
            _modelSettings = modelSettings;
            _budgetSettings = budgetSettings;
        }

        public string Id => Name;
        public string OwnerModId => "RimMindCore";
        public string Name => nameof(BudgetTrimMiddleware);
        public int Order => 2;

        public Task InvokeAsync(ContextBuildContext context, MiddlewareDelegate<ContextBuildContext> next)
        {
            var snapshot = context.Snapshot;
            if (snapshot == null || snapshot.Messages.Count == 0)
                return next(context);

            int totalBudget = 4000;
            int reserveForOutput = _modelSettings.MaxTokens > 0 ? _modelSettings.MaxTokens : 800;
            float budgetRatio = _budgetSettings.ContextBudget;
            int available = (int)(totalBudget * budgetRatio) - reserveForOutput;
            if (available <= 0) available = totalBudget - reserveForOutput;

            if (snapshot.EstimatedTokens <= available)
                return next(context);

            TrimMessages(snapshot, available);

            return next(context);
        }

        private static void TrimMessages(ContextSnapshot snapshot, int maxTokens)
        {
            if (snapshot.Messages == null || snapshot.Messages.Count <= 1) return;
            int estimatedTokens = 0;
            foreach (var msg in snapshot.Messages)
                estimatedTokens += (msg.Content?.Length ?? 0) / 4;

            while (estimatedTokens > maxTokens && snapshot.Messages.Count > 1)
            {
                int removed = (snapshot.Messages[1].Content?.Length ?? 0) / 4;
                snapshot.RemoveMessageAt(1);
                estimatedTokens -= removed;
            }
        }
    }
}
