namespace RimMind.Kernel.Abstractions
{
    public interface IThreadChecker
    {
        bool IsMainThread { get; }
    }
}
