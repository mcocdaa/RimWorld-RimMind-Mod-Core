using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.MentalState;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class MentalStateMechanismTests : TestBase
    {
        public MentalStateMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnCurrentState()
        {
            var mechanism = new MentalStateMechanism();
            var args = ReadArgs("pawn.mental_state", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            var json = JObject.Parse(result.Value);
            json["hasMentalState"].Should().NotBeNull();
        }

        [Fact]
        public async Task Trigger_ShouldStartMentalState()
        {
            var mechanism = new MentalStateMechanism();
            var args = WriteArgs("pawn.mental_state", PawnId, "trigger", defName: "MentalState_WanderSad");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task List_ShouldReturnAllMentalStateDefs()
        {
            var mechanism = new MentalStateMechanism();
            var result = await mechanism.ExecuteListAsync(PawnId, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public void GetRiskForOperation_TriggerShouldBeDangerous()
        {
            var mechanism = new MentalStateMechanism();
            mechanism.GetRiskForOperation(MechanismOperationType.Trigger).Should().Be(MechanismRisk.Dangerous);
            mechanism.GetRiskForOperation(MechanismOperationType.Query).Should().Be(MechanismRisk.Safe);
        }
    }
}
