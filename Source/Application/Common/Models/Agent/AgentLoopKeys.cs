namespace RimMind.Application.Common.Models.Agent
{
    public static class AgentLoopKeys
    {
        public static string ForPawn(int id) => $"pawn:{id}";

        public static string ForScoped(string compositeKey) => $"scope:{compositeKey}";
    }
}
