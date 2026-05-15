// Adapted for .NET Framework 4.8 compatibility — provides async-streams types
// that are not included in net48's mscorlib but are required by C# 8+ compilers
// for the `async IAsyncEnumerable` feature.
//
// NOTE: IAsyncEnumerable<T>, IAsyncEnumerator<T>, and EnumeratorCancellationAttribute
// are already present in mscorlib for this build environment and MUST NOT be
// redefined here to avoid CS0433 conflicts.
//
// DO NOT add Microsoft.Bcl.AsyncInterfaces NuGet package — it conflicts with
// mscorlib's definition.

#pragma warning disable CS0436 // Type conflicts with imported type

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// ---------- System namespace types ----------

namespace System
{
    /// <summary>
    /// Enables asynchronous disposal of unmanaged resources.
    /// </summary>
    public interface IAsyncDisposable
    {
        ValueTask DisposeAsync();
    }
}

// ---------- System.Threading.Tasks namespace types ----------

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides a value type that wraps a <see cref="Task"/>.
    /// </summary>
    public readonly struct ValueTask : IEquatable<ValueTask>
    {
        private readonly Task? _task;

        public ValueTask(Task task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
        }

        public Task AsTask() => _task ?? Task.CompletedTask;

        public ValueTaskAwaiter GetAwaiter() => new ValueTaskAwaiter(AsTask());

        public bool Equals(ValueTask other) => Equals(_task, other._task);

        public override bool Equals(object? obj) => obj is ValueTask vt && Equals(vt);

        public override int GetHashCode() => _task?.GetHashCode() ?? 0;

        public static bool operator ==(ValueTask left, ValueTask right) => left.Equals(right);

        public static bool operator !=(ValueTask left, ValueTask right) => !left.Equals(right);

        public ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredValueTaskAwaitable(AsTask(), continueOnCapturedContext);
    }

    /// <summary>
    /// Provides a value type that wraps a <see cref="T:Task{TResult}"/>.
    /// </summary>
    public readonly struct ValueTask<TResult> : IEquatable<ValueTask<TResult>>
    {
        private readonly Task<TResult>? _task;

        public ValueTask(Task<TResult> task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
        }

        public Task<TResult> AsTask() => _task ?? Task.FromResult(default(TResult)!);

        public ValueTaskAwaiter<TResult> GetAwaiter() => new ValueTaskAwaiter<TResult>(AsTask());

        public bool Equals(ValueTask<TResult> other) => Equals(_task, other._task);

        public override bool Equals(object? obj) => obj is ValueTask<TResult> vt && Equals(vt);

        public override int GetHashCode() => _task?.GetHashCode() ?? 0;

        public static bool operator ==(ValueTask<TResult> left, ValueTask<TResult> right) => left.Equals(right);

        public static bool operator !=(ValueTask<TResult> left, ValueTask<TResult> right) => !left.Equals(right);

        public ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredValueTaskAwaitable<TResult>(AsTask(), continueOnCapturedContext);
    }
}

// ---------- System.Runtime.CompilerServices namespace types ----------

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Builder for async iterators (async IAsyncEnumerable methods).
    /// Required by the C# compiler to generate async-iterator state machines.
    /// </summary>
    public struct AsyncIteratorMethodBuilder
    {
        private AsyncTaskMethodBuilder<int> _builder;

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            _builder.Start(ref stateMachine);
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            _builder.SetStateMachine(stateMachine);
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
        }

        public void MoveNext<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
        }

        public Task<bool> MoveNextAsync()
        {
            return Task.FromResult(true);
        }

        public void Complete()
        {
        }

        public ValueTask<bool> AwaitMoveNextAsync()
        {
            return new ValueTask<bool>(Task.FromResult(true));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }

    /// <summary>
    /// Provides an awaiter for a <see cref="ValueTask"/>.
    /// </summary>
    public struct ValueTaskAwaiter : ICriticalNotifyCompletion
    {
        private readonly Task _task;

        public ValueTaskAwaiter(Task task)
        {
            _task = task;
        }

        public bool IsCompleted => _task.IsCompleted;

        public void GetResult() => _task.GetAwaiter().GetResult();

        public void OnCompleted(Action continuation) => _task.GetAwaiter().OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation) =>
            ((ICriticalNotifyCompletion)_task.GetAwaiter()).UnsafeOnCompleted(continuation);
    }

    /// <summary>
    /// Provides an awaiter for a <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public struct ValueTaskAwaiter<TResult> : ICriticalNotifyCompletion
    {
        private readonly Task<TResult> _task;

        public ValueTaskAwaiter(Task<TResult> task)
        {
            _task = task;
        }

        public bool IsCompleted => _task.IsCompleted;

        public TResult GetResult() => _task.GetAwaiter().GetResult();

        public void OnCompleted(Action continuation) => _task.GetAwaiter().OnCompleted(continuation);

        public void UnsafeOnCompleted(Action continuation) =>
            ((ICriticalNotifyCompletion)_task.GetAwaiter()).UnsafeOnCompleted(continuation);
    }

    /// <summary>
    /// Provides an awaitable that enables configured awaits on a <see cref="ValueTask"/>.
    /// </summary>
    public struct ConfiguredValueTaskAwaitable
    {
        private readonly ConfiguredTaskAwaitable _configured;

        public ConfiguredValueTaskAwaitable(Task task, bool continueOnCapturedContext)
        {
            _configured = task.ConfigureAwait(continueOnCapturedContext);
        }

        public ConfiguredValueTaskAwaiter GetAwaiter() => new ConfiguredValueTaskAwaiter(_configured.GetAwaiter());

        public struct ConfiguredValueTaskAwaiter : ICriticalNotifyCompletion
        {
            private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _awaiter;

            public ConfiguredValueTaskAwaiter(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
            {
                _awaiter = awaiter;
            }

            public bool IsCompleted => _awaiter.IsCompleted;

            public void GetResult() => _awaiter.GetResult();

            public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);

            public void UnsafeOnCompleted(Action continuation) => _awaiter.UnsafeOnCompleted(continuation);
        }
    }

    /// <summary>
    /// Provides an awaitable that enables configured awaits on a <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public struct ConfiguredValueTaskAwaitable<TResult>
    {
        private readonly ConfiguredTaskAwaitable<TResult> _configured;

        public ConfiguredValueTaskAwaitable(Task<TResult> task, bool continueOnCapturedContext)
        {
            _configured = task.ConfigureAwait(continueOnCapturedContext);
        }

        public ConfiguredValueTaskAwaiter GetAwaiter() => new ConfiguredValueTaskAwaiter(_configured.GetAwaiter());

        public struct ConfiguredValueTaskAwaiter : ICriticalNotifyCompletion
        {
            private readonly ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter _awaiter;

            public ConfiguredValueTaskAwaiter(ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter)
            {
                _awaiter = awaiter;
            }

            public bool IsCompleted => _awaiter.IsCompleted;

            public TResult GetResult() => _awaiter.GetResult();

            public void OnCompleted(Action continuation) => _awaiter.OnCompleted(continuation);

            public void UnsafeOnCompleted(Action continuation) => _awaiter.UnsafeOnCompleted(continuation);
        }
    }
}
