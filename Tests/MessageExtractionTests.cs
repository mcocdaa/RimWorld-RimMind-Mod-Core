using RimMind.Application.Features.Json;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class MessageExtractionTests
    {
        [Fact]
        public void JsonRepairer_Repair_ReturnsInputUnchanged()
        {
            var json = "{\"reply\":\"hello\"}";
            Assert.Equal(json, JsonRepairer.Repair(json));
        }

        [Fact]
        public void JsonRepairer_TryRepairTruncatedJson_NullInput_ReturnsNull()
        {
            Assert.Null(JsonRepairer.TryRepairTruncatedJson(null!));
        }

        [Fact]
        public void JsonRepairer_TryRepairTruncatedJson_EmptyInput_ReturnsNull()
        {
            Assert.Null(JsonRepairer.TryRepairTruncatedJson(""));
        }

        [Fact]
        public void JsonRepairer_TryRepairTruncatedJson_ValidJson_ReturnsNull()
        {
            Assert.Null(JsonRepairer.TryRepairTruncatedJson("{\"key\":\"value\"}"));
        }

        [Fact]
        public void JsonRepairer_TryRepairTruncatedJson_TrailingComma_Removed()
        {
            var result = JsonRepairer.TryRepairTruncatedJson("{\"key\":\"value\",");
            Assert.Equal("{\"key\":\"value\"}", result);
        }

        [Fact]
        public void JsonRepairer_TryRepairTruncatedJson_MissingClosingBrace_Added()
        {
            var result = JsonRepairer.TryRepairTruncatedJson("{\"key\":\"value\"");
            Assert.Equal("{\"key\":\"value\"}", result);
        }

        [Fact]
        public void JsonRepairer_TryRepairTruncatedJson_MissingClosingBracket_Added()
        {
            var result = JsonRepairer.TryRepairTruncatedJson("[1,2,3");
            Assert.Equal("[1,2,3]", result);
        }

        [Fact]
        public void JsonRepairer_TryRepairTruncatedJson_UnclosedString_Closed()
        {
            var result = JsonRepairer.TryRepairTruncatedJson("{\"key\":\"value");
            Assert.Equal("{\"key\":\"value\"}", result);
        }

        [Fact]
        public void JsonRepairer_TryRepairTruncatedJson_ComplexTruncation_Repaired()
        {
            var result = JsonRepairer.TryRepairTruncatedJson("{\"items\":[1,2");
            Assert.Equal("{\"items\":[1,2]}", result);
        }
    }
}
