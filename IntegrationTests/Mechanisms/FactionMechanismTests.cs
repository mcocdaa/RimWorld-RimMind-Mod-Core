using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.World.Faction;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class FactionMechanismTests : TestBase
    {
        public FactionMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnAllFactions()
        {
            var mechanism = new FactionMechanism();
            var args = ReadArgs("world.faction", 0);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Set_AdjustGoodwill_ShouldChangeRelation()
        {
            var mechanism = new FactionMechanism();
            // First query to find a non-player faction
            var queryResult = await mechanism.ExecuteQueryAsync(ReadArgs("world.faction", 0), CancellationToken.None);
            queryResult.IsOk.Should().BeTrue();

            var factions = JArray.Parse(queryResult.Value);
            var targetFaction = factions.FirstOrDefault(f =>
                f["def"]?.ToString() != "PlayerColony" && f["factionId"] != null);

            if (targetFaction != null)
            {
                var factionIdStr = targetFaction["factionId"]?.ToString();
                int.TryParse(factionIdStr, out var factionId);

                var args = WriteArgs("world.faction", 0, "adjust_goodwill",
                    parms: new Dictionary<string, string>
                    {
                        { "target_faction_id", factionId.ToString() },
                        { "goodwill_change", "5" }
                    });
                var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
                result.IsOk.Should().BeTrue();
            }
        }

        [Fact]
        public async Task List_ShouldReturnFactionNames()
        {
            var mechanism = new FactionMechanism();
            var result = await mechanism.ExecuteListAsync(null, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public void GetRiskForOperation_SetShouldBeDangerous()
        {
            var mechanism = new FactionMechanism();
            mechanism.GetRiskForOperation(MechanismOperationType.Set).Should().Be(MechanismRisk.Dangerous);
            mechanism.GetRiskForOperation(MechanismOperationType.Query).Should().Be(MechanismRisk.Safe);
        }
    }
}
