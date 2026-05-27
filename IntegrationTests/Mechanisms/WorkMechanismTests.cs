using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Work;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class WorkMechanismTests : TestBase
    {
        public WorkMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnWorkPriorities()
        {
            var mechanism = new WorkMechanism();
            var args = ReadArgs("pawn.work", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Set_SetPriority_ShouldUpdatePriority()
        {
            var mechanism = new WorkMechanism();
            // Use a common work type that should exist
            var args = WriteArgs("pawn.work", PawnId, "set_priority", defName: "WorkType_Firefighting", valueJson: "3");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task List_ShouldReturnVisibleWorkTypes()
        {
            var mechanism = new WorkMechanism();
            var result = await mechanism.ExecuteListAsync(PawnId, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Set_InvalidPriority_ShouldReturnError()
        {
            var mechanism = new WorkMechanism();
            var args = WriteArgs("pawn.work", PawnId, "set_priority", defName: "WorkType_Firefighting", valueJson: "99");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
