namespace RimMind.Contracts.Abstractions
{
    public interface IThreadChecker
    {
        bool IsMainThread { get; }
    }
}
