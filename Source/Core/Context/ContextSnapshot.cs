using System.Collections.Generic;
using RimMind.Core.Client;

namespace RimMind.Core.Context
{
    public class ContextSnapshot
    {
        public string NpcId;
        public string Scenario = "";
        public List<ChatMessage> Messages = new List<ChatMessage>();
        public List<StructuredTool>? Tools;
        public int EstimatedTokens;
        public ContextLayerMeta Meta = new ContextLayerMeta();
        public int MaxTokens = 400;
        public float Temperature = 0.7f;
        public string? CurrentQuery;
    }

    public class ContextLayerMeta
    {
        public int L0Tokens;
        public int L1Tokens;
        public int L2Tokens;
        public int L3Tokens;
        public int L4Tokens;
        public int TotalTokens;
    }
}
