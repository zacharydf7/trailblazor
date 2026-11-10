using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Primitives;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbSpeedDialTests : BunitContext
{
    private static readonly Icon Pin = new("<path d=\"M12 2a6 6 0 0 0-6 6c0 5 6 14 6 14s6-9 6-14a6 6 0 0 0-6-6z\" />");

    public TbSpeedDialTests()
    {
        this.AddTrailblazor();
        JSInterop.SetupModule("./_content/Trailblazor/menu.js").SetupModule("createMenu", _ => true);
    }

    private List<string> Chosen { get; } = [];

    private IRenderedComponent<TbSpeedDial> RenderDial(
        Action<ComponentParameterCollectionBuilder<TbSpeedDial>>? configure = null) =>
        Render<TbSpeedDial>(parameters =>
        {
            parameters.Add(dial => dial.Label, "Add")
                .AddChildContent<TbMenuItem>(item => item.Add(action => action.Icon, Pin)
                    .Add(action => action.OnClick, () => Chosen.Add("Trail")).AddChildContent("Trail"))
                .AddChildContent<TbMenuItem>(item => item.Add(action => action.Icon, Pin)
                    .Add(action => action.OnClick, () => Chosen.Add("Folder")).AddChildContent("Folder"));
            configure?.Invoke(parameters);
        });

    private DotNetObjectReference<MenuEvents> Events =>
        (DotNetObjectReference<MenuEvents>)JSInterop.Invocations["createMenu"].Single().Arguments[3]!;

    [Fact]
    public void Render_ShouldMakeTheTriggerAFloatingMenuButtonNamedByItsLabel_WhenShown()
    {
        var cut = RenderDial();

        var trigger = cut.Find("[data-part=trigger]");
        Assert.Equal(("BUTTON", "Add", "menu"),
            (trigger.TagName, trigger.GetAttribute("aria-label"), trigger.GetAttribute("aria-haspopup")));
        Assert.Contains("tb-fab", trigger.ClassList);
        Assert.Equal(trigger.Id, cut.Find("[role=menu]").GetAttribute("aria-labelledby"));
        Assert.Equal(2, cut.FindAll("[role=menuitem]").Count);
        Assert.Contains("tb-speed-dial--bottomend", cut.Find(".tb-speed-dial").ClassList);
    }

    [Theory]
    [InlineData(FabPosition.BottomEnd, "Top", "End")]
    [InlineData(FabPosition.BottomStart, "Top", "Start")]
    [InlineData(FabPosition.BottomCenter, "Top", "End")]
    [InlineData(FabPosition.TopEnd, "Bottom", "End")]
    [InlineData(FabPosition.TopStart, "Bottom", "Start")]
    [InlineData(FabPosition.Inline, "Top", "End")]
    public void Render_ShouldOpenTheActionsAwayFromTheWindowsEdge_WhenGivenAPosition(FabPosition position,
        string side, string align)
    {
        RenderDial(parameters => parameters.Add(dial => dial.Position, position));

        var options = (AnchoredPositionOptions)JSInterop.Invocations["createMenu"].Single().Arguments[2]!;
        Assert.Equal((side, align), (options.Side.ToString(), options.Align.ToString()));
    }

    [Fact]
    public async Task Choose_ShouldRunTheAction_WhenAnActionIsChosen()
    {
        var cut = RenderDial();

        await cut.Tb().Menu("Add").ChooseAsync("Folder");

        Assert.Equal(["Folder"], Chosen);
    }

    [Fact]
    public async Task Toggle_ShouldReportEachOpenAndClose_WhenTheUserOpensAndClosesTheDial()
    {
        var reported = new List<bool>();
        var cut = RenderDial(parameters =>
            parameters.Add(dial => dial.OpenChanged, (bool open) => reported.Add(open)));

        await cut.InvokeAsync(() => Events.Value.OnToggle(true));
        await cut.InvokeAsync(() => Events.Value.OnToggle(false));

        Assert.Equal([true, false], reported);
    }

    [Fact]
    public void Open_ShouldOpenTheDial_WhenTheParentSetsIt()
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/menu.js").SetupModule("createMenu", _ => true);
        handle.SetupVoid("setOpen", _ => true);
        var cut = RenderDial();

        cut.Render(parameters => parameters.Add(dial => dial.Open, true));

        Assert.Equal(true, handle.Invocations["setOpen"].Single().Arguments[0]);
    }

    [Fact]
    public void Render_ShouldTurnOnlyTheDefaultPlus_WhenTheDialHasNoIconOfItsOwn()
    {
        var plain = RenderDial();
        var own = RenderDial(parameters => parameters.Add(dial => dial.Icon, Pin));

        Assert.Contains("tb-speed-dial__plus", plain.Find("[data-part=trigger]").ClassList);
        Assert.DoesNotContain("tb-speed-dial__plus", own.Find("[data-part=trigger]").ClassList);
    }

    [Fact]
    public void Render_ShouldDisableTheTrigger_WhenDisabled()
    {
        var cut = RenderDial(parameters => parameters.Add(dial => dial.Disabled, true));

        Assert.True(cut.Find("[data-part=trigger]").HasAttribute("disabled"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenTheLabelIsBlank()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbSpeedDial>(parameters =>
            parameters.Add(dial => dial.Label, "")));
    }
}
