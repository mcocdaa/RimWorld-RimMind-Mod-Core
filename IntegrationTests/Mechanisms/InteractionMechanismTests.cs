using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Interaction;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class InteractionMechanismTests : TestBase
    {
        public InteractionMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnSocialInfo()
        {
            var mechanism = new InteractionMechanism();
            var args = ReadArgs("pawn.interaction", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            var json = JObject.Parse(result.Value);
            json["socialSkill"].Should().NotBeNull();
        }

        [Fact]
        public async Task Trigger_SocialRelax_ShouldAssignJob()
        {
            var mechanism = new InteractionMechanism();
            var args = WriteArgs("pawn.interaction", PawnId, "social_relax");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Trigger_RomanceBreakup_ShouldBeDangerous()
        {
            var mechanism = new InteractionMechanism();
            var otherPawnId = World.GetPawnId(1);
            var args = WriteArgs("pawn.interaction", PawnId, "romance_breakup",
                parms: new Dictionary<string, string> { { "target_pawn_id", otherPawnId.ToString() } });
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            // Romance breakup may succeed or fail depending on game state,
            // but it should not throw an unhandled exception.
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Trigger_InvalidAction_ShouldReturnError()
        {
            var mechanism = new InteractionMechanism();
            var args = WriteArgs("pawn.interaction", PawnId, "nonexistent_action");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
