using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Snapshots;

public sealed class SettingsPageSnapshotTests
{
    [Fact]
    public void SettingsApiSnapshot_HasTabsAndScrollableForm()
    {
        var tabs = new[]
        {
            new TabbedPageTabModel("api", "API", "RimMind.Settings.Tab.Api", true, true, null),
            new TabbedPageTabModel("queue", "Queue", "RimMind.Settings.Tab.Queue", false, true, null),
            new TabbedPageTabModel("prompts", "Prompts", "RimMind.Settings.Tab.Prompts", false, true, null),
            new TabbedPageTabModel("context", "Context", "RimMind.Settings.Tab.Context", false, true, null)
        };
        TabbedPageLayoutResult tabLayout = TabbedPageLayout.Calculate(new Rect(0f, 0f, 780f, 580f), tabs);
        FormPageLayoutResult form = FormPageLayout.Calculate(tabLayout.Content, 5, 4);

        Assert.Equal(1, tabLayout.RowCount);
        Assert.True(form.ContentHeight > 0f);
        Assert.True(tabLayout.Content.y > tabLayout.TabBar.yMax);
    }
}
