using Bunit;

namespace Trailblazor.Tests.Components;

public class TbSkeletonTests : BunitContext
{
    [Fact]
    public void Render_ShouldBeAHiddenLineOfText_WhenNoParametersAreSet()
    {
        var cut = Render<TbSkeleton>();

        var skeleton = cut.Find("span");
        Assert.Equal(["tb-skeleton", "tb-skeleton--text"], skeleton.ClassList);
        Assert.Equal("true", skeleton.GetAttribute("aria-hidden"));
        Assert.Null(skeleton.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldSizeTheShapeAndKeepTheConsumersStyle_WhenWidthHeightAndStyleAreSet()
    {
        var cut = Render<TbSkeleton>(parameters => parameters.Add(skeleton => skeleton.Shape, SkeletonShape.Rectangle)
            .Add(skeleton => skeleton.Width, "100px").Add(skeleton => skeleton.Height, "64px")
            .Add(skeleton => skeleton.Style, "margin: 4px"));

        var skeleton = cut.Find("span");
        Assert.Contains("tb-skeleton--rectangle", skeleton.ClassList);
        Assert.Equal("inline-size: 100px; block-size: 64px; margin: 4px", skeleton.GetAttribute("style"));
    }
}
