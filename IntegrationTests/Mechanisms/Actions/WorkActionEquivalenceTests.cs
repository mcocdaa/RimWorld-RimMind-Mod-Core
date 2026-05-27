using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Work;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class WorkActionEquivalenceTests : TestBase
    {
        public WorkActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task SetWorkPriority_ShouldReturnOk()
        {
            var mechanism = new WorkMechanism();
            var args = WriteArgs("pawn.work", PawnId, "set_priority",
                defName: "ManualDumb",
                valueJson: "3");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }
    }
}
