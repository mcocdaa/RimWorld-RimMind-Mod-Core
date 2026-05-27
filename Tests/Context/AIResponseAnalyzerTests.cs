using RimMind.Application.Features.Context;
using Xunit;

namespace RimMind.Tests.Context
{
    public class AIResponseAnalyzerTests
    {
        [Fact]
        public void SubstringMatch_ReturnsUsedKey()
        {
            var analyzer = new AIResponseAnalyzer();
            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "The pawn is currently in a bad mood and needs food urgently.",
                LayerTag = "mood_status"
            });

            var response = new LlmResponse
            {
                Content = "Based on the context, The pawn is currently in a bad mood and needs food urgently. I suggest cooking.",
                State = AIRequestState.Completed
            };

            var used = analyzer.ExtractUsedKeys(snapshot, response);
            Assert.Contains("mood_status", used);
        }

        [Fact]
        public void NoMatch_ReturnsEmptyList()
        {
            var analyzer = new AIResponseAnalyzer();
            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "The weather is sunny and the crops are growing well today.",
                LayerTag = "weather_report"
            });

            var response = new LlmResponse
            {
                Content = "I will focus on building defenses around the colony.",
                State = AIRequestState.Completed
            };

            var used = analyzer.ExtractUsedKeys(snapshot, response);
            Assert.Empty(used);
        }

        [Fact]
        public void ShortContentSkipped_NotAnalyzed()
        {
            var analyzer = new AIResponseAnalyzer();
            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "Short",  // Less than MinSampleLength (20)
                LayerTag = "short_key"
            });

            var response = new LlmResponse
            {
                Content = "Short",
                State = AIRequestState.Completed
            };

            var used = analyzer.ExtractUsedKeys(snapshot, response);
            Assert.Empty(used);
        }

        [Fact]
        public void NullSnapshot_ReturnsEmptyList()
        {
            var analyzer = new AIResponseAnalyzer();
            var response = new LlmResponse { Content = "test", State = AIRequestState.Completed };

            var used = analyzer.ExtractUsedKeys(null!, response);
            Assert.Empty(used);
        }

        [Fact]
        public void NullResponse_ReturnsEmptyList()
        {
            var analyzer = new AIResponseAnalyzer();
            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "Some long enough content for analysis purposes here.",
                LayerTag = "test_key"
            });

            var used = analyzer.ExtractUsedKeys(snapshot, null!);
            Assert.Empty(used);
        }

        [Fact]
        public void EmptyLayerTag_NotIncluded()
        {
            var analyzer = new AIResponseAnalyzer();
            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "This is a sufficiently long content string for testing analysis.",
                LayerTag = null!
            });

            var response = new LlmResponse
            {
                Content = "This is a sufficiently long content string for testing analysis.",
                State = AIRequestState.Completed
            };

            var used = analyzer.ExtractUsedKeys(snapshot, response);
            Assert.Empty(used);
        }

        [Fact]
        public void ToolCallsJson_AlsoSearched()
        {
            var analyzer = new AIResponseAnalyzer();
            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "The colonist has a skill level of 15 in crafting and is very productive.",
                LayerTag = "skill_info"
            });

            var response = new LlmResponse
            {
                Content = "I will assign work.",
                ToolCallsJson = "The colonist has a skill level of 15 in crafting and is very productive.",
                State = AIRequestState.Completed
            };

            var used = analyzer.ExtractUsedKeys(snapshot, response);
            Assert.Contains("skill_info", used);
        }

        [Fact]
        public void MultipleMessages_MultipleMatches()
        {
            var analyzer = new AIResponseAnalyzer();
            var snapshot = new ContextSnapshot { NpcId = "npc-1" };
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "The temperature outside is freezing and the colonists need warm clothes.",
                LayerTag = "weather"
            });
            snapshot.AddMessage(new ChatMessage
            {
                Role = "system",
                Content = "There are three raiders approaching from the north side of the map.",
                LayerTag = "threat"
            });

            var response = new LlmResponse
            {
                Content = "The temperature outside is freezing and the colonists need warm clothes. Also, There are three raiders approaching from the north side of the map.",
                State = AIRequestState.Completed
            };

            var used = analyzer.ExtractUsedKeys(snapshot, response);
            Assert.Equal(2, used.Count);
            Assert.Contains("weather", used);
            Assert.Contains("threat", used);
        }
    }
}
