using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Relations;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class RelationsMechanismTests : TestBase
    {
        public RelationsMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnDirectRelations()
        {
            var mechanism = new RelationsMechanism();
            var args = ReadArgs("pawn.relations", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            var json = JObject.Parse(result.Value);
            json["relations"].Should().NotBeNull();
            json["opinions"].Should().NotBeNull();
        }

        [Fact]
        public async Task Query_InvalidPawnId_ShouldReturnError()
        {
            var mechanism = new RelationsMechanism();
            var args = ReadArgs("pawn.relations", -1);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
