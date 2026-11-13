using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Trailblazor.Tests.Components;

public class TbTooltipTests : BunitContext
{
    public TbTooltipTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldWrapTheTriggerAndHoldAClosedTooltipWithoutCallingAScript_WhenCalled()
    {
        var cut = Render<TbTooltip>(parameters => parameters
            .Add(tooltip => tooltip.Text, "Delete the selection")
            .AddChildContent("<button>Delete</button>"));

        var tooltip = cut.Find("[role=tooltip]");
        Assert.Equal("manual", tooltip.GetAttribute("popover"));
        Assert.Equal("Delete the selection", tooltip.TextContent);
        Assert.NotNull(cut.Find(".tb-tooltip-anchor > button"));
        Assert.Empty(JSInterop.Invocations);
    }

    [Theory]
    [InlineData(Placement.Top, PlacementAlign.Center, "Top", "Center")]
    [InlineData(Placement.Bottom, PlacementAlign.Start, "Bottom", "Start")]
    [InlineData(Placement.Start, PlacementAlign.End, "Start", "End")]
    [InlineData(Placement.End, PlacementAlign.Center, "End", "Center")]
    public void Render_ShouldSetUpTheTooltipOnTheRequestedSide_WhenGivenAPlacementAndAlignment(Placement placement,
        PlacementAlign align, string side, string alignment)
    {
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete")
            .Add(tooltip => tooltip.Placement, placement).Add(tooltip => tooltip.Align, align));

        var bubble = cut.Find("[role=tooltip]");
        Assert.Equal((side, alignment), (bubble.GetAttribute("data-placement"), bubble.GetAttribute("data-align")));
    }

    [Fact]
    public void Render_ShouldCenterTheTooltip_WhenNoAlignmentIsGiven()
    {
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete"));

        Assert.Equal("Center", cut.Find("[role=tooltip]").GetAttribute("data-align"));
    }

    [Fact]
    public void Render_ShouldShowTheContentInsteadOfTheText_WhenGivenContent()
    {
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Save")
            .Add(tooltip => tooltip.TextContent, "<strong>Save</strong> <kbd>Ctrl+S</kbd>"));

        var tooltip = cut.Find("[role=tooltip]");
        Assert.Equal("Ctrl+S", tooltip.QuerySelector("kbd")!.TextContent);
        Assert.Equal("Save Ctrl+S", tooltip.TextContent.Trim());
    }

    [Theory]
    [InlineData(" ", false)]
    [InlineData("Delete the selection", true)]
    public void Render_ShouldLeaveTheTriggerAlone_WhenTheTextIsBlankOrTheTooltipIsDisabled(string text, bool disabled)
    {
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, text)
            .Add(tooltip => tooltip.Disabled, disabled).AddChildContent("<button>Delete</button>"));

        Assert.NotNull(cut.Find(".tb-tooltip-anchor > button"));
        Assert.Empty(cut.FindAll("[role=tooltip]"));
    }

    [Fact]
    public void Render_ShouldStyleAndSizeTheBubble_WhenGivenABubbleClassAndMaxWidth()
    {
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete the selection")
            .Add(tooltip => tooltip.Class, "trigger").Add(tooltip => tooltip.BubbleClass, "wide-tip")
            .Add(tooltip => tooltip.MaxWidth, "24rem").AddChildContent("<button>Delete</button>"));

        var bubble = cut.Find("[role=tooltip]");
        Assert.Equal(["tb-tooltip", "wide-tip"], bubble.ClassList);
        Assert.Null(bubble.GetAttribute("style"));
        Assert.Equal("--tb-tooltip-max-width: 24rem", cut.Find(".tb-tooltip-anchor").GetAttribute("style"));
        Assert.DoesNotContain("wide-tip", cut.Find(".tb-tooltip-anchor").ClassList);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(int.MaxValue + 1L, 0)]
    [InlineData(0, int.MaxValue + 1L)]
    public void Render_ShouldThrow_WhenGivenADelayTheBrowserCantWaitFor(long show, long hide)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<TbTooltip>(parameters => parameters
            .Add(tooltip => tooltip.Text, "Delete").Add(tooltip => tooltip.ShowDelay, TimeSpan.FromMilliseconds(show))
            .Add(tooltip => tooltip.HideDelay, TimeSpan.FromMilliseconds(hide))));
    }

    [Fact]
    public void Render_ShouldUseTheAppsDelays_WhenTheTooltipGivesNone()
    {
        Services.Configure<TrailblazorOptions>(options =>
        {
            options.Tooltips.ShowDelay = TimeSpan.FromMilliseconds(800);
            options.Tooltips.HideDelay = TimeSpan.FromMilliseconds(100);
        });
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete"));

        var bubble = cut.Find("[role=tooltip]");
        Assert.Equal(("800", "100"), (bubble.GetAttribute("data-show-delay"), bubble.GetAttribute("data-hide-delay")));
    }

    [Fact]
    public void Render_ShouldUseItsOwnDelays_WhenTheTooltipGivesThem()
    {
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete")
            .Add(tooltip => tooltip.ShowDelay, TimeSpan.Zero)
            .Add(tooltip => tooltip.HideDelay, TimeSpan.FromSeconds(1.5)));

        var bubble = cut.Find("[role=tooltip]");
        Assert.Equal(("0", "1500"), (bubble.GetAttribute("data-show-delay"), bubble.GetAttribute("data-hide-delay")));
    }

    [Theory]
    [InlineData(false, "6")]
    [InlineData(true, "9")]
    public void Render_ShouldAddAnArrowAndRoomForIt_WhenShowArrowIsSet(bool showArrow, string offset)
    {
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete")
            .Add(tooltip => tooltip.ShowArrow, showArrow));

        Assert.Equal(showArrow, cut.FindAll("[role=tooltip] > [data-part=arrow]").Count == 1);
        Assert.Equal(offset, cut.Find("[role=tooltip]").GetAttribute("data-offset"));
    }
}
