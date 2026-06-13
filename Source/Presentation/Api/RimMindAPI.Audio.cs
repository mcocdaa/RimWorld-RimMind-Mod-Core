using RimMind.Application.Common.Interfaces.UI;
using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Api
{
    public static partial class RimMindAPI
    {
        public static class Audio
        {
            public static IAudioPlayer AudioPlayer => RimMindRuntime.Instance.AudioPlayer;
        }
    }
}
