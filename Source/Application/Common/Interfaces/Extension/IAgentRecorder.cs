using RimMind.Application.Common.Models.Agent;

namespace RimMind.Application.Common.Interfaces.Extension
{
    public interface IAgentRecorder
    {
        void RecordBehavior(BehaviorRecordDto record);
    }
}
