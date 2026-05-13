using System;
using RimMind.Contracts.Client;
using RimMind.Adapters.Client;

namespace RimMind.Core.Runtime
{
    internal static class TransientExceptionChecker
    {
        public static bool IsTransient(Exception ex)
        {
            if (ex is TimeoutException) return true;
            if (ex is HttpHelper.HttpException httpEx && httpEx.StatusCode >= 500 && httpEx.StatusCode < 600) return true;
            return false;
        }
    }
}
