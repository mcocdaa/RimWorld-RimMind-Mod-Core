using System;
using System.Threading;
using RimMind.Presentation.Runtime.Services;
using Xunit;

namespace RimMind.Tests.Presentation.Runtime
{
    public sealed class RequestCancellationRegistrationsTests
    {
        [Fact]
        public void TryCreate_DisposedCallerRegistrationThrow_DisposesRuntimeRegistration()
        {
            using var runtimeSource = new CancellationTokenSource();
            var callerSource = new CancellationTokenSource();
            callerSource.Dispose();
            int cancellationCallbacks = 0;

            bool created = RequestCancellationRegistrations.TryCreate(
                callback => runtimeSource.Token.Register(callback),
                _ => throw new ObjectDisposedException(nameof(callerSource)),
                () => Interlocked.Increment(ref cancellationCallbacks),
                out RequestCancellationRegistrations? registrations,
                out Exception? setupFailure);

            Assert.False(created);
            Assert.Null(registrations);
            Assert.IsType<ObjectDisposedException>(setupFailure);

            runtimeSource.Cancel();
            Assert.Equal(0, Volatile.Read(ref cancellationCallbacks));
        }
    }
}
