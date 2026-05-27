using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Job;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class JobActionEquivalenceTests : TestBase
    {
        public JobActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task ForceRest_ShouldReturnOk()
        {
            var mechanism = new JobMechanism();
            var args = WriteArgs("pawn.job", PawnId, "force_rest");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task AssignWork_ShouldReturnOk()
        {
            var mechanism = new JobMechanism();
            var parms = new Dictionary<string, string> { ["work_type"] = "DoBillsCookCampfire" };
            var args = WriteArgs("pawn.job", PawnId, "assign_work", parms: parms);
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task MoveTo_ShouldReturnOk()
        {
            var mechanism = new JobMechanism();
            var cellX = Pawn.Position.x;
            var cellZ = Pawn.Position.z;
            var parms = new Dictionary<string, string>
            {
                ["cell_x"] = cellX.ToString(),
                ["cell_z"] = cellZ.ToString()
            };
            var args = WriteArgs("pawn.job", PawnId, "move_to", parms: parms);
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task EatFood_ShouldReturnOk()
        {
            var mechanism = new JobMechanism();
            var args = WriteArgs("pawn.job", PawnId, "eat_food");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task TendPawn_ShouldReturnOk_WhenTargetExists()
        {
            var mechanism = new JobMechanism();
            var targetPawnId = World.GetPawnId(1);
            var parms = new Dictionary<string, string> { ["target_pawn_id"] = targetPawnId.ToString() };
            var args = WriteArgs("pawn.job", PawnId, "tend_pawn", parms: parms);
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task RescuePawn_ShouldReturnOk_WhenTargetExists()
        {
            var mechanism = new JobMechanism();
            var targetPawnId = World.GetPawnId(1);
            var parms = new Dictionary<string, string> { ["target_pawn_id"] = targetPawnId.ToString() };
            var args = WriteArgs("pawn.job", PawnId, "rescue_pawn", parms: parms);
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task ArrestPawn_ShouldReturnOk_WhenTargetExists()
        {
            var mechanism = new JobMechanism();
            var targetPawnId = World.GetPawnId(1);
            var parms = new Dictionary<string, string> { ["target_pawn_id"] = targetPawnId.ToString() };
            var args = WriteArgs("pawn.job", PawnId, "arrest_pawn", parms: parms);
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task CancelJob_ShouldReturnOk()
        {
            var mechanism = new JobMechanism();
            var args = WriteArgs("pawn.job", PawnId, "cancel_job");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }
    }
}
