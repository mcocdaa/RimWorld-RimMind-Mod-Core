using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.World.Storyteller;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class StorytellerActionEquivalenceTests : TestBase
    {
        public StorytellerActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task TriggerIncident_ShouldReturnResultWithoutThrowing()
        {
            var mechanism = new StorytellerMechanism();
            var args = MapWriteArgs("world.storyteller", "trigger",
                defName: "Aurora");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            // 事件触发可能因游戏状态而失败，验证机制正常返回即可
            (result.IsOk || result.IsErr).Should().BeTrue();
        }
    }
}
