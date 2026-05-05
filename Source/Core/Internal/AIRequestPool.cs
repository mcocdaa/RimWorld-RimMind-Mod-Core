using System.Collections.Concurrent;
using RimMind.Core.Client;

namespace RimMind.Core.Internal
{
    internal static class AIRequestPool
    {
        private static readonly ConcurrentBag<AIRequest> _pool = new ConcurrentBag<AIRequest>();
        private const int MaxPoolSize = 64;

        public static AIRequest Rent()
            => _pool.TryTake(out var req) ? req : new AIRequest();

        public static void Return(AIRequest req)
        {
            if (_pool.Count < MaxPoolSize)
            {
                req.Reset();
                _pool.Add(req);
            }
        }

        internal static int PoolCount => _pool.Count;
    }
}
