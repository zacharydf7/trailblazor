using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Trailblazor.Tests.Components;

public class TbSplitterTests : BunitContext
{
    public TbSplitterTests()
    {
        this.AddTrailblazor();
    }

    [Fact]
    public void Render_ShouldPutANamedSeparatorBetweenEachPair_WhenGivenPanes()
    {
        var cut = RenderSplitter(new Pane("Trails", 25), new Pane("Map"), new Pane("Details", 25));

        var separators = cut.FindAll("[role=separator]");
        Assert.Equal(["Resize Trails", "Resize Map"],
            separators.Select(separator => separator.GetAttribute("aria-label")));
        Assert.Equal(["25", "50"], separators.Select(separator => separator.GetAttribute("aria-valuenow")));
        Assert.Equal(cut.FindAll("[data-part=pane]")[0].Id, separators[0].GetAttribute("aria-controls"));
        Assert.Equal("vertical", separators[0].GetAttribute("aria-orientation"));
    }

    [Fact]
    public void Render_ShouldGiveACollapsedPanesShareToTheOthers_WhenOneIsCollapsed()
    {
        var cut = RenderSplitter(new Pane("Trails", 25, Collapsed: true), new Pane("Map"), new Pane("Details", 25));

        var panes = cut.FindAll("[data-part=pane]");
        Assert.True(panes[0].HasAttribute("data-collapsed"));
        Assert.Equal([true, false, false], panes.Select(pane => pane.HasAttribute("inert")));
        Assert.Contains("flex-grow: 0;", panes[0].GetAttribute("style"));
        Assert.Equal("0", cut.FindAll("[role=separator]")[0].GetAttribute("aria-valuenow"));
        Assert.Equal("75", cut.FindAll("[role=separator]")[1].GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Render_ShouldKeepEachPaneWithinItsLimits_WhenItsSizeIsOutside()
    {
        var cut = RenderSplitter(new Pane("Left", 60, MaxSize: 40), new Pane("Right"));

        var separator = cut.Find("[role=separator]");
        Assert.Equal("40", separator.GetAttribute("aria-valuemax"));
        Assert.Equal("40", separator.GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Render_ShouldStartTheRangeAtZero_WhenThePaneCanCollapse()
    {
        var cut = RenderSplitter(new Pane("Left", 30, Collapsible: true, MinSize: 15), new Pane("Right"));

        Assert.Equal("0", cut.Find("[role=separator]").GetAttribute("aria-valuemin"));
    }

    [Fact]
    public async Task Resize_ShouldKeepTheNewSizes_WhenThePanesAreRenderedAgainWithTheirOldSize()
    {
        var sizes = new List<double?>();
        var module = SetUpScript();
        var cut = RenderSplitter(new Pane("Left", 30, OnSize: sizes.Add), new Pane("Right"));
        var report = (DotNetObjectReference<SplitterReport>)module.Invocations["attachSplitter"].Single().Arguments[1]!;

        await cut.InvokeAsync(() => report.Value.OnResize(PaneIds(cut), [45, 55]));
        cut.Render();

        Assert.Equal([45d], sizes);
        Assert.Equal("45", cut.Find("[role=separator]").GetAttribute("aria-valuenow"));
    }

    [Theory]
    [InlineData(true, false, 0)]
    [InlineData(false, true, 1)]
    public async Task Toggle_ShouldCollapseThePaneBeforeOrElseAfter_WhenEnterIsPressed(bool first, bool second,
        int collapsed)
    {
        var module = SetUpScript();
        var cut = RenderSplitter(new Pane("Left", 30, Collapsible: first), new Pane("Right", Collapsible: second));
        var report = (DotNetObjectReference<SplitterReport>)module.Invocations["attachSplitter"].Single().Arguments[1]!;

        await cut.InvokeAsync(() => report.Value.OnToggle(PaneIds(cut)[0]));

        Assert.True(cut.FindAll("[data-part=pane]")[collapsed].HasAttribute("data-collapsed"));
    }

    [Fact]
    public async Task Toggle_ShouldRestoreThePaneBeforeFirst_WhenTheLastPaneIsCollapsedToo()
    {
        var module = SetUpScript();
        var cut = RenderSplitter(new Pane("Left", 30, Collapsible: true, Collapsed: true),
            new Pane("Right", Collapsible: true, Collapsed: true));
        var report = (DotNetObjectReference<SplitterReport>)module.Invocations["attachSplitter"].Single().Arguments[1]!;

        await cut.InvokeAsync(() => report.Value.OnToggle(PaneIds(cut)[0]));
        Assert.False(cut.FindAll("[data-part=pane]")[0].HasAttribute("data-collapsed"));
        Assert.True(cut.FindAll("[data-part=pane]")[1].HasAttribute("data-collapsed"));

        await cut.InvokeAsync(() => report.Value.OnToggle(PaneIds(cut)[0]));
        Assert.False(cut.FindAll("[data-part=pane]")[1].HasAttribute("data-collapsed"));
    }

    [Fact]
    public async Task Toggle_ShouldCollapseThePaneBefore_WhenAMiddlePaneAfterItIsCollapsed()
    {
        var module = SetUpScript();
        var cut = RenderSplitter(new Pane("Left", 30, Collapsible: true),
            new Pane("Middle", 30, Collapsible: true, Collapsed: true), new Pane("Right"));
        var report = (DotNetObjectReference<SplitterReport>)module.Invocations["attachSplitter"].Single().Arguments[1]!;

        await cut.InvokeAsync(() => report.Value.OnToggle(PaneIds(cut)[0]));

        Assert.True(cut.FindAll("[data-part=pane]")[0].HasAttribute("data-collapsed"));
        Assert.True(cut.FindAll("[data-part=pane]")[1].HasAttribute("data-collapsed"));
    }

    [Fact]
    public async Task Toggle_ShouldRestoreThePaneAtItsOwnSize_WhenTheOthersWereResizedWhileItWasCollapsed()
    {
        var module = SetUpScript();
        var cut = RenderSplitter(new Pane("Trails", 25, Collapsible: true), new Pane("Map"), new Pane("Details", 25));
        var report = (DotNetObjectReference<SplitterReport>)module.Invocations["attachSplitter"].Single().Arguments[1]!;
        var ids = PaneIds(cut);

        await cut.InvokeAsync(() => report.Value.OnToggle(ids[0]));
        await cut.InvokeAsync(() => report.Value.OnResize(ids, [0, 60, 40]));
        await cut.InvokeAsync(() => report.Value.OnToggle(ids[0]));

        Assert.Equal(["25", "45"],
            cut.FindAll("[role=separator]").Select(separator => separator.GetAttribute("aria-valuenow")));
    }

    [Fact]
    public async Task Resize_ShouldHoldAPaneAtItsLimit_WhenAnotherPaneIsCollapsed()
    {
        var module = SetUpScript();
        var cut = RenderSplitter(new Pane("Trails", 25, Collapsible: true), new Pane("Map", MinSize: 20),
            new Pane("Details", 25));
        var report = (DotNetObjectReference<SplitterReport>)module.Invocations["attachSplitter"].Single().Arguments[1]!;
        var ids = PaneIds(cut);
        await cut.InvokeAsync(() => report.Value.OnToggle(ids[0]));
        var map = () => cut.FindAll("[role=separator]")[1];

        // With Trails collapsed, Map's 20% of the whole is 26.67% of the 75% left open.
        Assert.Equal("27", map().GetAttribute("aria-valuemin"));
        Assert.Equal("26.67", cut.FindAll("[data-part=pane]")[1].GetAttribute("data-min"));
        await cut.InvokeAsync(() => report.Value.OnResize(ids, [0, 26.67, 73.33]));
        Assert.Equal("27", map().GetAttribute("aria-valuenow"));
        await cut.InvokeAsync(() => report.Value.OnToggle(ids[0]));

        Assert.Equal("25", cut.FindAll("[role=separator]")[0].GetAttribute("aria-valuenow"));
        Assert.Equal("20", map().GetAttribute("aria-valuenow"));
    }

    [Fact]
    public async Task Order_ShouldPutAPaneAddedLaterInItsPlace_WhenTheBrowserReportsTheMarkupOrder()
    {
        var module = SetUpScript();
        var showOutline = false;
        var cut = Render<TbSplitter>(parameters => parameters.Add(splitter => splitter.ChildContent, builder =>
        {
            if (showOutline)
            {
                AddPane(builder, new Pane("Outline", 20));
            }

            AddPane(builder, new Pane("Editor"));
            AddPane(builder, new Pane("Preview", 30));
        }));
        var report = (DotNetObjectReference<SplitterReport>)module.Invocations["attachSplitter"].Single().Arguments[1]!;
        showOutline = true;
        cut.Render();

        await cut.InvokeAsync(() => report.Value.OnOrder(PaneIds(cut)));

        Assert.Equal(["Resize Outline", "Resize Editor"],
            cut.FindAll("[role=separator]").Select(separator => separator.GetAttribute("aria-label")));
    }

    [Fact]
    public void Attach_ShouldApplyTheSavedLayout_WhenTheScriptHandsOneBack()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/splitter.js");
        var handle = module.SetupModule("attachSplitter", _ => true);
        handle.Setup<SplitterLayout?>("saved").SetResult(new([40, 60], [false, false]));
        handle.SetupVoid("save", _ => true);

        var cut = RenderSplitter(new Pane("Left", 30), new Pane("Right"));

        Assert.Equal("40", cut.Find("[role=separator]").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Render_ShouldGiveEachSeparatorAGripMenuOutOfTheTabOrder_WhenGivenPanes()
    {
        var cut = RenderSplitter(new Pane("Trails", 25, Collapsible: true), new Pane("Map"), new Pane("Details", 25));

        var grips = cut.FindAll("[data-part=grip] > [data-part=trigger]");
        Assert.Equal(["Size of Trails", "Size of Map"], grips.Select(grip => grip.GetAttribute("aria-label")));
        Assert.All(grips, grip => Assert.Equal("-1", grip.GetAttribute("tabindex")));
        Assert.Equal(["Make Trails larger", "Make Trails smaller", "Collapse Trails"], MenuItems(cut, 0));
        Assert.Equal(["Make Map larger", "Make Map smaller"], MenuItems(cut, 1));
    }

    [Theory]
    [InlineData(10, "Make Left larger")]
    [InlineData(-10, "Make Left smaller")]
    public void Grip_ShouldStepTheSeparatorBy10_WhenItsMenuResizes(double delta, string item)
    {
        var handle = JSInterop.SetupModule("./_content/Trailblazor/splitter.js")
            .SetupModule("attachSplitter", _ => true);
        handle.Mode = JSRuntimeMode.Loose;
        var cut = RenderSplitter(new Pane("Left", 30), new Pane("Right"));

        MenuItem(cut, item).Click();

        var step = handle.Invocations["step"].Single();
        Assert.IsType<ElementReference>(step.Arguments[0]);
        Assert.Equal(delta, step.Arguments[1]);
    }

    [Theory]
    [InlineData(true, false, "Left", 0)]
    [InlineData(false, true, "Right", 1)]
    public void Grip_ShouldCollapseAndRestoreThePaneBeforeOrElseAfter_WhenItsMenuToggles(bool first, bool second,
        string name, int collapsed)
    {
        SetUpScript();
        var cut = RenderSplitter(new Pane("Left", 30, Collapsible: first), new Pane("Right", Collapsible: second));
        var pane = () => cut.FindAll("[data-part=pane]")[collapsed];

        MenuItem(cut, $"Collapse {name}").Click();
        Assert.True(pane().HasAttribute("data-collapsed"));
        MenuItem(cut, $"Restore {name}").Click();

        Assert.False(pane().HasAttribute("data-collapsed"));
    }

    [Fact]
    public void Grip_ShouldRestoreTheLastPane_WhenThePaneBeforeItCanCollapseToo()
    {
        SetUpScript();
        var cut = RenderSplitter(new Pane("Left", 30, Collapsible: true),
            new Pane("Right", Collapsible: true, Collapsed: true));

        MenuItem(cut, "Restore Right").Click();

        Assert.False(cut.FindAll("[data-part=pane]")[1].HasAttribute("data-collapsed"));
    }

    [Theory]
    [InlineData(40, false, "true", null)]
    [InlineData(15, false, null, "true")]
    [InlineData(30, true, "true", "true")]
    public void Render_ShouldDisableTheGripsSteps_WhenAPaneIsAtItsLimitOrCollapsed(double size, bool collapsed,
        string? larger, string? smaller)
    {
        var cut = RenderSplitter(new Pane("Left", size, Collapsible: true, Collapsed: collapsed, MinSize: 15,
            MaxSize: 40), new Pane("Right", MinSize: 10));

        Assert.Equal(larger, MenuItem(cut, "Make Left larger").GetAttribute("aria-disabled"));
        Assert.Equal(smaller, MenuItem(cut, "Make Left smaller").GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAPanesLimitsAreOutOfRange()
    {
        Assert.Throws<InvalidOperationException>(() => RenderSplitter(new Pane("Left", MinSize: 60, MaxSize: 40)));
    }

    [Fact]
    public void Render_ShouldThrow_WhenAPaneIsOutsideASplitter()
    {
        Assert.Throws<InvalidOperationException>(() => Render<TbSplitterPane>());
    }

    // The script's handle answers with defaults, so the splitter finds no saved layout.
    private BunitJSModuleInterop SetUpScript()
    {
        var module = JSInterop.SetupModule("./_content/Trailblazor/splitter.js");
        module.SetupModule("attachSplitter", _ => true).Mode = JSRuntimeMode.Loose;
        return module;
    }

    private static IEnumerable<string> MenuItems(IRenderedComponent<TbSplitter> cut, int grip) =>
        cut.FindAll("[data-part=grip]")[grip].QuerySelectorAll("[role=menuitem]")
            .Select(item => item.TextContent.Trim());

    private static AngleSharp.Dom.IElement MenuItem(IRenderedComponent<TbSplitter> cut, string text) =>
        cut.FindAll("[data-part=grip] [role=menuitem]").Single(item => item.TextContent.Trim() == text);

    private static string[] PaneIds(IRenderedComponent<TbSplitter> cut) =>
        [.. cut.FindAll(".tb-splitter > [data-part=pane]").Select(pane => pane.Id!)];

    private IRenderedComponent<TbSplitter> RenderSplitter(params Pane[] panes) =>
        Render<TbSplitter>(parameters => parameters.Add(splitter => splitter.ChildContent, builder =>
        {
            foreach (var pane in panes)
            {
                AddPane(builder, pane);
            }
        }));

    private void AddPane(RenderTreeBuilder builder, Pane pane)
    {
        builder.OpenComponent<TbSplitterPane>(0);
        builder.AddAttribute(1, nameof(TbSplitterPane.Label), pane.Label);
        builder.AddAttribute(2, nameof(TbSplitterPane.Size), pane.Size);
        builder.AddAttribute(3, nameof(TbSplitterPane.Collapsible), pane.Collapsible);
        builder.AddAttribute(4, nameof(TbSplitterPane.Collapsed), pane.Collapsed);
        builder.AddAttribute(5, nameof(TbSplitterPane.MinSize), pane.MinSize);
        builder.AddAttribute(6, nameof(TbSplitterPane.MaxSize), pane.MaxSize);
        builder.AddAttribute(7, nameof(TbSplitterPane.SizeChanged),
            EventCallback.Factory.Create<double?>(this, size => pane.OnSize?.Invoke(size)));
        builder.CloseComponent();
    }

    private sealed record Pane(string Label, double? Size = null, bool Collapsible = false, bool Collapsed = false,
        double MinSize = 0, double MaxSize = 100, Action<double?>? OnSize = null);
}
