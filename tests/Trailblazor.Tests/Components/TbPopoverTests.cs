using Bunit;
using Microsoft.AspNetCore.Components;
using Trailblazor.Primitives;

namespace Trailblazor.Tests.Components;

public class TbPopoverTests : BunitContext
{
    public TbPopoverTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldWireTheTriggerToANamedPanel_WhenGivenALabelAndTitle()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.Title, "Filter the report").AddChildContent("Options"));

        var trigger = cut.Find("[data-part=trigger]");
        var panel = cut.Find("[data-part=panel]");
        Assert.Equal("Filters", trigger.TextContent.Trim());
        Assert.Equal(("dialog", panel.Id, panel.Id, "false"), (trigger.GetAttribute("aria-haspopup"),
            trigger.GetAttribute("popovertarget"), trigger.GetAttribute("aria-controls"),
            trigger.GetAttribute("aria-expanded")));
        Assert.Equal(("auto", "dialog"), (panel.GetAttribute("popover"), panel.GetAttribute("role")));
        Assert.Equal(cut.Find("[data-part=title]").Id, panel.GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_ShouldNameThePanelAfterTheTrigger_WhenThereIsNoTitle()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Shortcuts"));

        Assert.Equal(cut.Find("[data-part=trigger]").Id, cut.Find("[data-part=panel]").GetAttribute("aria-labelledby"));
        Assert.Empty(cut.FindAll("[data-part=title]"));
    }

    [Fact]
    public void Render_ShouldHandTheTriggerAttributesToACustomTrigger_WhenTriggerIsSet()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Trigger,
            (RenderFragment<PopoverTrigger>)(trigger => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddMultipleAttributes(1, trigger.Attributes);
                builder.AddContent(2, "Ada");
                builder.CloseElement();
            })));

        var button = cut.Find("button");
        Assert.Equal("Ada", button.TextContent);
        Assert.Equal(cut.Find("[data-part=panel]").Id, button.GetAttribute("aria-controls"));
        Assert.Equal("trigger", button.GetAttribute("data-part"));
    }

    [Fact]
    public void Render_ShouldPutAGivenIdOnTheTriggerAndNameThePanelByIt_WhenThePopoverHasOne()
    {
        var cut = Render<TbPopover>(parameters => parameters.AddUnmatched("id", "help").Add(popover => popover.Label,
            "Help"));

        Assert.Equal("trigger", Assert.Single(cut.FindAll("#help")).GetAttribute("data-part"));
        Assert.Equal("help", cut.Find("[data-part=panel]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenThereIsNoLabelOrTrigger()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbPopover>());
    }

    [Theory]
    [InlineData(Placement.Bottom, PlacementAlign.Start, "Bottom", "Start")]
    [InlineData(Placement.Top, PlacementAlign.Center, "Top", "Center")]
    [InlineData(Placement.Start, PlacementAlign.End, "Start", "End")]
    [InlineData(Placement.End, PlacementAlign.Start, "End", "Start")]
    public void Render_ShouldSetUpThePanelOnTheRequestedSide_WhenGivenAPlacementAndAlignment(Placement placement,
        PlacementAlign align, string side, string alignment)
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/popover.js");
        module.SetupModule("createPopover", _ => true);

        Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.Placement, placement).Add(popover => popover.Align, align));

        var options = Assert.IsType<AnchoredPositionOptions>(module.VerifyInvoke("createPopover").Arguments[2]);
        Assert.Equal(side, options.Side.ToString());
        Assert.Equal(alignment, options.Align.ToString());
    }

    [Fact]
    public async Task Toggle_ShouldTellTheParent_WhenTheBrowserOpensOrClosesThePanel()
    {
        var states = new List<bool>();
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.OpenChanged, (bool open) => states.Add(open)));

        await cut.InvokeAsync(() => cut.Instance.OnToggleAsync(true));
        await cut.InvokeAsync(() => cut.Instance.OnToggleAsync(true));
        await cut.InvokeAsync(() => cut.Instance.OnToggleAsync(false));

        Assert.Equal([true, false], states);
    }
}
