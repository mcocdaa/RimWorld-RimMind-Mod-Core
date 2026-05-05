using System;
using RimMind.Core.Client;

namespace RimMind.Core.Npc
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
