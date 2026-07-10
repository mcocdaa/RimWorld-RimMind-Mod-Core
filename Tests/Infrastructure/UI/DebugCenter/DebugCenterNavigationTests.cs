using RimMind.Infrastructure.UI.DebugCenter;
using Xunit;

namespace RimMind.Tests.Infrastructure.UI.DebugCenter;

public sealed class DebugCenterNavigationTests
{
    [Fact]
    public void GoTo_Stores_Requested_Page_Until_Consumed()
    {
        var navigation = new DebugCenterNavigation();

        navigation.GoTo("ai_requests");

        Assert.Equal("ai_requests", navigation.RequestedPageId);
        Assert.Equal("ai_requests", navigation.ConsumeRequestedPageId());
        Assert.Null(navigation.RequestedPageId);
    }

    [Fact]
    public void GoTo_Replaces_Previous_Request()
    {
        var navigation = new DebugCenterNavigation();

        navigation.GoTo("agents");
        navigation.GoTo("mechanisms");

        Assert.Equal("mechanisms", navigation.ConsumeRequestedPageId());
    }

    [Fact]
    public void PageContext_Exposes_Navigation()
    {
        var navigation = new DebugCenterNavigation();
        var context = new DebugCenterPageContext(selectedPawn: null, navigation);

        context.Navigation.GoTo("tool_calls");

        Assert.Equal("tool_calls", navigation.ConsumeRequestedPageId());
    }
}
