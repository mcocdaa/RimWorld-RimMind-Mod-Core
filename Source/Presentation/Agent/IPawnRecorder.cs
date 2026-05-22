using RimMind.Application.Common.Models;

namespace RimMind.Presentation.Agent
{
    public interface IPawnRecorder
    {
        void Record(BehaviorRecord record);
    }
}
