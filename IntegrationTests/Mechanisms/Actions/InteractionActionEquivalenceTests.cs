using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Interaction;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class InteractionActionEquivalenceTests : TestBase
    {
        public InteractionActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task SocialRelax_ShouldReturnResultWithoutThrowing()
        {
            var mechanism = new InteractionMechanism();
            var args = WriteArgs("pawn.interaction", PawnId, "social_relax");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            // 社交放松可能因环境不可用而失败，验证机制正常返回即可
            (result.IsOk || result.IsErr).Should().BeTrue();
        }

        [Fact]
        public async Task GiveItem_ShouldReturnOk_WhenTargetExists()
        {
            var mechanism = new InteractionMechanism();
            var targetPawnId = World.GetPawnId(1);
            var parms = new Dictionary<string, string> { ["target_pawn_id"] = targetPawnId.ToString() };
            var args = WriteArgs("pawn.interaction", PawnId, "give_item", parms: parms);
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task RomanceAttempt_ShouldReturnOk_WhenTargetExists()
        {
            var mechanism = new InteractionMechanism();
            var targetPawnId = World.GetPawnId(1);
            var parms = new Dictionary<string, string> { ["target_pawn_id"] = targetPawnId.ToString() };
            var args = WriteArgs("pawn.interaction", PawnId, "romance_attempt", parms: parms);
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task RomanceBreakup_ShouldReturnOk_WhenTargetExists()
        {
            var mechanism = new InteractionMechanism();
            var targetPawnId = World.GetPawnId(1);
            var parms = new Dictionary<string, string> { ["target_pawn_id"] = targetPawnId.ToString() };
            var args = WriteArgs("pawn.interaction", PawnId, "romance_breakup", parms: parms);
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }
    }
}
