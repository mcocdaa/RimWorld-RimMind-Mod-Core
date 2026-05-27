using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Draft;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class DraftActionEquivalenceTests : TestBase
    {
        public DraftActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Draft_ShouldSetDraftedTrue()
        {
            var mechanism = new DraftMechanism();
            var args = WriteArgs("pawn.draft", PawnId, "draft");
            var result = await mechanism.ExecuteToggleAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            Pawn.drafter.Drafted.Should().BeTrue();
        }

        [Fact]
        public async Task Undraft_ShouldSetDraftedFalse()
        {
            // 先确保处于 drafted 状态
            var mechanism = new DraftMechanism();
            var draftArgs = WriteArgs("pawn.draft", PawnId, "draft");
            await mechanism.ExecuteToggleAsync(draftArgs, CancellationToken.None);

            var undraftArgs = WriteArgs("pawn.draft", PawnId, "undraft");
            var result = await mechanism.ExecuteToggleAsync(undraftArgs, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            Pawn.drafter.Drafted.Should().BeFalse();
        }
    }
}
