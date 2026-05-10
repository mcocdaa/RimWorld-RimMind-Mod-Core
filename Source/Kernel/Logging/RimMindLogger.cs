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
        private const string Prefix = "[RimMind-Core]";
        private static readonly int _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        private static readonly ConcurrentQueue<(string level, string message)> _backgroundLogs = new ConcurrentQueue<(string, string)>();

        public static IDisposable BeginTraceScope(string traceId) => TraceContext.BeginScope(traceId);

        public static string? CurrentTraceId => TraceContext.Current;

        private static string FormatPrefix()
        {
            var trace = TraceContext.Current;
            return trace != null ? $"{Prefix}[trace={trace}]" : Prefix;
        }

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
            string formatted = $"{FormatPrefix()} {message}";
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                WriteToSink("Message", formatted);
            else
                _backgroundLogs.Enqueue(("Message", formatted));
        }

        public static void Warning(string message)
        {
            string formatted = $"{FormatPrefix()} {message}";
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                WriteToSink("Warning", formatted);
            else
                _backgroundLogs.Enqueue(("Warning", formatted));
        }

        public static void Error(string message)
        {
            string formatted = $"{FormatPrefix()} {message}";
            if (Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                WriteToSink("Error", formatted);
            else
                _backgroundLogs.Enqueue(("Error", formatted));
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
