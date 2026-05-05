using RimMind.Core.Client;
using Xunit;

namespace RimMind.Core.Tests
{
    public class JsonRepairHelperTests
    {
        [Fact]
        public void TryRepair_EmptyString_ReturnsNull()
        {
            Assert.Null(JsonRepairHelper.TryRepairTruncatedJson(""));
        }

        [Fact]
        public void TryRepair_NullInput_ReturnsNull()
        {
            Assert.Null(JsonRepairHelper.TryRepairTruncatedJson(null!));
        }

        [Fact]
        public void TryRepair_AlreadyValidJson_ReturnsNull()
        {
            Assert.Null(JsonRepairHelper.TryRepairTruncatedJson("{\"key\":\"value\"}"));
        }

        [Fact]
        public void TryRepair_MissingClosingBrace()
        {
            string input = "{\"key\":\"value\"";
            string? result = JsonRepairHelper.TryRepairTruncatedJson(input);
            Assert.NotNull(result);
            Assert.Equal("{\"key\":\"value\"}", result);
        }

        [Fact]
        public void TryRepair_MissingMultipleClosingBraces()
        {
            string input = "{\"outer\":{\"inner\":42";
            string? result = JsonRepairHelper.TryRepairTruncatedJson(input);
            Assert.NotNull(result);
            Assert.Equal("{\"outer\":{\"inner\":42}}", result);
        }

        [Fact]
        public void TryRepair_MissingClosingBracket()
        {
            string input = "{\"items\":[1,2,3";
            string? result = JsonRepairHelper.TryRepairTruncatedJson(input);
            Assert.NotNull(result);
            Assert.Equal("{\"items\":[1,2,3]}", result);
        }

        [Fact]
        public void TryRepair_UnclosedString()
        {
            string input = "{\"key\":\"incomplete";
            string? result = JsonRepairHelper.TryRepairTruncatedJson(input);
            Assert.NotNull(result);
            Assert.True(result!.EndsWith("}"));
        }

        [Fact]
        public void TryRepair_TrailingComma()
        {
            string input = "{\"key\":\"value\",";
            string? result = JsonRepairHelper.TryRepairTruncatedJson(input);
            Assert.NotNull(result);
            Assert.Equal("{\"key\":\"value\"}", result);
        }

        [Fact]
        public void TryRepair_ComplexTruncation()
        {
            string input = "{\"defName\":\"RaidEnemy\",\"params\":{\"points\":1.5";
            string? result = JsonRepairHelper.TryRepairTruncatedJson(input);
            Assert.NotNull(result);
            Assert.True(result!.EndsWith("}}"));
        }

        [Fact]
        public void TryRepair_ArrayTruncation()
        {
            string input = "[1,2,3";
            string? result = JsonRepairHelper.TryRepairTruncatedJson(input);
            Assert.NotNull(result);
            Assert.Equal("[1,2,3]", result);
        }

        [Fact]
        public void TryRepair_NestedArrayInObject()
        {
            string input = "{\"items\":[{\"name\":\"a\"";
            string? result = JsonRepairHelper.TryRepairTruncatedJson(input);
            Assert.NotNull(result);
            Assert.Equal("{\"items\":[{\"name\":\"a\"]}}", result);
        }

        [Fact]
        public void TryRepair_WhitespaceAtEnd()
        {
            string input = "{\"key\":\"value\"}   ";
            Assert.Null(JsonRepairHelper.TryRepairTruncatedJson(input));
        }
    }
}
