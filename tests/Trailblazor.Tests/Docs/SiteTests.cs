using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;
using Trailblazor.Docs.Layout;
using Trailblazor.Docs.Pages;

namespace Trailblazor.Tests.Docs;

public class SiteTests : DocsTest
{
    // The guides, then the components by category, as the sidebar and the home page list them.
    private static string[] Links => [.. GuideCatalog.All.Select(guide => guide.Title),
        .. ExampleCatalog.All.OrderBy(example => example.Category).Select(example => example.Title)];

    private static string[] Paths => [.. GuideCatalog.All.Select(guide => guide.Path),
        .. ExampleCatalog.All.OrderBy(example => example.Category).Select(example => $"components/{example.Name}")];

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void NavMenu_ShouldListEveryGuideAndComponentUnderItsCategory_WhenRendered()
    {
        var cut = Render<NavMenu>();

        Assert.Equal(["Guides", .. Enum.GetNames<ExampleCategory>()], Texts(cut.FindAll("h2")));
        Assert.Equal(Links, Texts(cut.FindAll("a")));
        Assert.Equal(Paths, cut.FindAll("a").Select(link => link.GetAttribute("href")));
    }

    [Fact]
    public void NavMenu_ShouldListUpcomingComponentsUnlinkedWithTheirStage_WhenRendered()
    {
        var upcoming = UpcomingCatalog.Current[0];

        var item = Render<NavMenu>().FindAll("li")
            .Single(item => item.TextContent.Contains(upcoming.Component.Title)
                && item.TextContent.Contains(upcoming.Label));

        Assert.Empty(item.QuerySelectorAll("a"));
        Assert.Equal(upcoming.Label, item.QuerySelector(".docs-nav__stage")!.TextContent);
    }

    [Fact]
    public void MainLayout_ShouldStartWithALinkThatSkipsToTheContent_WhenRendered()
    {
        Navigation.NavigateTo("guides/testing#bunit");
        var cut = Render<MainLayout>();

        var first = cut.FindAll("a[href], button, input").First();

        Assert.Equal("Skip to content", first.TextContent);
        Assert.Equal("guides/testing#main", first.GetAttribute("href"));
        Assert.Equal("-1", cut.Find("main#main").GetAttribute("tabindex"));
    }

    [Fact]
    public void MainLayout_ShouldLeaveOutTheSidebarAndItsToggle_WhenTheHomePageIsOpen()
    {
        var cut = Render<MainLayout>();

        Assert.Empty(cut.FindAll("nav[aria-label=Contents]"));
        Assert.DoesNotContain("Contents", Texts(cut.FindAll("button")));
    }

    [Fact]
    public void MainLayout_ShouldOpenTheSidebarAndCloseItOnTheNextPage_WhenContentsIsPressedOnANarrowScreen()
    {
        Navigation.NavigateTo("guides/getting-started");
        var cut = Render<MainLayout>();

        Button(cut, "Contents").Click();
        Assert.Equal("true", Button(cut, "Contents").GetAttribute("aria-expanded"));
        Assert.Contains("docs-nav--open", cut.Find("nav[aria-label=Contents]").ClassList);
        Navigation.NavigateTo("components/menu");

        Assert.Equal("false", Button(cut, "Contents").GetAttribute("aria-expanded"));
        Assert.DoesNotContain("docs-nav--open", cut.Find("nav[aria-label=Contents]").ClassList);
    }

    [Fact]
    public void Home_ShouldLinkEveryGuideAndComponentWithItsSummary_WhenOpened()
    {
        var catalog = Render<Home>().Find("section[aria-labelledby=components]");

        Assert.Equal("Guides and components", catalog.QuerySelector("#components")!.TextContent.Trim());
        Assert.Equal(Links, Texts(catalog.QuerySelectorAll("a")));
        Assert.Contains(ExampleCatalog.Find("menu")!.Summary, catalog.TextContent);
    }

    [Fact]
    public void SearchBox_ShouldOpenTheResultsForTheQuery_WhenItIsSubmitted()
    {
        var cut = Render<SearchBox>();

        cut.Find("input").Input("placeholder text");
        cut.Find("form").Submit();

        Assert.EndsWith("search?q=placeholder%20text", Navigation.Uri);
    }

    [Fact]
    public void SearchBox_ShouldShowTheQuery_WhenTheResultsPageIsOpenedFromALinkOrByGoingBack()
    {
        Navigation.NavigateTo("search?q=dialog%20options");
        var cut = Render<SearchBox>();
        Assert.Equal("dialog options", cut.Find("input").GetAttribute("value"));

        Navigation.NavigateTo("search?q=menu");

        Assert.Equal("menu", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Search_ShouldListTheComponentsWithAParameterAndLinkTheirApi_WhenItsNameIsSearched()
    {
        Navigation.NavigateTo("search?q=placeholder");
        var cut = Render<Search>();

        Assert.Equal($"{DocsSearch.Find("placeholder").Count} results for “placeholder”.",
            cut.Find("[role=status]").TextContent.Trim());
        var select = cut.FindAll(".docs-results li")
            .Single(item => item.TextContent.Contains("Picks one or several options"));
        Assert.Contains("Has Placeholder", select.TextContent);
        Assert.Equal("components/select/api",
            select.QuerySelectorAll("a").Single(link => link.TextContent == "Placeholder").GetAttribute("href"));
        Assert.Equal("components/select",
            select.QuerySelectorAll("a").Single(link => link.TextContent == "Select").GetAttribute("href"));
    }

    [Fact]
    public void Search_ShouldSaySoAndListNothing_WhenNothingMatches()
    {
        Navigation.NavigateTo("search?q=zebra");
        var cut = Render<Search>();

        Assert.Equal("Nothing matches “zebra”.", cut.Find("[role=status]").TextContent.Trim());
        Assert.Empty(cut.FindAll("li"));
    }
}
