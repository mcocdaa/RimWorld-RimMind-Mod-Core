namespace RimMind.Contracts.Extensions;

public interface IModCooldown : IExtension
{
    int CooldownTicks { get; }
}
