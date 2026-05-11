using System;
using System.Collections.Concurrent;
using System.Threading;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Abstractions;
using RimMind.Contracts.Result;

namespace RimMind.Kernel.Logging
{
    public static class RimMindLogger
    {
        private static readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        private static readonly ConcurrentQueue<(string level, string message)> _backgroundLogs = new ConcurrentQueue<(string, string)>();

        public static IDisposable BeginTraceScope(string traceId) => TraceContext.BeginScope(traceId);

        public static string? CurrentTraceId => TraceContext.Current;

        private static ILogSink? GetSink() => RimMindServiceLocator.Get<ILogSink>();

        private static void WriteToSink(string level, string message)
        {
            var sink = GetSink();
            if (sink != null)
            {
                switch (level)
                {
                    case "Message": sink.Message(message); break;
                    case "Warning": sink.Warning(message); break;
                    case "Error": sink.Error(message); break;
                }
            }
            else
            {
                Console.WriteLine($"[{level}] {message}");
            }
        }

        public static void Message(string message)
        {
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                WriteToSink("Message", message);
            else
                _backgroundLogs.Enqueue(("Message", message));
        }

        public static void Warning(string message)
        {
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                WriteToSink("Warning", message);
            else
                _backgroundLogs.Enqueue(("Warning", message));
        }

        public static void Error(string message)
        {
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                WriteToSink("Error", message);
            else
                _backgroundLogs.Enqueue(("Error", message));
        }

        public static void FlushBackgroundLogs()
        {
            while (_backgroundLogs.TryDequeue(out var entry))
            {
                WriteToSink(entry.level, entry.message);
            }
        }
    }
}
