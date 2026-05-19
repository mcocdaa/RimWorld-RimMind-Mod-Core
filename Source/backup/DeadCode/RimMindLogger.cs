using System;
using RimMind.Application.Common.Interfaces.Abstractions;

namespace RimMind.Application.Features.Logging
{
    internal sealed class RimMindLogger
    {
        private readonly ILogSink? _sink;
        private readonly string _prefix;

        public RimMindLogger(ILogSink? sink, string prefix = "RimMind")
        {
            _sink = sink;
            _prefix = prefix;
        }

        public void Info(string message)
        {
            _sink?.Message($"[{_prefix}] {message}");
        }

        public void Warning(string message)
        {
            _sink?.Warning($"[{_prefix}] {message}");
        }

        public void Error(string message)
        {
            _sink?.Error($"[{_prefix}] {message}");
        }

        public void Error(Exception ex, string message)
        {
            _sink?.Error($"[{_prefix}] {message}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
