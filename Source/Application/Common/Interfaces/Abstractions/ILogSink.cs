namespace RimMind.Application.Common.Interfaces.Abstractions
{
    public interface ILogSink
    {
        void Message(string msg);
        void Warning(string msg);
        void Error(string msg);
    }
}
