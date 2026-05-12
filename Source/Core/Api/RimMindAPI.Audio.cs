using RimMind.Contracts.UI;
using RimMind.Core.Runtime;

namespace RimMind.Core
{
    public static partial class RimMindAPI
    {
        public static class Audio
        {
            public static IAudioPlayer AudioPlayer => RimMindRuntime.Instance.AudioPlayer;
        }
    }
}
