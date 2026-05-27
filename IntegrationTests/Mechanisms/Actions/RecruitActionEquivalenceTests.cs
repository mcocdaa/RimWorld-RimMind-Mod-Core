using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Recruit;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class RecruitActionEquivalenceTests : TestBase
    {
        public RecruitActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task RecruitAgree_ShouldReturnErr_WhenPawnIsAlreadyColonist()
        {
            var mechanism = new RecruitMechanism();
            var args = WriteArgs("pawn.recruit", PawnId, "recruit_agree");
            var result = await mechanism.ExecuteTriggerAsync(args, CancellationToken.None);
            // 测试用的小人已经是殖民者，招募应返回错误
            result.IsErr.Should().BeTrue();
        }
    }
}
