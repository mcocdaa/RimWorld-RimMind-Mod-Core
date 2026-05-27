using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Map.Wealth;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class WealthMechanismTests : TestBase
    {
        public WealthMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnWealthData()
        {
            var mechanism = new WealthMechanism();
            var args = ReadArgs("map.wealth", 0);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            var json = JObject.Parse(result.Value);
            json["totalWealth"].Should().NotBeNull();
            json["itemsWealth"].Should().NotBeNull();
            json["buildingsWealth"].Should().NotBeNull();
            json["threatPoints"].Should().NotBeNull();
        }

        [Fact]
        public async Task Query_InvalidMapId_ShouldReturnError()
        {
            var mechanism = new WealthMechanism();
            var args = new MechanismReadArgs
            {
                MechanismId = "map.wealth",
                PawnId = 0,
                MapId = -99999
            };
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
