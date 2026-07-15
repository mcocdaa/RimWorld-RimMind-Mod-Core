using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.Enums;
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
        private readonly List<PendingMechanismExecution> _pendingMechanismExecutions = new();

        public bool HasPendingMechanismExecution => _pendingMechanismExecutions.Count > 0;

        public bool HasPendingMechanismExecutionForGeneration(int targetGeneration)
        {
            foreach (var pending in _pendingMechanismExecutions)
            {
                if (pending.Context.TargetGeneration == targetGeneration)
                    return true;
            }

            return false;
        }

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
            => BeginMechanismExecution(
                mechanismExecutionTask,
                new AgentFlowExecutionContext(0, string.Empty, string.Empty, string.Empty, MechanismOperationType.Set));

        public void BeginMechanismExecution(
            Task<Result<bool, RimMindError>> mechanismExecutionTask,
            AgentFlowExecutionContext context)
        {
            _pendingMechanismExecutions.Add(new PendingMechanismExecution(
                mechanismExecutionTask ?? throw new ArgumentNullException(nameof(mechanismExecutionTask)),
                context ?? throw new ArgumentNullException(nameof(context))));
        }

        public bool PollMechanismExecution(out Result<bool, RimMindError>? result, out string? error)
        {
            result = null;
            error = null;
            if (!PollMechanismExecution(out var completion))
                return false;

            result = completion!.Result;
            error = completion.Error;
            return true;
        }

        public bool PollMechanismExecution(out AgentFlowMechanismExecutionCompletion? completion)
        {
            completion = null;
            for (int index = 0; index < _pendingMechanismExecutions.Count; index++)
            {
                PendingMechanismExecution pending = _pendingMechanismExecutions[index];
                if (!pending.Task.IsCompleted)
                    continue;

                _pendingMechanismExecutions.RemoveAt(index);
                completion = CreateCompletion(pending);
                return true;
            }

            return false;
        }

        private static AgentFlowMechanismExecutionCompletion CreateCompletion(PendingMechanismExecution pending)
        {
            Task<Result<bool, RimMindError>> completedTask = pending.Task;
            if (completedTask.IsFaulted)
            {
                return new AgentFlowMechanismExecutionCompletion(
                    pending.Context,
                    null,
                    completedTask.Exception?.GetBaseException().Message ?? "mechanism execution failed");
            }

            if (completedTask.IsCanceled)
                return new AgentFlowMechanismExecutionCompletion(pending.Context, null, "mechanism execution cancelled");

            return new AgentFlowMechanismExecutionCompletion(
                pending.Context,
                completedTask.GetAwaiter().GetResult(),
                null);
        }

        public void ResetContextBuild()
        {
            _contextBuildTask = null;
        }

        private sealed class PendingMechanismExecution
        {
            public PendingMechanismExecution(Task<Result<bool, RimMindError>> task, AgentFlowExecutionContext context)
            {
                Task = task;
                Context = context;
            }

            public Task<Result<bool, RimMindError>> Task { get; }
            public AgentFlowExecutionContext Context { get; }
        }
    }

    internal sealed class AgentFlowExecutionContext
    {
        public AgentFlowExecutionContext(
            int targetGeneration,
            string scope,
            string targetId,
            string mechanismId,
            MechanismOperationType operation)
        {
            TargetGeneration = targetGeneration;
            Scope = scope ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            MechanismId = mechanismId ?? string.Empty;
            Operation = operation;
        }

        public int TargetGeneration { get; }
        public string Scope { get; }
        public string TargetId { get; }
        public string MechanismId { get; }
        public MechanismOperationType Operation { get; }
    }

    internal sealed class AgentFlowMechanismExecutionCompletion
    {
        public AgentFlowMechanismExecutionCompletion(
            AgentFlowExecutionContext context,
            Result<bool, RimMindError>? result,
            string? error)
        {
            Context = context;
            Result = result;
            Error = error;
        }

        public AgentFlowExecutionContext Context { get; }
        public Result<bool, RimMindError>? Result { get; }
        public string? Error { get; }
    }
}
