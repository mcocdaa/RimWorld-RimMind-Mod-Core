using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Context;

namespace RimMind.Infrastructure.UI.AgentStatePreview
{
    internal enum AgentContextPreviewState
    {
        Idle,
        Pending,
        Completed,
        Faulted
    }

    /// <summary>
    /// Keeps asynchronous context-preview work independent from the Agent State window.
    /// Call <see cref="Poll"/> only from the UI thread; it never waits for incomplete work.
    /// </summary>
    internal sealed class AgentContextPreviewCoordinator
    {
        private Task<ContextSnapshot?>? _pendingTask;

        public AgentContextPreviewState State { get; private set; }
        public string Summary { get; private set; } = string.Empty;

        public void Begin(Task<ContextSnapshot?> previewTask, string loadingSummary)
        {
            _pendingTask = previewTask ?? throw new ArgumentNullException(nameof(previewTask));
            Summary = loadingSummary ?? string.Empty;
            State = AgentContextPreviewState.Pending;
        }

        public void MarkUnavailable(string unavailableSummary)
        {
            _pendingTask = null;
            Summary = unavailableSummary ?? string.Empty;
            State = AgentContextPreviewState.Faulted;
        }

        public void Poll(string unavailableSummary, Func<ContextSnapshot, string> formatSnapshot)
        {
            if (_pendingTask == null || !_pendingTask.IsCompleted)
                return;

            Task<ContextSnapshot?> completedTask = _pendingTask;
            _pendingTask = null;

            if (completedTask.IsFaulted || completedTask.IsCanceled)
            {
                Summary = unavailableSummary ?? string.Empty;
                State = AgentContextPreviewState.Faulted;
                return;
            }

            ContextSnapshot? snapshot = completedTask.GetAwaiter().GetResult();
            if (snapshot == null)
            {
                Summary = unavailableSummary ?? string.Empty;
                State = AgentContextPreviewState.Faulted;
                return;
            }

            Summary = formatSnapshot(snapshot) ?? string.Empty;
            State = AgentContextPreviewState.Completed;
        }
    }
}
