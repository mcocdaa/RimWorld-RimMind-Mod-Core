using System.IO;
using FluentAssertions;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseStructure
{
    public class R_S1_NoNpcTopLevelDirTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        [Fact, Trait("Phase", "Structure")]
        public void Source_Npc_Directory_Should_Not_Exist()
        {
            var npcDir = Path.Combine(SourceRoot, "Npc");
            Directory.Exists(npcDir).Should().BeFalse(
                "Npc/ 已迁移到 Kernel/Npc/ 和 Adapters/Client/Player2/");
        }
    }
}
