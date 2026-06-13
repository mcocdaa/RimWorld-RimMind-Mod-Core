using RimMind.Infrastructure.UI;
using Verse;

namespace RimMind.Presentation.Api
{
    public static partial class RimMindAPI
    {
        public static class Debug
        {
            public static void OpenAIRequests()
            {
                Find.WindowStack.Add(Window_RimMindHub.OpenAIRequests());
            }
        }
    }
}
