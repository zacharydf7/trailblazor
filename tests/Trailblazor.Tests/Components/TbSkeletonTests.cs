using Bunit;

namespace Trailblazor.Tests.Components;

public class TbSkeletonTests : BunitContext
{
    [Fact]
    public void Render_ShouldBeAHiddenLineOfText_WhenNoParametersAreSet()
    {
        var cut = Render<TbSkeleton>();

        var skeleton = cut.Find("span");
        Assert.Equal(["tb-skeleton", "tb-skeleton--text", "tb-skeleton--pulse"], skeleton.ClassList);
        Assert.Equal("true", skeleton.GetAttribute("aria-hidden"));
        Assert.Null(skeleton.GetAttribute("style"));
    }

    [Fact]
    public void Render_ShouldSizeTheShapeAndKeepTheConsumersStyle_WhenWidthHeightAndStyleAreSet()
    {
        var cut = Render<TbSkeleton>(parameters => parameters
            .Add(skeleton => skeleton.Shape, SkeletonShape.Rectangle)
            .Add(skeleton => skeleton.Width, "100px").Add(skeleton => skeleton.Height, "64px")
            .Add(skeleton => skeleton.Style, "margin: 4px"));

        var skeleton = cut.Find("span");
        Assert.Contains("tb-skeleton--rectangle", skeleton.ClassList);
        Assert.Equal("inline-size: 100px; block-size: 64px; margin: 4px", skeleton.GetAttribute("style"));
    }

    [Theory]
    [InlineData(SkeletonAnimation.Wave, "tb-skeleton--wave")]
    [InlineData(SkeletonAnimation.None, "tb-skeleton--none")]
    public void Render_ShouldMarkTheAnimation_WhenGivenOne(SkeletonAnimation animation, string modifier)
    {
        var cut = Render<TbSkeleton>(parameters => parameters.Add(skeleton => skeleton.Animation, animation));

        var skeleton = cut.Find("span");
        Assert.Contains(modifier, skeleton.ClassList);
        Assert.DoesNotContain("tb-skeleton--pulse", skeleton.ClassList);
    }

    [Fact]
    public void Render_ShouldCoverTheContentAndMarkItBusy_WhenWrappingContentThatIsLoading()
    {
        var cut = Render<TbSkeleton>(parameters => parameters.Add(skeleton => skeleton.Class, "card")
            .AddChildContent("<p>Ada Lovelace</p>"));

        var region = cut.Find(".card");
        Assert.Equal("true", region.GetAttribute("aria-busy"));
        Assert.Contains("tb-skeleton--content", region.ClassList);
        Assert.Equal("Ada Lovelace", region.QuerySelector("[data-part=content] p")!.TextContent);
        Assert.True(region.QuerySelector("[data-part=content]")!.HasAttribute("inert"));
    }

    [Fact]
    public void Render_ShouldKeepTheContentsComponents_WhenLoadingEnds()
    {
        var cut = Render<TbSkeleton>(parameters => parameters.Add(skeleton => skeleton.Class, "card")
            .AddChildContent<TbText>(text => text.AddChildContent("Ada Lovelace")));
        var text = cut.FindComponent<TbText>().Instance;

        cut.Render(parameters => parameters.Add(skeleton => skeleton.Loading, false));

        var region = cut.Find(".card");
        Assert.Null(region.GetAttribute("aria-busy"));
        Assert.Equal(["tb-skeleton-region", "card"], region.ClassList);
        Assert.False(region.QuerySelector("[data-part=content]")!.HasAttribute("inert"));
        Assert.Same(text, cut.FindComponent<TbText>().Instance);
    }

    [Fact]
    public void Render_ShouldSizeWrappedContentOnlyWhileItLoads_WhenGivenAWidthAndHeight()
    {
        var cut = Render<TbSkeleton>(parameters => parameters.Add(skeleton => skeleton.Class, "card")
            .Add(skeleton => skeleton.Width, "200px").Add(skeleton => skeleton.Height, "64px")
            .Add(skeleton => skeleton.Style, "margin: 4px").AddChildContent("<p>Ada Lovelace</p>"));
        Assert.Equal("inline-size: 200px; block-size: 64px; margin: 4px", cut.Find(".card").GetAttribute("style"));

        cut.Render(parameters => parameters.Add(skeleton => skeleton.Loading, false));

        Assert.Equal("margin: 4px", cut.Find(".card").GetAttribute("style"));
    }
}
