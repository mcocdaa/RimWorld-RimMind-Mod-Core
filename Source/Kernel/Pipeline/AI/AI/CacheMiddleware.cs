using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using RimMind.Contracts.Pipeline;
using RimMind.Core.Pipeline.AI;
using RimMind.Contracts.Client;

namespace RimMind.Core.Pipeline.AI
{
    public sealed class CacheMiddleware : IMiddleware<AIRequestContext>
    {
        public string Id => Name;
        public string Name => nameof(CacheMiddleware);
        public int Order => 3;

        private const int MaxEntries = 100;
        private readonly Dictionary<string, LinkedListNode<CacheEntry>> _cache = new Dictionary<string, LinkedListNode<CacheEntry>>();
        private readonly LinkedList<CacheEntry> _lruList = new LinkedList<CacheEntry>();
        private readonly object _lock = new object();

        private sealed class CacheEntry
        {
            public string Key = null!;
            public AIResponse Response = null!;
        }

        public Task InvokeAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next)
        {
            string cacheKey = ComputeHash(context);

            lock (_lock)
            {
                if (_cache.TryGetValue(cacheKey, out var node))
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                    context.Response = node.Value.Response;
                    context.ShortCircuit("cache_hit");
                    return Task.CompletedTask;
                }
            }

            return ExecuteAndCacheAsync(context, next, cacheKey);
        }

        private async Task ExecuteAndCacheAsync(AIRequestContext context, MiddlewareDelegate<AIRequestContext> next, string cacheKey)
        {
            await next(context).ConfigureAwait(false);

            if (context.Response != null && context.Response.Success)
            {
                lock (_lock)
                {
                    if (_cache.TryGetValue(cacheKey, out var existingNode))
                    {
                        _lruList.Remove(existingNode);
                        _lruList.AddFirst(existingNode);
                        existingNode.Value.Response = context.Response;
                        return;
                    }

                    var entry = new CacheEntry { Key = cacheKey, Response = context.Response };
                    var node = _lruList.AddFirst(entry);
                    _cache[cacheKey] = node;

                    while (_cache.Count > MaxEntries)
                    {
                        var last = _lruList.Last!;
                        _lruList.RemoveLast();
                        _cache.Remove(last.Value.Key);
                    }
                }
            }
        }

        private static string ComputeHash(AIRequestContext context)
        {
            using var sha = SHA256.Create();
            var sb = new StringBuilder();
            sb.Append(context.Request.SystemPrompt);
            sb.Append(context.Request.UserPrompt);
            sb.Append(context.Request.JsonSchema ?? string.Empty);
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
