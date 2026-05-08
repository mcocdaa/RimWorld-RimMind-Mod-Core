using RimMind.Kernel.Abstractions;

namespace RimMind.Adapters.Verse
{
    public sealed class VerseLogSink : ILogSink
    {
        public void Message(string msg) => global::Verse.Log.Message(msg);
        public void Warning(string msg) => global::Verse.Log.Warning(msg);
        public void Error(string msg) => global::Verse.Log.Error(msg);
    }
}
