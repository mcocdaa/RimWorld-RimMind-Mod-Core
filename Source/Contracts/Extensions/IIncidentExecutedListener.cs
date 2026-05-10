namespace RimMind.Contracts.Extensions;

public interface IIncidentExecutedListener : IExtension
{
    void OnIncidentExecuted();
}
