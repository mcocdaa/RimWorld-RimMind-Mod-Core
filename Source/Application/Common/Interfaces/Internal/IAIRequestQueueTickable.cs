using System;

namespace RimMind.Application.Common.Interfaces.Internal
{
    public interface IAIRequestQueueTickable : IAIRequestQueue
    {
        int CurrentTick { get; set; }
        Action<string, bool>? LogHandler { get; set; }
        Action? FlushBackgroundQueue { get; set; }
        void Tick();
        void Reset();
    }
}
