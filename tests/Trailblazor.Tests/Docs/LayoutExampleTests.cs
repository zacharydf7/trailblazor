using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

// The layout pages' examples, as the docs show them.
public class LayoutExampleTests : DocsTest
{
    [Fact]
    public void DividerContentExample_ShouldRenderPlainDivsAroundItsContent_WhenRendered()
    {
        var cut = Render<DividerContentExample>();

        Assert.Empty(cut.FindAll("hr"));
        Assert.Equal(["or", "Billing"], cut.FindAll("[data-part=content]").Select(part => part.TextContent.Trim()));
    }

    [Fact]
    public void DividerVerticalExample_ShouldBeAVerticalSeparator_WhenRendered()
    {
        var cut = Render<DividerVerticalExample>();

        Assert.Equal("vertical", cut.Find("[role=separator]").GetAttribute("aria-orientation"));
    }

    [Fact]
    public void SurfaceElementExample_ShouldRenderTheElementsItAsksFor_WhenRendered()
    {
        var cut = Render<SurfaceElementExample>();

        Assert.NotNull(cut.Find("article"));
        Assert.Equal("Trip summary", cut.Find("section").GetAttribute("aria-label"));
    }

    [Fact]
    public void StackElementExample_ShouldRenderANamedNavOfLinks_WhenRendered()
    {
        var cut = Render<StackElementExample>();

        Assert.Equal("Layout pages", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal(["components/stack", "components/grid", "components/spacer"],
            cut.FindAll("ul > li a").Select(link => link.GetAttribute("href")));
    }

    [Fact]
    public void GridListExample_ShouldRenderAListOfArticles_WhenRendered()
    {
        var cut = Render<GridListExample>();

        Assert.Equal(3, cut.FindAll("ul > li article").Count);
    }

    [Fact]
    public void GridNestedExample_ShouldShowTwoColumnsInTheInnerGrid_WhenRendered()
    {
        var cut = Render<GridNestedExample>();

        Assert.Equal(2, cut.FindAll(".tb-grid .tb-grid .tb-grid-item").Count);
    }

    [Fact]
    public void ContainerElementExample_ShouldRenderANamedSection_WhenRendered()
    {
        var cut = Render<ContainerElementExample>();

        Assert.Equal("Trip summary", cut.Find("section.tb-container").GetAttribute("aria-label"));
    }

    [Fact]
    public void ContainerExample_ShouldShowEveryWidth_WhenRendered()
    {
        var cut = Render<ContainerExample>();

        Assert.Equal(Enum.GetValues<ContainerWidth>().Length, cut.FindAll(".tb-container").Count);
    }

    [Theory]
    [InlineData("surface", "LayoutElement"), InlineData("stack", "RowFrom"), InlineData("grid", "Offset")]
    [InlineData("grid", "GridSpan"), InlineData("container", "LayoutElement.Main")]
    public void ComponentPage_ShouldKeepFeaturesOutOfAccessibility_WhenShowingALayout(string example, string feature)
    {
        var cut = ComponentPage(example);

        Assert.DoesNotContain(feature, cut.Find("ul[aria-label=Accessibility]").TextContent);
    }

    [Theory]
    [InlineData("640px"), InlineData("768px"), InlineData("1024px"), InlineData("1280px")]
    public void ComponentPage_ShouldGiveTheBreakpointWidth_WhenShowingTheGrid(string width)
    {
        var cut = ComponentPage("grid");

        Assert.Contains(width, cut.Markup);
    }
}
