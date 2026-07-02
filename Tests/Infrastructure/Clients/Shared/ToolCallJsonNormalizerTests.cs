using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimMind.Infrastructure.Services.Clients.Shared;
using Xunit;

namespace RimMind.Tests.Infrastructure.Clients.Shared
{
    public class ToolCallJsonNormalizerTests
    {
        [Fact]
        public void Normalize_NullCollection_ReturnsNull()
        {
            string? result = ToolCallJsonNormalizer.Normalize(null);
            Assert.Null(result);
        }

        [Fact]
        public void Normalize_EmptyCollection_ReturnsNull()
        {
            string? result = ToolCallJsonNormalizer.Normalize(new List<ToolCallEntry>());
            Assert.Null(result);
        }

        [Fact]
        public void Normalize_SingleEntry_ReturnsValidJson()
        {
            var entries = new List<ToolCallEntry>
            {
                new ToolCallEntry
                {
                    Id = "call_001",
                    Type = "function",
                    FunctionName = "get_weather",
                    FunctionArguments = "{\"city\":\"Tokyo\"}",
                }
            };

            string? result = ToolCallJsonNormalizer.Normalize(entries);
            Assert.NotNull(result);

            var parsed = JArray.Parse(result!);
            Assert.Single(parsed);
            Assert.Equal("call_001", parsed[0]["id"]!.ToString());
            Assert.Equal("function", parsed[0]["type"]!.ToString());
            Assert.Equal("get_weather", parsed[0]["function"]!["name"]!.ToString());
            Assert.Equal("{\"city\":\"Tokyo\"}", parsed[0]["function"]!["arguments"]!.ToString());
        }

        [Fact]
        public void Normalize_MultipleEntries_ReturnsAllInArray()
        {
            var entries = new List<ToolCallEntry>
            {
                new ToolCallEntry { Id = "call_001", Type = "function", FunctionName = "search", FunctionArguments = "{}" },
                new ToolCallEntry { Id = "call_002", Type = "function", FunctionName = "reply", FunctionArguments = "{\"msg\":\"hi\"}" },
            };

            string? result = ToolCallJsonNormalizer.Normalize(entries);
            Assert.NotNull(result);

            var parsed = JArray.Parse(result!);
            Assert.Equal(2, parsed.Count);
        }

        [Fact]
        public void Normalize_NullFunctionFields_RendersNullInJson()
        {
            var entries = new List<ToolCallEntry>
            {
                new ToolCallEntry
                {
                    Id = "call_001",
                    Type = "function",
                    FunctionName = null,
                    FunctionArguments = null,
                }
            };

            string? result = ToolCallJsonNormalizer.Normalize(entries);
            Assert.NotNull(result);

            var parsed = JArray.Parse(result!);
            Assert.Equal(JTokenType.Null, parsed[0]["function"]!["name"]!.Type);
            Assert.Equal(JTokenType.Null, parsed[0]["function"]!["arguments"]!.Type);
        }

        [Fact]
        public void Normalize_ProducesConsistentFormat()
        {
            var entries = new List<ToolCallEntry>
            {
                new ToolCallEntry { Id = "c1", Type = "function", FunctionName = "f", FunctionArguments = "a" },
            };

            string? result = ToolCallJsonNormalizer.Normalize(entries);
            Assert.NotNull(result);
            // The format must match: [{"id":...,"type":...,"function":{"name":...,"arguments":...}}]
            Assert.Contains("\"id\":\"c1\"", result);
            Assert.Contains("\"type\":\"function\"", result);
            Assert.Contains("\"function\":", result);
            Assert.Contains("\"name\":\"f\"", result);
            Assert.Contains("\"arguments\":\"a\"", result);
        }
    }
}
