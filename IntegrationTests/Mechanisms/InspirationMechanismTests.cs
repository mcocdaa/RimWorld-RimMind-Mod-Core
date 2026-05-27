using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Inspiration;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class InspirationMechanismTests : TestBase
    {
        public InspirationMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnCurrentInspiration()
        {
            var mechanism = new InspirationMechanism();
            var args = ReadArgs("pawn.inspiration", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            var json = JObject.Parse(result.Value);
            json["hasInspiration"].Should().NotBeNull();
        }

        [Fact]
        public async Task Trigger_ShouldStartInspiration()
        {
            var mechanism = new InspirationMechanism();
            var args = WriteArgs("pawn.inspiration", PawnId, "trigger", defName: "Inspired_Creativity");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task List_ShouldReturnAllInspirationDefs()
        {
            var mechanism = new InspirationMechanism();
            var result = await mechanism.ExecuteListAsync(PawnId, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Trigger_InvalidDefName_ShouldReturnError()
        {
            var mechanism = new InspirationMechanism();
            var args = WriteArgs("pawn.inspiration", PawnId, "trigger", defName: "NonExistentDef");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
