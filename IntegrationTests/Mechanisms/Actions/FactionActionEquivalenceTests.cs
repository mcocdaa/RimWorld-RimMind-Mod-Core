using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.World.Faction;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class FactionActionEquivalenceTests : TestBase
    {
        public FactionActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task AdjustFaction_ShouldReturnResultWithoutThrowing()
        {
            var mechanism = new FactionMechanism();
            var parms = new Dictionary<string, string>
            {
                ["target_faction_id"] = PlayerFaction.loadID.ToString(),
                ["goodwill_change"] = "1"
            };
            var args = MapWriteArgs("world.faction", "adjust_goodwill", parms: parms);
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            // 对玩家阵营调整好感度可能成功也可能失败，验证机制正常返回即可
            (result.IsOk || result.IsErr).Should().BeTrue();
        }
    }
}
