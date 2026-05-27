using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RimMind.Application.Features.Context
{
    internal static class AsyncEnumerable
    {
        public static IAsyncEnumerable<T> Empty<T>() => new EmptyAsyncEnumerable<T>();

        private sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new EmptyAsyncEnumerator<T>();
        }

        private sealed class EmptyAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            public T Current => default!;
            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);
            public ValueTask DisposeAsync() => default;
        }
    }
}
