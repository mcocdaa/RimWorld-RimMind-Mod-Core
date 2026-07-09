using System.Linq;
using RimMind.Infrastructure.UI.DebugCenter;
using Xunit;

namespace RimMind.Tests.Infrastructure.UI.DebugCenter;

public sealed class DebugCenterPageRegistryTests
{
    [Fact]
    public void DefaultPageId_IsOverview()
    {
        Assert.Equal("overview", DebugCenterPageRegistry.DefaultPageId);
    }

    [Fact]
    public void Registry_IncludesSettingsEntryAfterContextKeys()
    {
        var pages = DebugCenterPageRegistry.GetAll();

        Assert.Contains(pages, page => page.Id == "settings");
        Assert.True(
            pages.Single(page => page.Id == "settings").Order >
            pages.Single(page => page.Id == "context_keys").Order);
    }

    [Fact]
    public void Create_SettingsPage_ReturnsDrawer()
    {
        Assert.NotNull(DebugCenterPageRegistry.Create("settings"));
    }
}
