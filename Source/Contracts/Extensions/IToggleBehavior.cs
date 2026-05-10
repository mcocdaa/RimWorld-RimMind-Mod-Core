namespace RimMind.Contracts.Extensions;

public interface IToggleBehavior : IExtension
{
    bool IsActive { get; }
    void Toggle();
}
