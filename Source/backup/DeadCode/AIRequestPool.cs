using System;
using System.Collections.Concurrent;
using System.Threading;
using RimMind.Application.Common.Interfaces.Client;
using RimMind.Application.Common.Models.Client;
using RimMind.Presentation.Pipeline.AI;
using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Runtime
{
    public class AIRequestPool
    {
        private readonly ConcurrentBag<AIRequestContext> _pool = new ConcurrentBag<AIRequestContext>();
        private int _created;

        public AIRequestContext Rent(IAIClient client)
        {
            if (_pool.TryTake(out var ctx))
            {
                ctx.Reset(client);
                return ctx;
            }
            Interlocked.Increment(ref _created);
            return new AIRequestContext(client);
        }

        public void Return(AIRequestContext ctx)
        {
            if (ctx == null) return;
            ctx.Clear();
            _pool.Add(ctx);
        }

        public int PoolSize => _pool.Count;
        public int TotalCreated => _created;
    }
}
