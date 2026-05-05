using System;
using System.Collections.Concurrent;
using System.Threading;
using Verse;

namespace RimMind.Core
{
    public static class RimMindLogger
    {
        private const string Prefix = "[RimMind-Core]";
        private static readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        private static readonly ConcurrentQueue<(string level, string message)> _backgroundLogs = new ConcurrentQueue<(string, string)>();

        public static void Message(string message)
        {
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                Log.Message($"{Prefix} {message}");
            else
                _backgroundLogs.Enqueue(("Message", $"{Prefix} {message}"));
        }

        public static void Warning(string message)
        {
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                Log.Warning($"{Prefix} {message}");
            else
                _backgroundLogs.Enqueue(("Warning", $"{Prefix} {message}"));
        }

        public static void Error(string message)
        {
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                Log.Error($"{Prefix} {message}");
            else
                _backgroundLogs.Enqueue(("Error", $"{Prefix} {message}"));
        }

        public static void FlushBackgroundLogs()
        {
            while (_backgroundLogs.TryDequeue(out var entry))
            {
                switch (entry.level)
                {
                    case "Message": Log.Message(entry.message); break;
                    case "Warning": Log.Warning(entry.message); break;
                    case "Error": Log.Error(entry.message); break;
                }
            }
        }
    }
}
