using Bunit;
using Trailblazor.Docs.Examples;

namespace Trailblazor.Tests.Docs;

// The alert, badge, progress, skeleton and link pages' examples, as the docs show them.
public class FeedbackExampleTests : DocsTest
{
    [Fact]
    public void AlertCloseExample_ShouldBringTheAlertBack_WhenItIsClosedAndShownAgain()
    {
        var cut = Render<AlertCloseExample>();

        cut.Find("button[aria-label='Close the warning']").Click();
        Assert.Empty(cut.FindAll("[role=alert]"));

        Button(cut, "Show the alert again").Click();

        Assert.NotNull(cut.Find("[role=alert]"));
    }

    [Fact]
    public void BadgeMaxExample_ShouldCapTheCountAtEachMax_WhenRendered()
    {
        var cut = Render<BadgeMaxExample>();

        var badges = cut.FindAll("[data-part=badge]");

        Assert.Contains("99+", badges[0].TextContent);
        Assert.Contains("999+", badges[1].TextContent);
    }

    [Fact]
    public void BadgeVisibleExample_ShouldHideTheBadge_WhenAllAreMarkedRead()
    {
        var cut = Render<BadgeVisibleExample>();
        Assert.NotEmpty(cut.FindAll("[data-part=badge]"));

        Button(cut, "Mark all read").Click();

        Assert.Empty(cut.FindAll("[data-part=badge]"));
    }

    [Fact]
    public void ProgressFailedExample_ShouldShowTheMessageWithTheDangerTone_WhenFailed()
    {
        var cut = Render<ProgressFailedExample>();

        Assert.Contains("stopped at 62%", cut.Find("[role=alert]").TextContent);
        Assert.Contains("danger", cut.Find(".tb-progress").ClassName);
    }

    [Fact]
    public void SkeletonWrapExample_ShouldStopBeingBusy_WhenLoadingTurnsOff()
    {
        var cut = Render<SkeletonWrapExample>();
        Assert.Equal("true", cut.Find("[aria-label=Trip]").GetAttribute("aria-busy"));

        cut.Find("input[role=switch]").Change(false);

        Assert.NotEqual("true", cut.Find("[aria-label=Trip]").GetAttribute("aria-busy"));
    }

    [Fact]
    public void BadgeToneExample_ShouldShowEveryTone_WhenRendered()
    {
        var cut = Render<BadgeToneExample>();

        Assert.Equal(Enum.GetValues<Tone>().Length, cut.FindAll("[data-part=badge]").Count);
    }

    [Fact]
    public void LinkButtonExample_ShouldShowTheId_WhenPressed()
    {
        var cut = Render<LinkButtonExample>();

        Button(cut, "Show the batch ID").Click();

        Assert.Contains("2041-B", cut.Markup);
    }
}
