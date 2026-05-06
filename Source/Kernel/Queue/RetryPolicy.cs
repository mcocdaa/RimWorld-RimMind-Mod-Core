namespace RimMind.Kernel.Queue
{
    public static class RetryPolicy
    {
        public static bool IsTransient(string error)
        {
            if (string.IsNullOrEmpty(error)) return false;
            if (RimMind.Core.Client.QuotaExceededException.IsQuotaError(error)) return false;
            string lower = error.ToLowerInvariant();
            return lower.Contains("timeout")
                || lower.Contains("connection")
                || lower.Contains("network")
                || lower.Contains("503")
                || lower.Contains("502")
                || lower.Contains("429")
                || lower.Contains("rate limit");
        }
    }
}
