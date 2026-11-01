using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Components;

public class TbTooltipTests : BunitContext
{
    private const string Module = "./_content/Trailblazor/tooltip.js";

    public TbTooltipTests() => this.AddTrailblazor();

    [Fact]
    public void Render_ShouldWrapTheTriggerAndHoldAClosedTooltip_WhenCalled()
    {
        JSInterop.SetupModule(Module).SetupModule("createTooltip", _ => true);

        var cut = Render<TbTooltip>(parameters => parameters
            .Add(tooltip => tooltip.Text, "Delete the selection")
            .AddChildContent("<button>Delete</button>"));

        var tooltip = cut.Find("[role=tooltip]");
        Assert.Equal("manual", tooltip.GetAttribute("popover"));
        Assert.Equal("Delete the selection", tooltip.TextContent);
        Assert.NotNull(cut.Find(".tb-tooltip-anchor > button"));
    }

    [Theory]
    [InlineData(Placement.Top, PlacementAlign.Center, "Top", "Center")]
    [InlineData(Placement.Bottom, PlacementAlign.Start, "Bottom", "Start")]
    [InlineData(Placement.Start, PlacementAlign.End, "Start", "End")]
    [InlineData(Placement.End, PlacementAlign.Center, "End", "Center")]
    public void Render_ShouldSetUpTheTooltipOnTheRequestedSide_WhenGivenAPlacementAndAlignment(Placement placement,
        PlacementAlign align, string side, string alignment)
    {
        var module = JSInterop.SetupModule(Module);
        module.SetupModule("createTooltip", _ => true);

        Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete")
            .Add(tooltip => tooltip.Placement, placement).Add(tooltip => tooltip.Align, align));

        var options = Assert.IsType<AnchoredPositionOptions>(module.VerifyInvoke("createTooltip").Arguments[2]);
        Assert.Equal((side, alignment), (options.Side.ToString(), options.Align.ToString()));
    }

    [Fact]
    public void Render_ShouldCenterTheTooltip_WhenNoAlignmentIsGiven()
    {
        var module = JSInterop.SetupModule(Module);
        module.SetupModule("createTooltip", _ => true);

        Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete"));

        var options = Assert.IsType<AnchoredPositionOptions>(module.VerifyInvoke("createTooltip").Arguments[2]);
        Assert.Equal(AnchorAlign.Center, options.Align);
    }

    [Fact]
    public void Render_ShouldShowTheContentInsteadOfTheText_WhenGivenContent()
    {
        JSInterop.SetupModule(Module).SetupModule("createTooltip", _ => true);

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
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier == "createTooltip");
    }

    [Fact]
    public void Render_ShouldDetachAndAttachTheScript_WhenTheTooltipIsTurnedOffAndOn()
    {
        var handle = JSInterop.SetupModule(Module).SetupModule("createTooltip", _ => true);
        handle.SetupVoid("dispose", _ => true);
        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete the selection")
            .AddChildContent("<button>Delete</button>"));

        cut.Render(parameters => parameters.Add(tooltip => tooltip.Disabled, true));
        Assert.Empty(cut.FindAll("[role=tooltip]"));
        cut.Render(parameters => parameters.Add(tooltip => tooltip.Disabled, false));

        Assert.NotNull(cut.Find("[role=tooltip]"));
        Assert.Equal(2, JSInterop.Invocations.Count(call => call.Identifier == "createTooltip"));
        Assert.Single(handle.Invocations, call => call.Identifier == "dispose");
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
        var module = JSInterop.SetupModule(Module);
        module.SetupModule("createTooltip", _ => true);

        Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete"));

        Assert.Equal(new TbTooltip.TooltipDelays(800, 100), module.VerifyInvoke("createTooltip").Arguments[3]);
    }

    [Fact]
    public void Render_ShouldUseItsOwnDelays_WhenTheTooltipGivesThem()
    {
        var module = JSInterop.SetupModule(Module);
        module.SetupModule("createTooltip", _ => true);

        Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete")
            .Add(tooltip => tooltip.ShowDelay, TimeSpan.Zero)
            .Add(tooltip => tooltip.HideDelay, TimeSpan.FromSeconds(1)));

        Assert.Equal(new TbTooltip.TooltipDelays(0, 1000), module.VerifyInvoke("createTooltip").Arguments[3]);
    }

    [Theory]
    [InlineData(false, 6)]
    [InlineData(true, 9)]
    public void Render_ShouldAddAnArrowAndRoomForIt_WhenShowArrowIsSet(bool showArrow, double offset)
    {
        var module = JSInterop.SetupModule(Module);
        module.SetupModule("createTooltip", _ => true);

        var cut = Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete")
            .Add(tooltip => tooltip.ShowArrow, showArrow));

        Assert.Equal(showArrow, cut.FindAll("[role=tooltip] > [data-part=arrow]").Count == 1);
        Assert.Equal(offset, Assert.IsType<AnchoredPositionOptions>(module.VerifyInvoke("createTooltip").Arguments[2])
            .Offset);
    }

    [Fact]
    public async Task DisposeAsync_ShouldTearDownTheTooltip_WhenTheComponentGoes()
    {
        var handle = JSInterop.SetupModule(Module).SetupModule("createTooltip", _ => true);
        handle.SetupVoid("dispose").SetVoidResult();
        Render<TbTooltip>(parameters => parameters.Add(tooltip => tooltip.Text, "Delete"));

        await DisposeComponentsAsync();

        handle.VerifyInvoke("dispose");
    }
}
