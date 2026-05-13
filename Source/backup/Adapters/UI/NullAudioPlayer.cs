using RimMind.Contracts.UI;
using Verse;

namespace RimMind.Adapters.UI
{
    internal sealed class NullAudioPlayer : IAudioPlayer
    {
        public void PlayAudio(string audioUrl)
        {
            Log.Message($"[RimMind-Core] NullAudioPlayer: audio playback skipped for {audioUrl}");
        }
    }
}
