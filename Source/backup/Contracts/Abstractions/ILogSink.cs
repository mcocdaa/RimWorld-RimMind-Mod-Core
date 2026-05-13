namespace RimMind.Contracts.Abstractions
{
    public interface ILogSink
    {
        void Message(string msg);
        void Warning(string msg);
        void Error(string msg);
    }
}
