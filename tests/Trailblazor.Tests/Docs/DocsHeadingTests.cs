using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Docs.Examples;
using Trailblazor.Docs.Shared;

namespace Trailblazor.Tests.Docs;

public class DocsHeadingTests : DocsTest
{
    public static TheoryData<string> Pages => new(GuideCatalog.All.Select(guide => guide.Path).Concat(ExampleCatalog.All
        .SelectMany(example => new[] { $"components/{example.Name}", $"components/{example.Name}/api" })));

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void DocsHeading_ShouldLinkToItsSectionBesideTheHeading_WhenRendered()
    {
        Navigation.NavigateTo("guides/testing#bunit");

        var cut = Render<DocsHeading>(parameters => parameters.Add(heading => heading.Id, "playwright")
            .Add(heading => heading.Text, "Playwright").Add(heading => heading.Class, "docs-section"));

        var heading = cut.Find(".docs-heading.docs-heading--h2.docs-section > h2#playwright");
        var link = cut.Find(".docs-heading > a");
        Assert.Equal("Playwright", heading.TextContent.Trim());
        Assert.Empty(heading.QuerySelectorAll("a"));
        Assert.Equal("guides/testing#playwright", link.GetAttribute("href"));
        Assert.Equal("Link to Playwright", link.GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(true, "Copied the link to Playwright.")]
    [InlineData(false, "Couldn't copy the link to Playwright.")]
    public void DocsHeading_ShouldCopyTheAddressOfItsSectionAndSaySo_WhenItsLinkIsClicked(bool canCopy, string message)
    {
        Browser.CanCopy = canCopy;
        Navigation.NavigateTo("guides/testing");
        var announcer = Render<AnnouncerHost>();
        var cut = Render<DocsHeading>(parameters => parameters.Add(heading => heading.Id, "playwright")
            .Add(heading => heading.Text, "Playwright"));

        cut.Find("a").Click();

        Assert.Equal(canCopy ? ["http://localhost/guides/testing#playwright"] : [], Browser.Copied);
        announcer.WaitForAssertion(() => Assert.Equal([message],
            announcer.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Theory, MemberData(nameof(Pages))]
    public void Page_ShouldLinkEachAnchoredHeadingToItsSection_WhenOpened(string path)
    {
        const string selector = ".docs-article__body h2[id], .docs-article__body h3[id], .docs-article__body h4[id]";

        var headings = path.Split('/') switch
        {
            ["components", var name] => ComponentPage(name).FindAll(selector),
            ["components", var name, "api"] => ApiPage(name).FindAll(selector),
            _ => PageAt(path).FindAll(selector),
        };

        // A demo's headings belong to the example, not to the page.
        var own = headings.Where(heading => heading.Closest(".docs-example__demo") is null).ToList();
        Assert.NotEmpty(own);
        Assert.All(own, heading => Assert.EndsWith($"#{heading.Id}",
            heading.NextElementSibling is { ClassName: "docs-heading__link" } link ? link.GetAttribute("href") : ""));
    }
}
