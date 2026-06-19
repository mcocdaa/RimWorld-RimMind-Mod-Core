using System.IO;
using Xunit;

namespace RimMind.Core.ArchTests.PhaseN
{
    public class R_N3_DebugCenterPageRegistryTests
    {
        private static readonly string SourceRoot = ArchTestExtensions.FindSourceDirectory();

        private static readonly string RegistryPath = Path.Combine(
            SourceRoot,
            "Infrastructure",
            "UI",
            "DebugCenter",
            "DebugCenterPageRegistry.cs");

        private static readonly string HubPath = Path.Combine(
            SourceRoot,
            "Infrastructure",
            "UI",
            "MainTabWindow_RimMindHub.cs");

        [Fact]
        public void R_N3_DebugCenterPageRegistry_Source_Exists()
        {
            Assert.True(File.Exists(RegistryPath), "DebugCenterPageRegistry.cs must exist under Infrastructure/UI/DebugCenter.");
        }

        [Fact]
        public void R_N3_DebugCenterPageRegistry_Exposes_Factory_Based_Register_Find_GetAll_And_Create()
        {
            Assert.True(File.Exists(RegistryPath), "DebugCenterPageRegistry.cs must exist under Infrastructure/UI/DebugCenter.");

            string content = File.ReadAllText(RegistryPath);

            Assert.Contains("Func<IDebugCenterPageDrawer>", content);
            Assert.Contains("Register(DebugCenterPageDescriptor descriptor, Func<IDebugCenterPageDrawer> factory)", content);
            Assert.Contains("GetAll()", content);
            Assert.Contains("Find(string id)", content);
            Assert.Contains("CreateAll()", content);
            Assert.Contains("Create(string id)", content);
        }

        [Fact]
        public void R_N3_Hub_Owns_Page_Drawer_Instances_Per_Window()
        {
            Assert.True(File.Exists(HubPath), "MainTabWindow_RimMindHub.cs must exist.");

            string content = File.ReadAllText(HubPath);

            Assert.Contains("DebugCenterPageRegistry", content);
            Assert.Contains("CreateAll()", content);
            Assert.Contains("private readonly IReadOnlyList<IDebugCenterPageDrawer> _pages", content);
            Assert.DoesNotContain("DebugCenterPageRegistry.GetAll()", content);
        }

        [Fact]
        public void R_N3_DebugCenterPageRegistry_Does_Not_Store_Drawer_Instances_Or_SelectedPawn_State()
        {
            Assert.True(File.Exists(RegistryPath), "DebugCenterPageRegistry.cs must exist under Infrastructure/UI/DebugCenter.");

            string content = File.ReadAllText(RegistryPath);

            Assert.DoesNotContain("private static readonly List<IDebugCenterPageDrawer>", content);
            Assert.DoesNotContain("SelectedPawn", content);
            Assert.DoesNotContain("SetSelectedPawnProvider", content);
        }

        [Fact]
        public void R_N3_Hub_Does_Not_Directly_Open_Legacy_Debug_Windows()
        {
            Assert.True(File.Exists(HubPath), "MainTabWindow_RimMindHub.cs must exist.");

            string content = File.ReadAllText(HubPath);

            Assert.DoesNotContain("new Window_AgentFlowLab", content);
            Assert.DoesNotContain("new Window_ToolCallDebug", content);
        }
    }
}
