namespace RimMind.Contracts.Extension;

public interface IToggleBehavior : IExtension
{
    bool IsActive { get; }
    void Toggle();
}
