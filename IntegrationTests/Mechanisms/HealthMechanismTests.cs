using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Health;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class HealthMechanismTests : TestBase
    {
        public HealthMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnHediffs()
        {
            var mechanism = new HealthMechanism();
            var args = ReadArgs("pawn.health", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task List_ShouldReturnAllHediffDefs()
        {
            var mechanism = new HealthMechanism();
            var result = await mechanism.ExecuteListAsync(PawnId, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Query_InvalidPawnId_ShouldReturnError()
        {
            var mechanism = new HealthMechanism();
            var args = ReadArgs("pawn.health", -1);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
