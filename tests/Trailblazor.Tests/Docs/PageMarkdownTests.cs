using Bunit;
using Trailblazor.Docs.Shared;

namespace Trailblazor.Tests.Docs;

public class PageMarkdownTests : DocsTest
{
    [Theory]
    [InlineData(true, "Copied the page as Markdown.")]
    [InlineData(false, "Couldn't copy the page as Markdown.")]
    public void PageMarkdown_ShouldCopyThePagesMarkdownAndSaySo_WhenCopyIsClicked(bool canCopy, string message)
    {
        Browser.CanCopy = canCopy;
        Site.Files["components/select.md"] = "# Select\n";
        var announcer = Render<AnnouncerHost>();
        var cut = Render<PageMarkdown>(parameters => parameters.Add(markdown => markdown.Path, "components/select"));

        Button(cut, "Copy page as Markdown").Click();

        cut.WaitForAssertion(() => Assert.Equal(canCopy ? ["# Select\n"] : [], Browser.Copied));
        announcer.WaitForAssertion(() => Assert.Equal([message],
            announcer.FindAll("[role=status] p").Select(p => p.TextContent)));
    }

    [Fact]
    public void PageMarkdown_ShouldSaySoAndTryAgainOnTheNextClick_WhenThePagesMarkdownDidntLoad()
    {
        var announcer = Render<AnnouncerHost>();
        var cut = Render<PageMarkdown>(parameters => parameters.Add(markdown => markdown.Path, "components/select"));

        Button(cut, "Copy page as Markdown").Click();
        announcer.WaitForAssertion(() => Assert.Equal(["Couldn't copy the page as Markdown."],
            announcer.FindAll("[role=status] p").Select(p => p.TextContent)));
        Assert.Empty(Browser.Copied);
        Site.Files["components/select.md"] = "# Select\n";
        Button(cut, "Copy page as Markdown").Click();

        cut.WaitForAssertion(() => Assert.Equal(["# Select\n"], Browser.Copied));
    }

    [Theory]
    [InlineData("components/select")]
    [InlineData("components/select/api")]
    [InlineData("guides/testing")]
    public void Page_ShouldLinkItsOwnMarkdownInANewTab_WhenOpened(string path)
    {
        var markdown = path.Split('/') switch
        {
            ["components", var name] => ComponentPage(name).Find("ul[aria-label='As Markdown'] a"),
            ["components", var name, "api"] => ApiPage(name).Find("ul[aria-label='As Markdown'] a"),
            _ => PageAt(path).Find("ul[aria-label='As Markdown'] a"),
        };

        Assert.StartsWith("View as Markdown", markdown.TextContent);
        Assert.Equal($"{path}.md", markdown.GetAttribute("href"));
        Assert.Equal("_blank", markdown.GetAttribute("target"));
    }
}
