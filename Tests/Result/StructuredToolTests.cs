using RimMind.Domain.Llm;
using Xunit;

namespace RimMind.Presentation.Tests.Result
{
    /// <summary>
    /// StructuredTool / StructuredToolCall POCO 测试：验证默认值和赋值行为。迁移自 RimMind-Actions/Tests。
    /// </summary>
    public class StructuredToolTests
    {
        [Fact]
        public void StructuredTool_默认Name为空字符串()
        {
            var tool = new StructuredTool();
            Assert.Equal("", tool.Name);
        }

        [Fact]
        public void StructuredTool_默认Description为空字符串()
        {
            var tool = new StructuredTool();
            Assert.Equal("", tool.Description);
        }

        [Fact]
        public void StructuredTool_Parameters和ToolChoice默认为null()
        {
            var tool = new StructuredTool();
            Assert.Null(tool.Parameters);
            Assert.Null(tool.ToolChoice);
        }

        [Fact]
        public void StructuredTool_属性可正确赋值()
        {
            var tool = new StructuredTool
            {
                Name = "move_to",
                Description = "移动到指定坐标",
                Parameters = "{\"x\":0,\"z\":0}",
                ToolChoice = "auto"
            };
            Assert.Equal("move_to", tool.Name);
            Assert.Equal("移动到指定坐标", tool.Description);
            Assert.Equal("{\"x\":0,\"z\":0}", tool.Parameters);
            Assert.Equal("auto", tool.ToolChoice);
        }

        [Fact]
        public void StructuredToolCall_默认值均为空字符串()
        {
            var call = new StructuredToolCall();
            Assert.Equal("", call.Id);
            Assert.Equal("", call.Name);
            Assert.Equal("", call.Arguments);
        }

        [Fact]
        public void StructuredToolCall_属性可正确赋值()
        {
            var call = new StructuredToolCall
            {
                Id = "call-001",
                Name = "draft",
                Arguments = "{\"pawn_id\":1}"
            };
            Assert.Equal("call-001", call.Id);
            Assert.Equal("draft", call.Name);
            Assert.Equal("{\"pawn_id\":1}", call.Arguments);
        }
    }
}
