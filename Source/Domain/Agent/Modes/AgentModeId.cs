namespace RimMind.Domain.Agent.Modes;

public readonly record struct AgentModeId(string Value)
{
    public static readonly AgentModeId Reactive = new("reactive");
    public static readonly AgentModeId Proactive = new("proactive");

    public override string ToString() => Value;
}
