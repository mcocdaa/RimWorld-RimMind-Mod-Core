using RimMind.Contracts.Extensions;

namespace RimMind.Kernel.Flywheel
{
    public class FlywheelBuiltinTuner : IParameterTuner
    {
        public string Name => "FlywheelBuiltin";
        public string TunerId => "flywheel_builtin";

        public float TuneParameter(string parameterName, float currentValue)
        {
            return currentValue;
        }

        public bool ShouldApply(string npcId)
        {
            return true;
        }

        public void Tune(object config)
        {
        }
    }
}
