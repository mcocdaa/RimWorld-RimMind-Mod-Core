namespace RimMind.Contracts.Extension;

public interface IModCooldown : IExtension
{
    int CooldownTicks { get; }
}
