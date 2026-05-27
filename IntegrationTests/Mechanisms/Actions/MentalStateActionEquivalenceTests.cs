using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.MentalState;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class MentalStateActionEquivalenceTests : TestBase
    {
        public MentalStateActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TriggerMentalState_ShouldReturnOk()
        {
            var mechanism = new MentalStateMechanism();
            var args = WriteArgs("pawn.mental_state", PawnId, "trigger",
                defName: "WanderPsychotic");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }
    }
}
