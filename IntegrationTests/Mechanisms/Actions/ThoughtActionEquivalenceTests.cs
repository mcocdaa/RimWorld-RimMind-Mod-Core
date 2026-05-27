using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Thought;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class ThoughtActionEquivalenceTests : TestBase
    {
        public ThoughtActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task AddThought_ShouldReturnOk()
        {
            var mechanism = new ThoughtMechanism();
            var args = WriteArgs("pawn.thought", PawnId, "add",
                defName: "AteWithoutTable");
            var result = await mechanism.ExecuteAddAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }
    }
}
