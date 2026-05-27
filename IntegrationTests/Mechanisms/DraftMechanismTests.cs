using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Draft;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class DraftMechanismTests : TestBase
    {
        public DraftMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnDraftedState()
        {
            var mechanism = new DraftMechanism();
            var args = ReadArgs("pawn.draft", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            var json = JObject.Parse(result.Value);
            json["drafted"].Should().NotBeNull();
        }

        [Fact]
        public async Task Toggle_Draft_ShouldSetDraftedTrue()
        {
            var mechanism = new DraftMechanism();
            var args = WriteArgs("pawn.draft", PawnId, "draft");
            var result = await mechanism.ExecuteToggleAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        [Fact]
        public async Task Toggle_Undraft_ShouldSetDraftedFalse()
        {
            var mechanism = new DraftMechanism();
            var draftArgs = WriteArgs("pawn.draft", PawnId, "draft");
            await mechanism.ExecuteToggleAsync(draftArgs, CancellationToken.None);

            var undraftArgs = WriteArgs("pawn.draft", PawnId, "undraft");
            var result = await mechanism.ExecuteToggleAsync(undraftArgs, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        [Fact]
        public async Task Query_InvalidPawnId_ShouldReturnError()
        {
            var mechanism = new DraftMechanism();
            var args = ReadArgs("pawn.draft", -1);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
