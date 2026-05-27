using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Need;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class NeedMechanismTests : TestBase
    {
        public NeedMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnAllNeeds()
        {
            var mechanism = new NeedMechanism();
            var args = ReadArgs("pawn.need", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Set_SetLevel_ShouldUpdateNeedLevel()
        {
            var mechanism = new NeedMechanism();
            var queryArgs = ReadArgs("pawn.need", PawnId);
            var queryResult = await mechanism.ExecuteQueryAsync(queryArgs, CancellationToken.None);
            queryResult.IsOk.Should().BeTrue();

            var needs = JArray.Parse(queryResult.Value);
            if (needs.Count > 0)
            {
                var firstNeed = needs[0]["def"]?.ToString();
                var args = WriteArgs("pawn.need", PawnId, "set_level", defName: firstNeed, valueJson: "0.5");
                var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
                result.IsOk.Should().BeTrue();
            }
        }

        [Fact]
        public async Task List_ShouldReturnNeedTypes()
        {
            var mechanism = new NeedMechanism();
            var result = await mechanism.ExecuteListAsync(PawnId, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Query_InvalidPawnId_ShouldReturnError()
        {
            var mechanism = new NeedMechanism();
            var args = ReadArgs("pawn.need", -1);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
