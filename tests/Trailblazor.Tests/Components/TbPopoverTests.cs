using Microsoft.JSInterop;
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
    public void Render_ShouldNameTheIconTriggerWithTheAriaLabel_WhenAnIconIsSet()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.AriaLabel, "Filters")
            .Add(popover => popover.Icon, new Icon("<path d=\"M4 4h16v16H4z\" />")).AddChildContent("Options"));

        Assert.Equal("Filters", cut.Find("[data-part=trigger]").GetAttribute("aria-label"));
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScriptOnlyOnceTheTriggerTakesFocus_WhenSet()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.AttachOnFirstUse, true).AddChildContent("Options"));
        Assert.Empty(JSInterop.Invocations["createPopover"]);
        var trigger = cut.Find("[data-part=trigger]");
        Assert.True(trigger.HasAttribute("data-attach-on-use"));
        Assert.Equal(cut.Find("[data-part=panel]").Id, trigger.GetAttribute("popovertarget"));

        trigger.Focus();

        Assert.Single(JSInterop.Invocations["createPopover"]);
        Assert.False(cut.Find("[data-part=trigger]").HasAttribute("data-attach-on-use"));
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScript_WhenThePointerReachesTheTrigger()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.AttachOnFirstUse, true).AddChildContent("Options"));

        cut.Find("[data-part=trigger]").PointerEnter();

        Assert.Single(JSInterop.Invocations["createPopover"]);
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScript_WhenTheTriggerIsClickedWithoutFocus()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.AttachOnFirstUse, true).AddChildContent("Options"));

        cut.Find("[data-part=trigger]").Click();

        Assert.Single(JSInterop.Invocations["createPopover"]);
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScriptAndOpen_WhenTheParentOpensItBeforeUse()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/popover.js").SetupModule("createPopover", _ => true);
        handle.SetupVoid("setOpen", _ => true);
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.AttachOnFirstUse, true).AddChildContent("Options"));

        cut.Render(parameters => parameters.Add(popover => popover.Open, true));

        Assert.Single(JSInterop.Invocations["createPopover"]);
        Assert.Equal([true], handle.Invocations["setOpen"].Select(call => call.Arguments[0]));
        Assert.False(cut.Find("[data-part=trigger]").HasAttribute("data-attach-on-use"));
    }

    [Fact]
    public void AttachOnFirstUse_ShouldStartTheScript_WhenTurnedOffBeforeUse()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.AttachOnFirstUse, true).AddChildContent("Options"));

        cut.Render(parameters => parameters.Add(popover => popover.AttachOnFirstUse, false));

        Assert.Single(JSInterop.Invocations["createPopover"]);
    }

    [Fact]
    public void Render_ShouldThrow_WhenAnIconTriggerHasALabelButNoAriaLabel()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Render<TbPopover>(parameters => parameters
            .Add(popover => popover.Label, "Filters")
            .Add(popover => popover.Icon, new Icon("<path d=\"M4 4h16v16H4z\" />"))));

        Assert.Contains("AriaLabel", error.Message);
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
    public void Render_ShouldNameThePanelByTheGivenElement_WhenPanelLabelledByIsSetWithoutATitle()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Shortcuts")
            .Add(popover => popover.PanelLabelledBy, "shortcuts-heading"));

        Assert.Equal("shortcuts-heading", cut.Find("[data-part=panel]").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Render_ShouldNameThePanelByItsTitle_WhenPanelLabelledByIsSetToo()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Shortcuts")
            .Add(popover => popover.Title, "Keyboard shortcuts").Add(popover => popover.PanelLabelledBy, "elsewhere"));

        Assert.Equal(cut.Find("[data-part=title]").Id, cut.Find("[data-part=panel]").GetAttribute("aria-labelledby"));
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
    public void Render_ShouldDisableTheTrigger_WhenThePopoverIsDisabled()
    {
        var builtIn = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Help")
            .Add(popover => popover.Disabled, true));
        var custom = Render<TbPopover>(parameters => parameters.Add(popover => popover.Disabled, true)
            .Add(popover => popover.Trigger, (RenderFragment<PopoverTrigger>)(trigger => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddMultipleAttributes(1, trigger.Attributes);
                builder.CloseElement();
            })));

        Assert.True(builtIn.Find("[data-part=trigger]").HasAttribute("disabled"));
        Assert.True(custom.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldDisableATbButtonTrigger_WhenItTakesOnlyTheTriggerAttributes()
    {
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Disabled, true)
            .Add(popover => popover.Trigger, (RenderFragment<PopoverTrigger>)(trigger => builder =>
            {
                builder.OpenComponent<TbButton>(0);
                builder.AddMultipleAttributes(1, trigger.Attributes);
                builder.CloseComponent();
            })));

        Assert.True(cut.Find("[data-part=trigger]").HasAttribute("disabled"));
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

    [Fact]
    public void Placement_ShouldAttachTheScriptAgainWithTheNewOptions_WhenPlacementAlignOrOpenOnHoverChanges()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/popover.js");
        module.SetupModule("createPopover", _ => true).Mode = JSRuntimeMode.Loose;
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .AddChildContent("Options"));

        cut.Render(parameters => parameters.Add(popover => popover.Placement, Placement.Top));
        cut.Render(parameters => parameters.Add(popover => popover.Placement, Placement.Top));
        cut.Render(parameters => parameters.Add(popover => popover.Align, PlacementAlign.End));
        cut.Render(parameters => parameters.Add(popover => popover.OpenOnHover, true));

        var calls = module.Invocations["createPopover"];
        Assert.Equal(4, calls.Count);
        Assert.Equal(AnchorSide.Top, ((AnchoredPositionOptions)calls[1].Arguments[2]!).Side);
        Assert.Equal(Anchors.Align(PlacementAlign.End), ((AnchoredPositionOptions)calls[2].Arguments[2]!).Align);
        Assert.Equal(true, calls[3].Arguments[3]);
    }

    [Fact]
    public void Placement_ShouldCloseThePanelFirst_WhenTheParentClosesItAlongWithAPlacementChange()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/popover.js");
        var handle = module.SetupModule("createPopover", _ => true);
        handle.Mode = JSRuntimeMode.Loose;
        var cut = Render<TbPopover>(parameters => parameters.Add(popover => popover.Label, "Filters")
            .Add(popover => popover.Open, true).Add(popover => popover.OpenChanged, (bool _) => { })
            .AddChildContent("Options"));

        cut.Render(parameters => parameters.Add(popover => popover.Open, false)
            .Add(popover => popover.Placement, Placement.Top));

        var calls = handle.Invocations.Select(call => (call.Identifier, call.Arguments.FirstOrDefault())).ToList();
        var closed = calls.IndexOf(("setOpen", false));
        Assert.True(closed >= 0, "The parent's close didn't reach the script.");
        Assert.True(closed < calls.IndexOf(("dispose", null)), "The old script was disposed before it closed.");
    }
}
