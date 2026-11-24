using Bunit;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

// Only AddTrailblazor, as an app's test has, so the viewport's script answers each call with a default.
public class TbViewportWithoutScriptTests : BunitContext
{
    public TbViewportWithoutScriptTests() => this.AddTrailblazor();

    [Fact]
    public async Task Zoom_ShouldBeTheOneAskedFor_WhenTheScriptAnswersWithADefault()
    {
        var cut = Render<TbViewport>(parameters => parameters.Add(viewport => viewport.Label, "Trail map")
            .Add(viewport => viewport.Zoom, 2).AddChildContent("<img src=\"map.png\" alt=\"Map of the ridge\">"));
        var map = cut.Tb().Viewport("Trail map");
        Assert.Equal(2, map.Zoom);

        await map.ZoomInAsync();
        await map.FitAsync(ViewportFit.Width);

        Assert.Equal((2.5, ViewportFit.Width), (map.Zoom, map.CurrentFit));
    }
}
