using System;
using RimMind.Domain.Exceptions;

namespace RimMind.Presentation.Runtime
{
    public static class TransientExceptionChecker
    {
        public static bool IsTransient(Exception ex)
        {
            if (ex == null) return false;
            if (ex is QuotaExceededException) return true;
            if (ex is TimeoutException) return true;
            if (ex is System.Net.Http.HttpRequestException) return true;
            if (ex is OperationCanceledException) return false;
            if (ex.Message != null)
            {
                string msg = ex.Message.ToLowerInvariant();
                if (msg.Contains("timeout") || msg.Contains("rate limit") ||
                    msg.Contains("too many requests") || msg.Contains("service unavailable") ||
                    msg.Contains("internal server error") || msg.Contains("bad gateway") ||
                    msg.Contains("gateway timeout") || msg.Contains("connection reset"))
                    return true;
            }
            if (ex.InnerException != null)
                return IsTransient(ex.InnerException);
            return false;
        }

        public static int GetRetryDelayMs(int attempt)
        {
            int baseDelay = 1000;
            int maxDelay = 30000;
            int delay = baseDelay * (int)Math.Pow(2, attempt);
            return Math.Min(delay, maxDelay);
        }
    }
}
