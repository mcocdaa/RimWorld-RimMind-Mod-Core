using System;
using System.Threading.Tasks;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.ValueObjects;

namespace RimMind.Infrastructure.UI.AgentFlow
{
    /// <summary>
    /// Owns the pending async work initiated by Agent Flow Lab.
    /// The window polls this coordinator while drawing, so it never waits for an
    /// incomplete context build or mechanism execution on RimWorld's UI thread.
    /// </summary>
    internal sealed class AgentFlowAsyncCoordinator
    {
        private Task<ContextSnapshot?>? _contextBuildTask;
        private Task<Result<bool, RimMindError>>? _mechanismExecutionTask;

        public bool HasPendingMechanismExecution => _mechanismExecutionTask != null;

        public void BeginContextBuild(Task<ContextSnapshot?> contextBuildTask)
        {
            _contextBuildTask = contextBuildTask ?? throw new ArgumentNullException(nameof(contextBuildTask));
        }

        public bool PollContextBuild(out ContextSnapshot? snapshot, out string? error)
        {
            snapshot = null;
            error = null;
            if (_contextBuildTask == null || !_contextBuildTask.IsCompleted)
                return false;

            Task<ContextSnapshot?> completedTask = _contextBuildTask;
            _contextBuildTask = null;
            if (completedTask.IsFaulted)
            {
                error = completedTask.Exception?.GetBaseException().Message ?? "context build failed";
                return true;
            }

            if (completedTask.IsCanceled)
            {
                error = "context build cancelled";
                return true;
            }

            snapshot = completedTask.GetAwaiter().GetResult();
            if (snapshot == null)
                error = "no snapshot returned";
            return true;
        }

        public void BeginMechanismExecution(Task<Result<bool, RimMindError>> mechanismExecutionTask)
        {
            _mechanismExecutionTask = mechanismExecutionTask
                ?? throw new ArgumentNullException(nameof(mechanismExecutionTask));
        }

        public bool PollMechanismExecution(out Result<bool, RimMindError>? result, out string? error)
        {
            result = null;
            error = null;
            if (_mechanismExecutionTask == null || !_mechanismExecutionTask.IsCompleted)
                return false;

            Task<Result<bool, RimMindError>> completedTask = _mechanismExecutionTask;
            _mechanismExecutionTask = null;
            if (completedTask.IsFaulted)
            {
                error = completedTask.Exception?.GetBaseException().Message ?? "mechanism execution failed";
                return true;
            }

            if (completedTask.IsCanceled)
            {
                error = "mechanism execution cancelled";
                return true;
            }

            result = completedTask.GetAwaiter().GetResult();
            return true;
        }

        public void ResetContextBuild()
        {
            _contextBuildTask = null;
        }
    }
}
