using RimMind.Kernel.Json;
using Xunit;

namespace RimMind.Core.Tests
{
    public class MessageExtractionTests
    {
        [Fact]
        public void JsonRepairHelper_Repair_ReturnsInputUnchanged()
        {
            var json = "{\"reply\":\"hello\"}";
            Assert.Equal(json, JsonRepairHelper.Repair(json));
        }

        [Fact]
        public void JsonRepairHelper_TryRepairTruncatedJson_NullInput_ReturnsNull()
        {
            Assert.Null(JsonRepairHelper.TryRepairTruncatedJson(null));
        }

        [Fact]
        public void JsonRepairHelper_TryRepairTruncatedJson_EmptyInput_ReturnsNull()
        {
            Assert.Null(JsonRepairHelper.TryRepairTruncatedJson(""));
        }

        [Fact]
        public void JsonRepairHelper_TryRepairTruncatedJson_ValidJson_ReturnsNull()
        {
            Assert.Null(JsonRepairHelper.TryRepairTruncatedJson("{\"key\":\"value\"}"));
        }

        [Fact]
        public void JsonRepairHelper_TryRepairTruncatedJson_TrailingComma_Removed()
        {
            var result = JsonRepairHelper.TryRepairTruncatedJson("{\"key\":\"value\",");
            Assert.Equal("{\"key\":\"value\"}", result);
        }

        [Fact]
        public void JsonRepairHelper_TryRepairTruncatedJson_MissingClosingBrace_Added()
        {
            var result = JsonRepairHelper.TryRepairTruncatedJson("{\"key\":\"value\"");
            Assert.Equal("{\"key\":\"value\"}", result);
        }

        [Fact]
        public void JsonRepairHelper_TryRepairTruncatedJson_MissingClosingBracket_Added()
        {
            var result = JsonRepairHelper.TryRepairTruncatedJson("[1,2,3");
            Assert.Equal("[1,2,3]", result);
        }

        [Fact]
        public void JsonRepairHelper_TryRepairTruncatedJson_UnclosedString_Closed()
        {
            var result = JsonRepairHelper.TryRepairTruncatedJson("{\"key\":\"value");
            Assert.Equal("{\"key\":\"value\"}", result);
        }

        [Fact]
        public void JsonRepairHelper_TryRepairTruncatedJson_ComplexTruncation_Repaired()
        {
            var result = JsonRepairHelper.TryRepairTruncatedJson("{\"items\":[1,2");
            Assert.Equal("{\"items\":[1,2]}", result);
        }
    }
}
