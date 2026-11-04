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
    public void MainLayout_ShouldLeaveOutTheSidebarAndMenuOnlyTheHeader_WhenTheHomePageIsOpen()
    {
        var cut = Render<MainLayout>();

        Assert.Empty(cut.FindAll("nav[aria-label=Contents]"));
        Assert.Equal("docs-site-nav docs-look", Button(cut, "Menu").GetAttribute("aria-controls"));
    }

    [Fact]
    public void MainLayout_ShouldOpenTheSiteLinksTheLookAndTheContentsAndCloseThemOnTheNextPage_WhenMenuIsPressed()
    {
        Navigation.NavigateTo("guides/getting-started");
        var cut = Render<MainLayout>();
        var controls = Button(cut, "Menu").GetAttribute("aria-controls")!.Split(' ');

        Button(cut, "Menu").Click();
        Assert.Equal(["docs-site-nav", "docs-look", "docs-nav"], controls);
        Assert.All(controls, id => cut.Find($"#{id}"));
        Assert.Equal("true", Button(cut, "Menu").GetAttribute("aria-expanded"));
        Assert.Contains("docs-header__inner--menu", cut.Find(".docs-header__inner").ClassList);
        Assert.Contains("docs-nav--open", cut.Find("nav[aria-label=Contents]").ClassList);
        Navigation.NavigateTo("components/menu");

        Assert.Equal("false", Button(cut, "Menu").GetAttribute("aria-expanded"));
        Assert.DoesNotContain("docs-header__inner--menu", cut.Find(".docs-header__inner").ClassList);
        Assert.DoesNotContain("docs-nav--open", cut.Find("nav[aria-label=Contents]").ClassList);
    }

    [Fact]
    public void MainLayout_ShouldOpenAndFocusTheSearchInPlaceOfTheMenu_WhenSearchIsPressed()
    {
        Navigation.NavigateTo("guides/getting-started");
        var cut = Render<MainLayout>();
        Button(cut, "Menu").Click();

        Button(cut, "Search").Click();

        Assert.Equal("docs-search", Button(cut, "Search").GetAttribute("aria-controls"));
        Assert.Equal("true", Button(cut, "Search").GetAttribute("aria-expanded"));
        Assert.Equal("false", Button(cut, "Menu").GetAttribute("aria-expanded"));
        Assert.Equal(["docs-header__inner", "docs-header__inner--search"], cut.Find(".docs-header__inner").ClassList);
        JSInterop.VerifyFocusAsyncInvoke().Arguments[0].ShouldBeElementReferenceTo(cut.Find("#docs-search input"));
        Navigation.NavigateTo("components/menu");
        Assert.Equal("false", Button(cut, "Search").GetAttribute("aria-expanded"));
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
    public void Search_ShouldGroupTheMatchesByPageAndLinkEachMembersRow_WhenAParameterIsSearched()
    {
        Navigation.NavigateTo("search?q=placeholder");
        var cut = Render<Search>();
        var results = DocsSearch.Find("placeholder");
        var pages = results.Select(result => result.Page).Distinct().ToList();

        Assert.Equal($"{pages.Count} pages match “placeholder”.", cut.Find("[role=status]").TextContent.Trim());
        var items = cut.FindAll(".docs-results > li");
        Assert.Equal(pages, items.Select(item => item.QuerySelector(".docs-card__title")!.TextContent));
        var select = items.Single(item => item.QuerySelector(".docs-card__title")!.TextContent == "Select");
        Assert.Equal("components/select", select.QuerySelector(".docs-card__title")!.GetAttribute("href"));
        var member = select.QuerySelectorAll(".docs-results__matches > li")
            .Single(match => match.QuerySelector("a code")?.TextContent == "TbSelect<TValue>.Placeholder");
        Assert.Equal("components/select/api#tbselect-placeholder", member.QuerySelector("a")!.GetAttribute("href"));
        Assert.Equal(results.Single(result => result.Path == "components/select/api#tbselect-placeholder").Snippet,
            member.QuerySelector(".docs-note")!.TextContent);
    }

    [Fact]
    public void Search_ShouldShowAPagesFirstFourMatchesAndCountTheRest_WhenItHasMore()
    {
        Navigation.NavigateTo("search?q=dialog");
        var cut = Render<Search>();
        var dialog = DocsSearch.Find("dialog").Where(result => result.Page == "Dialog").ToList();

        var item = cut.FindAll(".docs-results > li").First();

        Assert.Equal(dialog.Take(4).Select(result => result.Section ?? result.Snippet), item
            .QuerySelectorAll(".docs-results__matches > li")
            .Select(match => match.QuerySelector("a")?.TextContent.Trim() ?? match.TextContent.Trim()));
        Assert.Equal($"{dialog.Count - 4} more matches on Dialog.", item.Children.Last().TextContent.Trim());
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
