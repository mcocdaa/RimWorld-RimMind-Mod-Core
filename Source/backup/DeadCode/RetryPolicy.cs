using System;

namespace RimMind.Application.Features.Queue
{
    internal sealed class RetryPolicy
    {
        public int MaxRetries { get; }
        public TimeSpan BaseDelay { get; }
        public float BackoffMultiplier { get; }
        public TimeSpan MaxDelay { get; }

        public RetryPolicy(int maxRetries = 3, TimeSpan? baseDelay = null, float backoffMultiplier = 2f, TimeSpan? maxDelay = null)
        {
            MaxRetries = maxRetries;
            BaseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
            BackoffMultiplier = backoffMultiplier;
            MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        }

        public TimeSpan GetDelay(int attempt)
        {
            var delay = TimeSpan.FromTicks((long)(BaseDelay.Ticks * Math.Pow(BackoffMultiplier, attempt)));
            return delay > MaxDelay ? MaxDelay : delay;
        }

        public bool ShouldRetry(int attempt, long httpStatusCode)
        {
            if (attempt >= MaxRetries) return false;
            if (httpStatusCode == 429) return true;
            if (httpStatusCode >= 500) return true;
            return false;
        }
    }
}
