using System;
using System.IO;
using System.Linq;
using RimMind.Infrastructure.UI.DebugCenter;
using Xunit;

namespace RimMind.Tests.Infrastructure.UI.DebugCenter;

public sealed class DebugCenterDescriptorConsistencyTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static readonly string CoreSourceRoot = Path.Combine(ProjectRoot, "Source");

    [Fact]
    public void Registry_Should_Have_Exactly_One_Default_Page()
    {
        var defaultPages = DebugCenterPageRegistry.GetAll()
            .Where(page => page.IsDefault)
            .ToList();

        var defaultPage = Assert.Single(defaultPages);
        Assert.Equal("overview", defaultPage.Id);
    }

    [Fact]
    public void CreateAllRegistrations_Should_Preserve_Registry_Descriptor()
    {
        var descriptors = DebugCenterPageRegistry.GetAll();
        var registrations = DebugCenterPageRegistry.CreateAllRegistrations();

        Assert.Equal(descriptors.Count, registrations.Count);

        foreach (var descriptor in descriptors)
        {
            var registration = Assert.Single(
                registrations,
                candidate => candidate.Descriptor.Id == descriptor.Id);

            Assert.Equal(descriptor, registration.Descriptor);
            Assert.NotNull(registration.CreateDrawer());
        }
    }

    [Fact]
    public void AIRequests_Drawer_Should_Not_Declare_Default_Itself()
    {
        string source = ReadCoreSource("Infrastructure/UI/DebugCenter/Pages/AIRequestsDebugCenterPageDrawer.cs");

        Assert.DoesNotContain("IsDefault: true", source);
    }

    private static string ReadCoreSource(string relativePath)
    {
        string path = Path.Combine(CoreSourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }
}
