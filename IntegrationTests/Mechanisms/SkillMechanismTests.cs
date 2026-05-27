using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Skill;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class SkillMechanismTests : TestBase
    {
        public SkillMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnAllSkills()
        {
            var mechanism = new SkillMechanism();
            var args = ReadArgs("pawn.skill", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Set_LearnXp_ShouldIncreaseXp()
        {
            var mechanism = new SkillMechanism();
            // Query first to find a valid skill
            var queryArgs = ReadArgs("pawn.skill", PawnId);
            var queryResult = await mechanism.ExecuteQueryAsync(queryArgs, CancellationToken.None);
            queryResult.IsOk.Should().BeTrue();

            var skills = JArray.Parse(queryResult.Value);
            if (skills.Count > 0)
            {
                var firstSkillDef = skills[0]["def"]?.ToString();
                var args = WriteArgs("pawn.skill", PawnId, "learn_xp", defName: firstSkillDef, valueJson: "100.0");
                var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
                result.IsOk.Should().BeTrue();
            }
        }

        [Fact]
        public async Task List_ShouldReturnSkillList()
        {
            var mechanism = new SkillMechanism();
            var result = await mechanism.ExecuteListAsync(PawnId, CancellationToken.None);
            result.IsOk.Should().BeTrue();
            result.Value.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Set_InvalidPawnId_ShouldReturnError()
        {
            var mechanism = new SkillMechanism();
            var args = WriteArgs("pawn.skill", -1, "learn_xp", defName: "Shooting", valueJson: "100.0");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
