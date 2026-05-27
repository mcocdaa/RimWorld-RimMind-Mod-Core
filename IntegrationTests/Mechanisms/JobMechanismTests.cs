using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Job;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class JobMechanismTests : TestBase
    {
        public JobMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnCurrentJob()
        {
            var mechanism = new JobMechanism();
            var args = ReadArgs("pawn.job", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            var json = JObject.Parse(result.Value);
            json["jobQueueCount"].Should().NotBeNull();
        }

        [Fact]
        public async Task Set_AssignWork_ShouldStartJob()
        {
            var mechanism = new JobMechanism();
            var args = WriteArgs("pawn.job", PawnId, "assign_work", defName: "WorkGiver_HaulGeneral");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Set_CancelJob_ShouldEndJob()
        {
            var mechanism = new JobMechanism();
            var args = WriteArgs("pawn.job", PawnId, "cancel_job");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Set_InvalidAction_ShouldReturnError()
        {
            var mechanism = new JobMechanism();
            var args = WriteArgs("pawn.job", PawnId, "nonexistent_action");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
