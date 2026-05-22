namespace RimMind.Application.Common.Models.Context
{
    public class ContextRequest
    {
        public string NpcId = null!;
        public string Scenario = ScenarioIds.Dialogue;
        public float Budget = 0;
        public string? CurrentQuery;
        public string[]? ExcludeKeys;
        public int MaxTokens = RimMindDefaults.MaxTokens;
        public float Temperature = 0.7f;
        public object? Map;
        public string? SpeakerName;
        public int MaxRounds;
        public bool IsMonologue;
    }
}
