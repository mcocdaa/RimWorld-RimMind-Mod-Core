// mirrors RimMind.Actions.RiskLevel — Core cannot reference Actions module (no dependency)
// keep in sync manually if Actions.RiskLevel changes
namespace RimMind.Core.Agent
{
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }
}
