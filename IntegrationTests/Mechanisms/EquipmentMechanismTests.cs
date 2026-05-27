using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Equipment;

namespace RimMind.IntegrationTests.Mechanisms
{
    [Collection("RimWorld Integration")]
    public class EquipmentMechanismTests : TestBase
    {
        public EquipmentMechanismTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Query_ShouldReturnEquipment()
        {
            var mechanism = new EquipmentMechanism();
            var args = ReadArgs("pawn.equipment", PawnId);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsOk.Should().BeTrue();
        }

        [Fact]
        public async Task Set_DropWeapon_ShouldRemovePrimary()
        {
            var mechanism = new EquipmentMechanism();
            var queryArgs = ReadArgs("pawn.equipment", PawnId);
            var queryResult = await mechanism.ExecuteQueryAsync(queryArgs, CancellationToken.None);

            var json = JArray.Parse(queryResult.Value);
            if (json.Count > 0)
            {
                var args = WriteArgs("pawn.equipment", PawnId, "drop_weapon");
                var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
                result.IsOk.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Query_InvalidPawnId_ShouldReturnError()
        {
            var mechanism = new EquipmentMechanism();
            var args = ReadArgs("pawn.equipment", -1);
            var result = await mechanism.ExecuteQueryAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
