using System.Threading;
using System.Threading.Tasks;
using RimMind.Application.Common.Models.Mechanisms;
using RimMind.Infrastructure.Mechanisms.Pawn.Equipment;

namespace RimMind.IntegrationTests.Mechanisms.Actions
{
    [Collection("RimWorld Integration")]
    public class EquipmentActionEquivalenceTests : TestBase
    {
        public EquipmentActionEquivalenceTests(TestWorldFixture fixture) : base(fixture) { }

        [Fact]
        public async Task DropWeapon_ShouldReturnErr_WhenNoWeaponEquipped()
        {
            var mechanism = new EquipmentMechanism();
            var args = WriteArgs("pawn.equipment", PawnId, "drop_weapon");
            var result = await mechanism.ExecuteSetAsync(args, CancellationToken.None);
            result.IsErr.Should().BeTrue();
        }
    }
}
