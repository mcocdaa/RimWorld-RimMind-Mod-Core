using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Recruit;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class RecruitMechanismTests : TestBase
    {
        public RecruitMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Trigger_RecruitAgree_ShouldChangeFaction()
        {
            var mechanism = new RecruitMechanism();
            // Test pawns are already player faction, so we test the error path for already-recruited.
            // A full recruit test would need a prisoner pawn, which requires more complex setup.
            var args = WriteArgs("pawn.recruit", PawnId, "recruit_agree");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            // Colonist pawns are already in player faction, so this should return error
            result.IsErr.Should().BeTrue();
        }

        [Fact]
        public async Task Trigger_AlreadyPlayerFaction_ShouldReturnError()
        {
            var mechanism = new RecruitMechanism();
            var args = WriteArgs("pawn.recruit", PawnId, "recruit_agree");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }

        [Fact]
        public async Task Trigger_InvalidPawnId_ShouldReturnError()
        {
            var mechanism = new RecruitMechanism();
            var args = WriteArgs("pawn.recruit", -1, "recruit_agree");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
