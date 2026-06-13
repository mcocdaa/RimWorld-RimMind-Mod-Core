using RimMind.Presentation.Runtime;

namespace RimMind.Presentation.Api
{
    public static partial class RimMindAPI
    {
        public static class Debug
        {
            public static void OpenAIRequests()
            {
                RimMindRuntime.Instance.WindowService?.OpenAIRequests();
            }
        }
    }
}
