using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Thought;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class ThoughtMechanismTests : TestBase
    {
        public ThoughtMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnMemories()
        {
            var mechanism = new ThoughtMechanism();
            var args = ReadArgs("pawn.thought", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Add_ShouldAddMemory()
        {
            var mechanism = new ThoughtMechanism();
            var args = WriteArgs("pawn.thought", PawnId, "add", defName: "AteWithoutTable");
            var result = await mechanism.ExecuteAddAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task List_ShouldReturnAllThoughtDefs()
        {
            var mechanism = new ThoughtMechanism();
            var result = await mechanism.ExecuteListAsync(PawnId, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Add_InvalidDefName_ShouldReturnError()
        {
            var mechanism = new ThoughtMechanism();
            var args = WriteArgs("pawn.thought", PawnId, "add", defName: "NonExistentDef");
            var result = await mechanism.ExecuteAddAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
