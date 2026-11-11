using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

public class RenderModesTests : DocsTest
{
    [Fact]
    public void Modes_ShouldSayItAllWorks_WhenTheComponentIsPlainHtml()
    {
        var plain = ExampleCatalog.All
            .Where(example => example.BeforeInteractive.SequenceEqual([ExampleCatalog.PlainHtml])).ToList();

        Assert.NotEmpty(plain);
        Assert.Empty(plain.Where(example => example.Modes != new RenderModes(ModeSupport.Full, ModeSupport.Full))
            .Select(example => example.Name));
    }

    [Fact]
    public void Modes_ShouldNeverWorkBetterOnAPrerenderedPageThanAStaticOne_WhenCalled()
    {
        var wrong = ExampleCatalog.All.Where(example => example.Modes.BeforeInteractive < example.Modes.Static)
            .Select(example => example.Name);

        Assert.Empty(wrong);
    }

    [Fact]
    public void GettingStarted_ShouldShowWhatWorksForEachComponent_WhenOpened()
    {
        var cut = PageAt("guides/getting-started");

        var rows = cut.FindAll("[aria-label='What works before the page is interactive'] tbody tr");

        Assert.Equal(ExampleCatalog.All.Select(example => example.Title), Texts(rows.Select(row => row.Children[0])));
        Assert.Equal(["Switch", "Works", "Partly"],
            Texts(rows.Single(row => row.Children[0].TextContent.Trim() == "Switch").Children));
        Assert.Equal(["Toast", "Not shown", "Not shown"],
            Texts(rows.Single(row => row.Children[0].TextContent.Trim() == "Toast").Children));
    }

    [Fact]
    public void ComponentPage_ShouldSayHowMuchWorksBeforeThePageIsInteractive_WhenOpened()
    {
        var cut = ComponentPage("carousel");

        Assert.Equal("On a static page: Display only. On a prerendered page before it turns interactive: Display only. "
            + "See every component's render modes.", Normalized(cut.Find("[data-testid=render-modes]").TextContent));
    }

    private static string Normalized(string text) => string.Join(' ', text.Split((char[]?)null,
        StringSplitOptions.RemoveEmptyEntries));
}
