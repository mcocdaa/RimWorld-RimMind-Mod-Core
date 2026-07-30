using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimMind.Application.Common.Models;
using RimMind.Application.Common.Models.Context;
using RimMind.Domain.Enums;
using RimMind.Domain.ValueObjects;
using RimMind.Presentation.Runtime.Services;

namespace RimMind.Infrastructure.UI.AgentFlow
{
    /// <summary>
    /// Owns the pending async work initiated by Agent Flow Lab.
    /// The window polls this coordinator while drawing, so it never waits for an
    /// incomplete context build or mechanism execution on RimWorld's UI thread.
    /// </summary>
    internal sealed class AgentFlowAsyncCoordinator
    {
        public const string StaleCompletionTranslationKey = "RimMind.UI.Lifecycle.StaleCompletion";

        private Task<ContextSnapshot?>? _contextBuildTask;
        private RuntimeGenerationToken? _contextBuildToken;
        private readonly List<PendingMechanismExecution> _pendingMechanismExecutions = new();
        private readonly RuntimeServiceHub _runtimeHub;

        public AgentFlowAsyncCoordinator()
            : this(RuntimeServiceHub.Shared)
        {
        }

        internal AgentFlowAsyncCoordinator(RuntimeServiceHub runtimeHub)
        {
            _runtimeHub = runtimeHub ?? throw new ArgumentNullException(nameof(runtimeHub));
        }

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
            _contextBuildToken = null;
        }

        public void BeginContextBuild(
            Task<ContextSnapshot?> contextBuildTask,
            RuntimeGenerationToken token)
        {
            _contextBuildTask = contextBuildTask ?? throw new ArgumentNullException(nameof(contextBuildTask));
            _contextBuildToken = token;
        }

        public bool PollContextBuild(out ContextSnapshot? snapshot, out string? error)
        {
            snapshot = null;
            error = null;
            if (_contextBuildTask == null || !_contextBuildTask.IsCompleted)
                return false;

            Task<ContextSnapshot?> completedTask = _contextBuildTask;
            _contextBuildTask = null;
            RuntimeGenerationToken? token = _contextBuildToken;
            _contextBuildToken = null;
            if (token.HasValue && !_runtimeHub.IsCurrent(token.Value))
            {
                _runtimeHub.RecordStaleCompletion(LifecycleEventSources.AgentFlow);
                error = StaleCompletionTranslationKey;
                return true;
            }
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
                new AgentFlowExecutionContext(0, string.Empty, string.Empty, string.Empty, MechanismOperationType.Set),
                token: null);

        public void BeginMechanismExecution(
            Task<Result<bool, RimMindError>> mechanismExecutionTask,
            AgentFlowExecutionContext context)
            => BeginMechanismExecution(mechanismExecutionTask, context, token: null);

        public void BeginMechanismExecution(
            Task<Result<bool, RimMindError>> mechanismExecutionTask,
            AgentFlowExecutionContext context,
            RuntimeGenerationToken token)
            => BeginMechanismExecution(mechanismExecutionTask, context, (RuntimeGenerationToken?)token);

        private void BeginMechanismExecution(
            Task<Result<bool, RimMindError>> mechanismExecutionTask,
            AgentFlowExecutionContext context,
            RuntimeGenerationToken? token)
        {
            _pendingMechanismExecutions.Add(new PendingMechanismExecution(
                mechanismExecutionTask ?? throw new ArgumentNullException(nameof(mechanismExecutionTask)),
                context ?? throw new ArgumentNullException(nameof(context)),
                token));
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
                if (pending.Token.HasValue && !_runtimeHub.IsCurrent(pending.Token.Value))
                {
                    _runtimeHub.RecordStaleCompletion(LifecycleEventSources.AgentFlow);
                    completion = new AgentFlowMechanismExecutionCompletion(
                        pending.Context,
                        null,
                        StaleCompletionTranslationKey);
                    return true;
                }
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
            _contextBuildToken = null;
        }

        private sealed class PendingMechanismExecution
        {
            public PendingMechanismExecution(
                Task<Result<bool, RimMindError>> task,
                AgentFlowExecutionContext context,
                RuntimeGenerationToken? token)
            {
                Task = task;
                Context = context;
                Token = token;
            }

            public Task<Result<bool, RimMindError>> Task { get; }
            public AgentFlowExecutionContext Context { get; }
            public RuntimeGenerationToken? Token { get; }
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
