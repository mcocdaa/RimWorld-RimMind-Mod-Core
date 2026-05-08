namespace RimMind.Contracts.Extensions
{
    public interface IParameterTuner
    {
        string Name { get; }
        float TuneParameter(string parameterName, float currentValue);
        bool ShouldApply(string npcId);
    }
}
