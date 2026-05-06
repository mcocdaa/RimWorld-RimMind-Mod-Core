using RimMind.Kernel.Abstractions;

namespace RimMind.Adapters.Verse
{
    public sealed class VerseLogSink : ILogSink
    {
        public void Message(string msg) => Verse.Log.Message(msg);
        public void Warning(string msg) => Verse.Log.Warning(msg);
        public void Error(string msg) => Verse.Log.Error(msg);
    }
}
