using System.Collections.Generic;
using RimMind.Domain.Llm;
using Xunit;

namespace RimMind.Tests.Domain.Llm
{
    public class PromptAugmentationTests
    {
        [Fact]
        public void InsertAfterLastSystem_InsertsValidAdditionsInDeterministicOrder()
        {
            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = "first" },
                new() { Role = "user", Content = "question" },
                new() { Role = "SYSTEM", Content = "last" },
                new() { Role = "assistant", Content = "answer" },
            };

            PromptAugmentation.InsertAfterLastSystem(messages, new[]
            {
                new PromptAugmentation("z", "third", 20),
                new PromptAugmentation("b", "second", 10),
                new PromptAugmentation("a", "first", 10),
                new PromptAugmentation("ignored", " ", 0),
            });

            Assert.Collection(messages,
                message => Assert.Equal("first", message.Content),
                message => Assert.Equal("question", message.Content),
                message => Assert.Equal("last", message.Content),
                message => Assert.Equal("first", message.Content),
                message => Assert.Equal("second", message.Content),
                message => Assert.Equal("third", message.Content),
                message => Assert.Equal("answer", message.Content));
        }

        [Fact]
        public void InsertAfterLastSystem_WithoutSystemInsertsAtStart()
        {
            var messages = new List<ChatMessage> { new() { Role = "user", Content = "question" } };

            PromptAugmentation.InsertAfterLastSystem(messages, new[] { new PromptAugmentation("id", "addition", 1) });

            Assert.Equal("addition", messages[0].Content);
            Assert.Equal("question", messages[1].Content);
        }
    }
}
