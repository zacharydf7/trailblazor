using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

// The overlay, focus scope and menu pages' examples, as the docs show them.
public class OverlayExampleTests : DocsTest
{
    [Fact]
    public void OverlayClickExample_ShouldCountAClickOnTheOverlay_WhenTheMapIsLocked()
    {
        var cut = Render<OverlayClickExample>();
        Assert.Empty(cut.FindAll("[data-part=overlay]"));

        cut.Find("input[role=switch]").Change(true);
        cut.Find("[data-part=scrim]").Click();

        Assert.Contains("Clicks caught: 1", cut.Find("output").TextContent);
    }

    [Fact]
    public void OverlayContainedExample_ShouldShowAndHideTheOverlay_WhenItsButtonIsPressed()
    {
        var cut = Render<OverlayContainedExample>();

        Button(cut, "Load the report").Click();
        Assert.NotNull(cut.Find("[data-part=overlay]"));

        Button(cut, "Finish loading").Click();
        Assert.Empty(cut.FindAll("[data-part=overlay]"));
    }

    [Fact]
    public void FocusScopeAutoFocusExample_ShouldShowEachPanel_WhenItsButtonIsPressed()
    {
        var cut = Render<FocusScopeAutoFocusExample>();

        Button(cut, "Focus the marked field").Click();
        Assert.NotNull(cut.Find("[aria-label='Your name']"));

        Button(cut, "Leave focus where it is").Click();
        Assert.NotNull(cut.Find("[aria-label='Your town']"));
        Assert.Empty(cut.FindAll("[aria-label='Your name']"));
    }

    [Fact]
    public void MenuOpenExample_ShouldOpenTheMenu_WhenItsButtonIsPressed()
    {
        var cut = Render<MenuOpenExample>();
        Assert.Empty(cut.FindAll("[role=menu][data-open]"));

        Button(cut, "Show the saved searches").Click();

        Assert.NotNull(cut.Find("[role=menu][data-open]"));
    }
}
