using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Tools;
using RimMind.Application.Features.Tools;
using Xunit;

namespace RimMind.Presentation.Tests
{
    public class ToolRegistryTests
    {
        private class StubToolHandler : IToolHandler
        {
            public string Id => Definition.Id;
            public ToolDefinition Definition { get; }
            public StubToolHandler(string id, string description = "", string category = "general")
            {
                Definition = new ToolDefinition { Id = id, Description = description, Category = category };
            }
            public Task<Result<ToolResult, RimMindError>> ExecuteAsync(ToolCallArgs args, CancellationToken ct)
                => Task.FromResult(Result<ToolResult, RimMindError>.Ok(new ToolResult { ToolCallId = args.ToolCallId, Content = "stub" }));
        }

        private readonly ToolRegistry _sut = new();

        [Fact]
        public void Register_AddsHandler_And_FindById_ReturnsIt()
        {
            var handler = new StubToolHandler("tool-1", "desc");
            _sut.Register(handler);
            Assert.Same(handler, _sut.FindById("tool-1"));
        }

        [Fact]
        public void Register_NullHandler_DoesNotThrow()
        {
            var ex = Record.Exception(() => _sut.Register(null!));
            Assert.Null(ex);
        }

        [Fact]
        public void FindById_ReturnsNull_ForUnknownTool()
        {
            Assert.Null(_sut.FindById("nonexistent"));
        }

        [Fact]
        public void Unregister_RemovesHandler_And_ReturnsTrue()
        {
            _sut.Register(new StubToolHandler("tool-1"));
            var result = _sut.Unregister("tool-1");
            Assert.True(result);
            Assert.Null(_sut.FindById("tool-1"));
        }

        [Fact]
        public void Unregister_ReturnsFalse_ForUnknownTool()
        {
            Assert.False(_sut.Unregister("nonexistent"));
        }

        [Fact]
        public void All_ReturnsAllRegisteredHandlers()
        {
            var h1 = new StubToolHandler("a");
            var h2 = new StubToolHandler("b");
            _sut.Register(h1);
            _sut.Register(h2);
            var all = _sut.All;
            Assert.Equal(2, all.Count);
            Assert.Contains(h1, all);
            Assert.Contains(h2, all);
        }

        [Fact]
        public void All_ReturnsEmptyList_WhenNoHandlers()
        {
            Assert.Empty(_sut.All);
        }

        [Fact]
        public void GetAllDefinitions_ReturnsDefinitionsOfAllHandlers()
        {
            _sut.Register(new StubToolHandler("x", "desc-x", "cat-a"));
            _sut.Register(new StubToolHandler("y", "desc-y", "cat-b"));
            var defs = _sut.GetAllDefinitions();
            Assert.Equal(2, defs.Count);
            Assert.Contains(defs, d => d.Id == "x" && d.Description == "desc-x");
            Assert.Contains(defs, d => d.Id == "y" && d.Category == "cat-b");
        }

        [Fact]
        public void Register_WithSameId_ReplacesExistingHandler()
        {
            var original = new StubToolHandler("tool-1", "original");
            var replacement = new StubToolHandler("tool-1", "replaced");
            _sut.Register(original);
            _sut.Register(replacement);
            Assert.Same(replacement, _sut.FindById("tool-1"));
            Assert.Single(_sut.All);
        }

        [Fact]
        public void GetAllDefinitions_ReturnsEmptyList_WhenNoHandlers()
        {
            Assert.Empty(_sut.GetAllDefinitions());
        }

        [Fact]
        public void Register_WithThreeSegmentId_Works()
        {
            var handler = new StubToolHandler("pawn.skill.query", "query pawn skill", "pawn");
            _sut.Register(handler);
            Assert.Same(handler, _sut.FindById("pawn.skill.query"));
        }
    }
}
