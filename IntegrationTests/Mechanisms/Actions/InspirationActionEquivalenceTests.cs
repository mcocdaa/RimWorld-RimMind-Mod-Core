using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Inspiration;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class InspirationActionEquivalenceTests : TestBase
    {
        public InspirationActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task InspireWork_ShouldReturnOk()
        {
            var mechanism = new InspirationMechanism();
            var args = WriteArgs("pawn.inspiration", PawnId, "trigger",
                defName: "Inspired_Work");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task InspireShoot_ShouldReturnOk()
        {
            var mechanism = new InspirationMechanism();
            var args = WriteArgs("pawn.inspiration", PawnId, "trigger",
                defName: "Inspired_Shooting");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task InspireTrade_ShouldReturnOk()
        {
            var mechanism = new InspirationMechanism();
            var args = WriteArgs("pawn.inspiration", PawnId, "trigger",
                defName: "Inspired_Trade");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }
    }
}
