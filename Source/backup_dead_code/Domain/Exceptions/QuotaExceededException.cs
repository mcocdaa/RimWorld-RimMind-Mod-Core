using System.Net;

namespace RimMind.Domain.Exceptions
{
    public static class QuotaExceededException
    {
        private const int HttpTooManyRequests = 429;

        public static bool IsQuotaError(string? error)
        {
            if (string.IsNullOrEmpty(error)) return false;
            var lower = error!.ToLowerInvariant();
            return lower.Contains("quota") || lower.Contains("rate_limit") || lower.Contains("429")
                || lower.Contains("resource_exhausted") || lower.Contains("capacity");
        }

        public static bool IsQuotaHttpStatusCode(long statusCode)
        {
            return statusCode == HttpTooManyRequests
                || statusCode == (long)HttpStatusCode.ServiceUnavailable;
        }
    }
}
