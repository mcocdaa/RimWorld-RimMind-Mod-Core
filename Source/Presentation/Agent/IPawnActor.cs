using Verse.AI;

namespace RimMind.Presentation.Agent
{
    public interface IPawnActor
    {
        void Tick();
        Job? ConsumePendingJob();
        void SetPendingJob(Job job);
    }
}
