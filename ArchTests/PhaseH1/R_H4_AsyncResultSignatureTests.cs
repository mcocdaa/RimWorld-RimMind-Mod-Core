using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using RimMind.Application.Common.Interfaces.Mechanisms;
using RimMind.Domain.ValueObjects;
using RimMind.Application.Common.Interfaces.Tools;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseH1
{
    public class R_H4_AsyncResultSignatureTests
    {
        [Fact]
        [Trait("Phase", "H1")]
        public void IToolHandler_ExecuteAsync_Should_Return_Task_Result()
        {
            var m = typeof(IToolHandler).GetMethod("ExecuteAsync");
            m.Should().NotBeNull("IToolHandler must define ExecuteAsync");

            var ret = m!.ReturnType;
            ret.IsGenericType.Should().BeTrue("ExecuteAsync must return a generic Task<>");
            ret.GetGenericTypeDefinition().Should().Be(typeof(Task<>),
                "ExecuteAsync must return Task<>");

            var inner = ret.GetGenericArguments()[0];
            inner.IsGenericType.Should().BeTrue("Inner type must be Result<,>");
            inner.GetGenericTypeDefinition().Should().Be(typeof(Result<,>),
                "ExecuteAsync must return Task<Result<ToolResult, RimMindError>>");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanism_ExecuteQueryAsync_Should_Return_Task_Result()
        {
            var m = typeof(IGameMechanism).GetMethod("ExecuteQueryAsync");
            m.Should().NotBeNull("IGameMechanism must define ExecuteQueryAsync");

            var ret = m!.ReturnType;
            ret.IsGenericType.Should().BeTrue("ExecuteQueryAsync must return a generic Task<>");
            ret.GetGenericTypeDefinition().Should().Be(typeof(Task<>),
                "ExecuteQueryAsync must return Task<>");

            var inner = ret.GetGenericArguments()[0];
            inner.IsGenericType.Should().BeTrue("Inner type must be Result<,>");
            inner.GetGenericTypeDefinition().Should().Be(typeof(Result<,>),
                "ExecuteQueryAsync must return Task<Result<...>>");
        }

        [Fact]
        [Trait("Phase", "H1")]
        public void IGameMechanism_ExecuteSetAsync_Should_Return_Task_Result()
        {
            var m = typeof(IGameMechanism).GetMethod("ExecuteSetAsync");
            m.Should().NotBeNull("IGameMechanism must define ExecuteSetAsync");

            var ret = m!.ReturnType;
            ret.IsGenericType.Should().BeTrue("ExecuteSetAsync must return a generic Task<>");
            ret.GetGenericTypeDefinition().Should().Be(typeof(Task<>),
                "ExecuteSetAsync must return Task<>");

            var inner = ret.GetGenericArguments()[0];
            inner.IsGenericType.Should().BeTrue("Inner type must be Result<,>");
            inner.GetGenericTypeDefinition().Should().Be(typeof(Result<,>),
                "ExecuteSetAsync must return Task<Result<...>>");
        }
    }
}
