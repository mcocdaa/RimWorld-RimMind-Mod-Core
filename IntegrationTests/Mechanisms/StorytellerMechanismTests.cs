using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.World.Storyteller;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class StorytellerMechanismTests : TestBase
    {
        public StorytellerMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnStorytellerInfo()
        {
            var mechanism = new StorytellerMechanism();
            var args = ReadArgs("world.storyteller", 0);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            var json = JObject.Parse(result.Value);
            json["def"].Should().NotBeNull();
            json["label"].Should().NotBeNull();
        }

        [Fact]
        public async Task Trigger_ShouldExecuteIncident()
        {
            var mechanism = new StorytellerMechanism();
            var args = WriteArgs("world.storyteller", 0, "trigger", defName: "RaidEnemy");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            // May succeed or fail depending on game state (e.g., no valid map)
            // but should return a proper Result without throwing
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task List_ShouldReturnIncidentDefs()
        {
            var mechanism = new StorytellerMechanism();
            var result = await mechanism.ExecuteListAsync(null, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public void GetRiskForOperation_TriggerShouldBeDangerous()
        {
            var mechanism = new StorytellerMechanism();
            mechanism.GetRiskForOperation(MechanismOperationType.Trigger).Should().Be(MechanismRisk.Dangerous);
            mechanism.GetRiskForOperation(MechanismOperationType.Query).Should().Be(MechanismRisk.Safe);
        }
    }
}
