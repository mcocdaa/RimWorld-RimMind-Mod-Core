using System.Threading.Tasks;
using FluentAssertions;
using RimMind.Application.Common.Behaviours;
using RimMind.Application.Common.Constants;
using RimMind.Application.Common.Interfaces.Abstractions;
using RimMind.Application.Common.Models.Pipeline;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN
{
    public class R_N7_MiddlewareBaseTests
    {
        [Fact]
        [Trait("Phase", "N")]
        public void MiddlewareBase_DefaultOwnerModId_ShouldBe_CoreModId()
        {
            var dummy = new TestMiddleware();
            dummy.OwnerModId.Should().Be(RimMindOwnerConsts.CoreModId);
        }

        [Fact]
        [Trait("Phase", "N")]
        public void MiddlewareBase_DefaultId_ShouldEqual_Name()
        {
            var dummy = new TestMiddleware();
            dummy.Id.Should().Be(dummy.Name);
        }

        [Fact]
        [Trait("Phase", "N")]
        public void MiddlewareBase_Constructor_ShouldStore_Log()
        {
            var log = new CapturingLogSink();
            var dummy = new TestMiddleware(log);
            dummy.LogAccessed.Should().BeTrue("the protected Log field should be available to subclasses");
        }

        private sealed class TestMiddleware : MiddlewareBase<LlmRequestContext>
        {
            public bool LogAccessed => Log != null;

            public TestMiddleware(ILogSink? log = null) : base(log) { }

            public override string Name => "Test";
            public override int Order => 0;
            public override Task InvokeAsync(LlmRequestContext context, MiddlewareDelegate<LlmRequestContext> next)
                => next(context);
        }

        private sealed class CapturingLogSink : ILogSink
        {
            public void Message(string msg) { }
            public void Warning(string msg) { }
            public void Error(string msg) { }
            public void LogFromBackground(string msg, bool isWarning = false) { }
        }
    }
}
