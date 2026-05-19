namespace RimMind.Domain.Agent.Modes;

public readonly record struct AgentModeId(string Value)
{
    public static readonly AgentModeId Reactive = new("rimmind.reactive");
    public static readonly AgentModeId Proactive = new("rimmind.proactive");

    public override string ToString() => Value;

    public static AgentModeId Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return default;
        var lower = raw.ToLowerInvariant();
        return lower switch
        {
            "reactive" => Reactive,
            "proactive" => Proactive,
            _ => new AgentModeId(lower.Contains(".") ? lower : $"rimmind.{lower}")
        };
    }
}
