using System;
using RimMind.Application.Common.Models.UI;
using Xunit;

namespace RimMind.Tests
{
    public class RequestEntryLifecycleTests
    {
        [Fact]
        public void TryComplete_IsOneShotAcrossCompetingTerminalPaths()
        {
            var selected = 0;
            var completed = 0;
            var completionReason = RequestCompletionReason.Dismissed;
            var entry = new RequestEntry
            {
                callback = _ => selected++,
                completionCallback = reason =>
                {
                    completed++;
                    completionReason = reason;
                }
            };

            Assert.True(entry.TryComplete("approve", RequestCompletionReason.Selected));
            Assert.False(entry.TryComplete("reject", RequestCompletionReason.Expired));

            Assert.Equal(1, selected);
            Assert.Equal(1, completed);
            Assert.Equal(RequestCompletionReason.Selected, completionReason);
        }

        [Fact]
        public void TryComplete_StillPublishesTerminalStateWhenChoiceCallbackThrows()
        {
            var completed = 0;
            var entry = new RequestEntry
            {
                callback = _ => throw new InvalidOperationException("choice failed"),
                completionCallback = _ => completed++
            };

            Assert.Throws<InvalidOperationException>(() =>
                entry.TryComplete("approve", RequestCompletionReason.Selected));

            Assert.Equal(1, completed);
            Assert.False(entry.TryComplete(null, RequestCompletionReason.Dismissed));
        }
    }
}
