using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbNavMenuTests : BunitContext
{
    public TbNavMenuTests()
    {
        this.AddTrailblazor();
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<TbNavMenu> RenderMenu() => Render<TbNavMenu>(parameters => parameters
        .AddChildContent<TbNavLink>(link => link.Add(l => l.Href, "").Add(l => l.Match, NavLinkMatch.All)
            .AddChildContent("Home"))
        .AddChildContent<TbNavLink>(link => link.Add(l => l.Href, "trails").AddChildContent("Trails"))
        .AddChildContent<TbNavGroup>(group => group.Add(g => g.Text, "Settings")
            .AddChildContent<TbNavLink>(link => link.Add(l => l.Href, "settings/profile").AddChildContent("Profile"))));

    private string? CurrentLink(IRenderedComponent<TbNavMenu> cut) =>
        cut.FindAll("a[aria-current=page]").SingleOrDefault()?.TextContent.Trim();

    [Theory]
    [InlineData("http://localhost/trails", "http://localhost/trails", NavLinkMatch.All, true)]
    [InlineData("http://localhost/trails/", "http://localhost/trails", NavLinkMatch.All, true)]
    [InlineData("http://localhost/trails?sort=name#top", "http://localhost/trails", NavLinkMatch.All, true)]
    [InlineData("http://localhost/trails/ridge", "http://localhost/trails", NavLinkMatch.All, false)]
    [InlineData("http://localhost/trails/ridge", "http://localhost/trails", NavLinkMatch.Prefix, true)]
    [InlineData("http://localhost/TRAILS", "http://localhost/trails", NavLinkMatch.Prefix, true)]
    [InlineData("http://localhost/trailheads", "http://localhost/trails", NavLinkMatch.Prefix, false)]
    [InlineData("http://localhost/trails", "http://localhost/", NavLinkMatch.All, false)]
    [InlineData("http://localhost/trails", "http://localhost/", NavLinkMatch.Prefix, true)]
    public void IsCurrent_ShouldMatchAsBlazorsNavLinkDoes_WhenGivenAnAddress(string current, string href,
        NavLinkMatch match, bool expected) => Assert.Equal(expected, TbNavLink.IsCurrent(current, href, match));

    [Fact]
    public void Render_ShouldListItsLinksAndMarkTheCurrentPage_WhenCalled()
    {
        Navigation.NavigateTo("trails/ridge-loop");

        var cut = RenderMenu();

        Assert.Equal(("UL", "list"), (cut.Find("[data-part=nav-menu]").TagName,
            cut.Find("[data-part=nav-menu]").GetAttribute("role")));
        Assert.Equal(["", "trails", "settings/profile"], cut.FindAll("a[data-part=link]")
            .Select(link => link.GetAttribute("href")));
        Assert.Equal("Trails", CurrentLink(cut));
        Assert.Contains("tb-nav-menu__link--current", cut.Find("a[aria-current]").ClassList);
    }

    [Fact]
    public void Render_ShouldMoveTheCurrentMark_WhenTheAddressChanges()
    {
        var cut = RenderMenu();
        Assert.Equal("Home", CurrentLink(cut));

        Navigation.NavigateTo("trails");

        cut.WaitForAssertion(() => Assert.Equal("Trails", CurrentLink(cut)));
    }

    [Fact]
    public void Group_ShouldOpenItsLinks_WhenItsButtonIsPressed()
    {
        var changes = new List<bool>();
        var cut = Render<TbNavMenu>(parameters => parameters.AddChildContent<TbNavGroup>(group => group
            .Add(g => g.Text, "Settings").Add(g => g.ExpandedChanged, (bool expanded) => changes.Add(expanded))
            .AddChildContent<TbNavLink>(link => link.Add(l => l.Href, "settings/profile").AddChildContent("Profile"))));
        var toggle = cut.Find("[data-part=group-toggle]");

        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.True(cut.Find("[data-part=group-items]").HasAttribute("hidden"));
        Assert.Equal(cut.Find("[data-part=group-items]").Id, toggle.GetAttribute("aria-controls"));
        toggle.Click();

        Assert.Equal("true", cut.Find("[data-part=group-toggle]").GetAttribute("aria-expanded"));
        Assert.False(cut.Find("[data-part=group-items]").HasAttribute("hidden"));
        Assert.Equal([true], changes);
    }

    [Fact]
    public void Group_ShouldOpenByItself_WhenItHoldsTheCurrentPage()
    {
        Navigation.NavigateTo("settings/profile");

        var cut = RenderMenu();

        Assert.Equal("true", cut.Find("[data-part=group-toggle]").GetAttribute("aria-expanded"));
        Assert.Equal("Profile", CurrentLink(cut));
    }

    [Fact]
    public void Group_ShouldClose_WhenItsButtonIsPressedWhileItHoldsTheCurrentPage()
    {
        Navigation.NavigateTo("settings/profile");
        var changes = new List<bool>();
        var cut = Render<TbNavMenu>(parameters => parameters.AddChildContent<TbNavGroup>(group => group
            .Add(g => g.Text, "Settings").Add(g => g.ExpandedChanged, (bool expanded) => changes.Add(expanded))
            .AddChildContent<TbNavLink>(link => link.Add(l => l.Href, "settings/profile").AddChildContent("Profile"))));
        cut.WaitForAssertion(() => Assert.Equal([true], changes));

        cut.Find("[data-part=group-toggle]").Click();

        Assert.Equal("false", cut.Find("[data-part=group-toggle]").GetAttribute("aria-expanded"));
        Assert.True(cut.Find("[data-part=group-items]").HasAttribute("hidden"));
        Assert.Equal([true, false], changes);
    }

    [Fact]
    public void Group_ShouldPutItsClassStyleAndAttributesOnItsListItem_WhenTheyArePassed()
    {
        var cut = Render<TbNavMenu>(parameters => parameters.AddChildContent<TbNavGroup>(group => group
            .Add(g => g.Text, "Settings").Add(g => g.Class, "settings").Add(g => g.Style, "color: teal")
            .AddUnmatched("data-kind", "account")));

        var item = cut.Find("li[data-part=nav-group]");

        Assert.Contains("settings", item.ClassList);
        Assert.Equal(("color: teal", "account"), (item.GetAttribute("style"), item.GetAttribute("data-kind")));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAGroupIsOutsideAMenu()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbNavGroup>(parameters => parameters
            .Add(group => group.Text, "Settings")));

        Assert.Contains(nameof(TbNavMenu), thrown.Message);
    }

    [Fact]
    public void Render_ShouldPutAnItemsClassStyleAndAttributesOnItsListItem_WhenTheyArePassed()
    {
        var cut = Render<TbNavMenu>(parameters => parameters.AddChildContent<TbNavLink>(link => link
            .Add(l => l.Href, "trails").Add(l => l.Class, "trails").Add(l => l.Style, "color: teal")
            .AddUnmatched("data-kind", "main").AddChildContent("Trails")));

        var item = cut.Find("li");

        Assert.Contains("trails", item.ClassList);
        Assert.Equal(("color: teal", "main"), (item.GetAttribute("style"), item.GetAttribute("data-kind")));
    }

    [Fact]
    public void Render_ShouldThrow_WhenALinkIsOutsideAMenu()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbNavLink>(parameters => parameters
            .Add(link => link.Href, "trails")));

        Assert.Contains(nameof(TbNavMenu), thrown.Message);
    }
}
