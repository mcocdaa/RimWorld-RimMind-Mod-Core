using RimMind.Presentation.UI.Framework;
using UnityEngine;
using Xunit;

namespace RimMind.Tests.Presentation.UI.Framework;

public sealed class FormPageLayoutTests
{
    [Fact]
    public void CalculateRows_ComputesContentHeightBeyondViewport()
    {
        var layout = FormPageLayout.Calculate(new Rect(0f, 0f, 500f, 200f), sectionCount: 4, rowsPerSection: 6);

        Assert.True(layout.ContentHeight > layout.Viewport.height);
        Assert.Equal(4, layout.Sections.Count);
        Assert.All(layout.Sections, s => Assert.True(s.Header.height > 0f));
    }
}
