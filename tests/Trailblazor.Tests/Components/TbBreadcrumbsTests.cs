using Bunit;
using Microsoft.AspNetCore.Components;

namespace Trailblazor.Tests.Components;

public class TbBreadcrumbsTests : BunitContext
{
    public TbBreadcrumbsTests()
    {
        this.AddTrailblazor();
    }

    private IRenderedComponent<TbBreadcrumbs> RenderTrail(Action<ComponentParameterCollectionBuilder<TbBreadcrumbs>>?
        configure = null) => Render<TbBreadcrumbs>(parameters =>
    {
        configure?.Invoke(parameters);
        parameters.AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Href, "/").AddChildContent("Home"))
            .AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Href, "/trails").AddChildContent("Trails"))
            .AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Current, true)
                .AddChildContent("Ridge loop"));
    });

    [Fact]
    public void Render_ShouldListTheLinksAndMarkTheCurrentPage_WhenGivenATrail()
    {
        var cut = RenderTrail();

        var nav = cut.Find("nav");
        var items = cut.FindAll("[data-part=breadcrumb]");

        Assert.Equal(("breadcrumbs", "Breadcrumb"), (nav.GetAttribute("data-part"), nav.GetAttribute("aria-label")));
        Assert.Equal(("OL", "list"), (cut.Find("nav > *").TagName, cut.Find("nav > *").GetAttribute("role")));
        Assert.Equal(["LI", "LI", "LI"], items.Select(item => item.TagName));
        Assert.Equal(["/", "/trails"], cut.FindAll("a[data-part=link]").Select(link => link.GetAttribute("href")));
        Assert.Empty(cut.FindAll("a[aria-current]"));
        var current = cut.Find("[aria-current]");
        Assert.Equal(("text", "page", "Ridge loop"),
            (current.GetAttribute("data-part"), current.GetAttribute("aria-current"), current.TextContent));
        Assert.Contains("tb-breadcrumbs__item--current", items[2].ClassList);
    }

    [Fact]
    public void Render_ShouldMarkTheLinkAsTheCurrentPage_WhenTheCurrentItemHasAnAddress()
    {
        var cut = Render<TbBreadcrumbs>(parameters => parameters
            .AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Href, "/").AddChildContent("Home"))
            .AddChildContent<TbBreadcrumb>(item => item.Add(crumb => crumb.Href, "/trails")
                .Add(crumb => crumb.Current, true).AddChildContent("Trails")));

        var current = cut.Find("[aria-current=page]");

        Assert.Equal(("A", "/trails"), (current.TagName, current.GetAttribute("href")));
    }

    [Fact]
    public void Render_ShouldShowPlainText_WhenAnItemHasNoAddress()
    {
        var cut = Render<TbBreadcrumbs>(parameters => parameters
            .AddChildContent<TbBreadcrumb>(item => item.AddChildContent("Settings")));

        var text = cut.Find("[data-part=text]");

        Assert.Equal(("SPAN", null, "Settings"), (text.TagName, text.GetAttribute("aria-current"), text.TextContent));
        Assert.Empty(cut.FindAll("a"));
    }

    [Fact]
    public void Render_ShouldHideEachSeparatorFromScreenReaders_WhenCalled()
    {
        var cut = RenderTrail();

        var separators = cut.FindAll("[data-part=separator]");

        Assert.NotEmpty(separators);
        Assert.All(separators, separator => Assert.Equal("true", separator.GetAttribute("aria-hidden")));
    }

    [Fact]
    public void Render_ShouldMirrorTheDefaultChevronInRightToLeftText_WhenCalled()
    {
        var chevrons = RenderTrail().FindAll("[data-part=separator] svg");

        Assert.NotEmpty(chevrons);
        Assert.All(chevrons, chevron => Assert.Contains("tb-icon--mirrored", chevron.ClassList));
    }

    [Fact]
    public void Render_ShouldPutAnItemsClassStyleAndAttributesOnItsListItem_WhenTheyArePassed()
    {
        var cut = Render<TbBreadcrumbs>(parameters => parameters.AddChildContent<TbBreadcrumb>(item => item
            .Add(crumb => crumb.Href, "/").Add(crumb => crumb.Class, "home").Add(crumb => crumb.Style, "color: teal")
            .AddUnmatched("data-kind", "root").AddChildContent("Home")));

        var listItem = cut.Find("li");
        Assert.Contains("home", listItem.ClassList);
        Assert.Equal("color: teal", listItem.GetAttribute("style"));
        Assert.Equal("root", listItem.GetAttribute("data-kind"));
    }

    [Fact]
    public void Render_ShouldPutTheGivenSeparatorAndLabelOnTheTrail_WhenTheyAreSet()
    {
        RenderFragment slash = builder => builder.AddContent(0, "/");
        var cut = RenderTrail(parameters => parameters.Add(trail => trail.Separator, slash)
            .Add(trail => trail.AriaLabel, "You are here"));

        Assert.Equal("You are here", cut.Find("nav").GetAttribute("aria-label"));
        Assert.All(cut.FindAll("[data-part=separator]"), separator => Assert.Equal("/", separator.TextContent.Trim()));
    }

    [Fact]
    public void Render_ShouldThrow_WhenABreadcrumbIsOutsideATrail()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbBreadcrumb>());

        Assert.Contains(nameof(TbBreadcrumbs), thrown.Message);
    }
}
