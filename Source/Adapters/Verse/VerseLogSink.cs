using RimMind.Contracts.Abstractions;
using RimMind.Contracts.Internal;
using RimMind.Contracts.Result;

namespace RimMind.Adapters.Verse
{
    public sealed class VerseLogSink : ILogSink
    {
        private const string Prefix = "[RimMind-Core]";

        private static string Format(string msg)
        {
            var trace = TraceContext.Current;
            return trace != null ? $"{Prefix}[trace={trace}] {msg}" : $"{Prefix} {msg}";
        }

        public void Message(string msg) => global::Verse.Log.Message(Format(msg));
        public void Warning(string msg) => global::Verse.Log.Warning(Format(msg));
        public void Error(string msg) => global::Verse.Log.Error(Format(msg));
    }
}
