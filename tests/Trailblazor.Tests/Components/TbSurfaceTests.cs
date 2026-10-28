using Bunit;

namespace Trailblazor.Tests.Components;

public class TbSurfaceTests : BunitContext
{
    [Fact]
    public void Render_ShouldUseLowElevation_WhenNoParametersAreSet()
    {
        var cut = Render<TbSurface>(parameters => parameters.AddChildContent("Content"));

        var surface = cut.Find(".tb-surface");
        Assert.Contains("tb-surface--low", surface.ClassList);
        Assert.Equal("Content", surface.TextContent.Trim());
    }

    [Fact]
    public void Render_ShouldAddElevationAndOutlineClasses_WhenTheyAreSet()
    {
        var cut = Render<TbSurface>(parameters => parameters
            .Add(surface => surface.Elevation, Elevation.High)
            .Add(surface => surface.Outlined, true));

        var surface = cut.Find(".tb-surface");
        Assert.Contains("tb-surface--high", surface.ClassList);
        Assert.Contains("tb-surface--outlined", surface.ClassList);
    }
}
