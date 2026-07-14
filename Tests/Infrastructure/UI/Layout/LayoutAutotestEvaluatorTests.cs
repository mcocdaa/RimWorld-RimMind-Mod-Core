using System.Collections.Generic;
using RimMind.Infrastructure.UI.Layout;
using RimMind.Presentation.UI.Layout;
using Xunit;

namespace RimMind.Tests.Infrastructure.UI.Layout
{
    public class LayoutAutotestEvaluatorTests
    {
        [Fact]
        public void Evaluate_MissingReport_IsFailure_NotSuccessfulSkip()
        {
            var evaluation = LayoutAutotestEvaluator.Evaluate(
                new[] { "RenderedWindow", "MissingWindow" },
                name => name == "RenderedWindow"
                    ? new LayoutReport(name, new List<LayoutConflict>())
                    : null);

            Assert.Equal(1, evaluation.PassCount);
            Assert.Equal(1, evaluation.FailCount);
            Assert.Equal(1, evaluation.MissingReportCount);
            Assert.False(evaluation.IsSuccess);
            Assert.Contains(evaluation.Details, detail => detail.Contains("[FAIL] MissingWindow"));
        }

        [Fact]
        public void Evaluate_ConflictingReport_IsFailure_AndKeepsConflictDetail()
        {
            var conflict = LayoutConflict.NegativeSize(default);
            var evaluation = LayoutAutotestEvaluator.Evaluate(
                new[] { "ConflictingWindow" },
                name => new LayoutReport(name, new[] { conflict }));

            Assert.Equal(0, evaluation.PassCount);
            Assert.Equal(1, evaluation.FailCount);
            Assert.Equal(0, evaluation.MissingReportCount);
            Assert.Contains(evaluation.Details, detail => detail.Contains(conflict.Message));
        }
    }
}
