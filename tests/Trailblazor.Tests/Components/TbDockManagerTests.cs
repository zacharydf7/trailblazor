using System.Diagnostics.Metrics;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDockManagerTests : BunitContext
{
    // Trails and Map share a group beside the notes, which sit above the weather.
    private static readonly DockLayout Hike = new(new DockSplit(
        new DockGroup("trails", "map") { Size = 1 },
        new DockSplit(new DockGroup("notes") { Size = 3 }, new DockGroup("weather")) { Vertical = true, Size = 3 }));

    private static readonly Panel[] HikePanels =
        [new("trails", "Trails"), new("map", "Map"), new("notes", "Notes"), new("weather", "Weather")];

    private readonly BunitJSModuleInterop _module;
    private readonly BunitJSModuleInterop _handle;
    private readonly List<string> _log = [];

    public TbDockManagerTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/dock-manager.js");
        _handle = _module.SetupModule("attachDock", _ => true);
        _handle.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Render_ShouldGiveEachGroupANamedTabListInItsBox_WhenGivenALayout()
    {
        var cut = RenderDock(Hike, HikePanels);

        var groups = cut.FindAll("[data-part=group]");
        Assert.Equal(["Panel group 1", "Panel group 2", "Panel group 3"],
            groups.Select(group => group.QuerySelector("[role=tablist]")!.GetAttribute("aria-label")));
        Assert.Equal([["Trails", "Map"], ["Notes"], ["Weather"]], groups.Select(Titles));
        Assert.Equal(["--tb-dock-x: 0; --tb-dock-y: 0; --tb-dock-w: 25; --tb-dock-h: 100;",
            "--tb-dock-x: 25; --tb-dock-y: 0; --tb-dock-w: 75; --tb-dock-h: 75;",
            "--tb-dock-x: 25; --tb-dock-y: 75; --tb-dock-w: 75; --tb-dock-h: 25;"],
            groups.Select(group => group.GetAttribute("style")));
    }

    [Fact]
    public void Render_ShouldSelectEachGroupsShowingTabAndRenderOnlyItsPanel_WhenGivenALayout()
    {
        var cut = RenderDock(Hike, HikePanels);

        Assert.Equal(["Trails", "Notes", "Weather"],
            cut.FindAll("[role=tab][aria-selected=true]").Select(tab => tab.TextContent.Trim()));
        Assert.Equal(["0", "-1", "0", "0"], cut.FindAll("[role=tab]").Select(tab => tab.GetAttribute("tabindex")));
        var panels = cut.FindAll("[role=tabpanel]");
        Assert.Equal(["trails", "notes", "weather"], panels.Select(panel => panel.GetAttribute("data-panel")));
        Assert.All(panels, panel =>
        {
            Assert.False(panel.HasAttribute("inert"));
            Assert.Equal("0", panel.GetAttribute("tabindex"));
        });
        Assert.Equal(["init trails", "init notes", "init weather"], _log.Where(line => line.StartsWith("init")));
    }

    [Fact]
    public void Render_ShouldTieEachTabToItsPanel_WhenThePanelIsRendered()
    {
        var cut = RenderDock(Hike, HikePanels);

        var trails = Tab(cut, "Trails");
        var panel = cut.Find("[role=tabpanel][data-panel=trails]");
        Assert.Equal(panel.Id, trails.GetAttribute("aria-controls"));
        Assert.Equal(trails.Id, panel.GetAttribute("aria-labelledby"));
        Assert.Equal(cut.FindAll("[data-part=group]")[0].GetAttribute("style"), panel.GetAttribute("style")!.Trim());
        // The map's panel hasn't shown yet, so there is nothing for its tab to name.
        Assert.False(Tab(cut, "Map").HasAttribute("aria-controls"));
    }

    [Fact]
    public void Render_ShouldPutEachPanelAfterItsGroupsTabsAndASeparatorBetweenGroups_WhenFirstRendered()
    {
        var cut = RenderDock(Hike, HikePanels);

        Assert.Equal(["group", "panel", "separator", "group", "panel", "separator", "group", "panel"], Parts(cut));
    }

    [Fact]
    public void Render_ShouldPutEveryPanelInOneGroup_WhenGivenNoLayout()
    {
        var cut = RenderDock(null, HikePanels);

        Assert.Equal(["Trails", "Map", "Notes", "Weather"], Titles(cut.Find("[data-part=group]")));
        Assert.Empty(cut.FindAll("[role=separator]"));
    }

    [Fact]
    public void Render_ShouldAddAPanelTheLayoutLacksToTheLargestGroup_WhenGivenOne()
    {
        var cut = RenderDock(Hike, [.. HikePanels, new("photos", "Photos")]);

        Assert.Equal(["Notes", "Photos"], Titles(cut.FindAll("[data-part=group]")[1]));
        Assert.Equal("true", Tab(cut, "Notes").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task Render_ShouldSkipAPanelIdWithNoPanelAndKeepItsPlace_WhenTheLayoutNamesOne()
    {
        DockLayout? changed = null;
        var cut = RenderDock(Hike, [.. HikePanels.Where(panel => panel.Id != "weather")], layout => changed = layout);

        Assert.Equal([["Trails", "Map"], ["Notes"]], cut.FindAll("[data-part=group]").Select(Titles));
        // With the weather gone, the notes take the whole column.
        Assert.Contains("--tb-dock-h: 100;", cut.FindAll("[data-part=group]")[1].GetAttribute("style"));
        await Tab(cut, "Map").ClickAsync();
        Assert.Equal(["weather"], changed!.GroupOf("weather")!.Panels);
    }

    [Fact]
    public void Render_ShouldLeaveAClosedPanelOut_WhenTheLayoutListsItAsClosed()
    {
        var cut = RenderDock(Hike.Close("map"), HikePanels);

        Assert.Equal(["Trails"], Titles(cut.FindAll("[data-part=group]")[0]));
    }

    [Fact]
    public void Render_ShouldShowTheEmptyContent_WhenNothingIsDocked()
    {
        var cut = Render<TbDockManager>(parameters => parameters
            .Add(dock => dock.Layout, new DockLayout(null, ["map"]))
            .Add(dock => dock.ChildContent, Panels(new Panel("map", "Map")))
            .Add(dock => dock.EmptyContent, "Open a panel from the View menu."));

        Assert.Equal("Open a panel from the View menu.", cut.Find("[data-part=empty]").TextContent);
        Assert.Empty(cut.FindAll("[data-part=group]"));
    }

    [Fact]
    public void Render_ShouldLeaveTheEmptyContentOut_WhenAPanelIsDocked()
    {
        var cut = RenderDock(Hike, HikePanels);

        Assert.Empty(cut.FindAll("[data-part=empty]"));
    }

    [Fact]
    public void Render_ShouldNameTheDockAndPassItsAttributes_WhenGivenThem()
    {
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.AriaLabel, "Hike planner")
            .Add(dock => dock.Class, "planner").Add(dock => dock.Style, "height: 30rem").AddUnmatched("id", "dock")
            .Add(dock => dock.MinGroupSize, 120));

        var root = cut.Find("[data-part=dock]");
        Assert.Equal(("group", "Hike planner", "tb-dock planner", "height: 30rem", "dock", "120"),
            (root.GetAttribute("role"), root.GetAttribute("aria-label"), root.ClassName, root.GetAttribute("style"),
                root.Id, root.GetAttribute("data-min-size")));
    }

    [Fact]
    public void Render_ShouldPutAPanelsClassStyleAndAttributesOnItsPanel_WhenGivenThem()
    {
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.ChildContent,
            (RenderFragment)(builder =>
        {
            builder.OpenComponent<TbDockPanel>(0);
            builder.AddAttribute(1, nameof(TbDockPanel.Id), "map");
            builder.AddAttribute(2, nameof(TbDockPanel.Title), "Map");
            builder.AddAttribute(3, nameof(TbDockPanel.Class), "wide");
            builder.AddAttribute(4, nameof(TbDockPanel.Style), "padding: 1rem");
            builder.AddAttribute(5, "data-kind", "viewer");
            builder.AddAttribute(6, nameof(TbDockPanel.Icon), BuiltInIcons.Plus);
            builder.CloseComponent();
        })));

        var panel = cut.Find("[role=tabpanel]");
        Assert.Equal("tb-dock__panel wide", panel.ClassName);
        Assert.EndsWith("padding: 1rem", panel.GetAttribute("style"));
        Assert.Equal("viewer", panel.GetAttribute("data-kind"));
        Assert.NotNull(cut.Find("[role=tab] [data-part=icon]"));
    }

    [Fact]
    public async Task Select_ShouldShowTheTabsPanelAndKeepTheOtherAlive_WhenATabIsClicked()
    {
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);

        await Tab(cut, "Map").ClickAsync();

        Assert.Equal(("false", "true"),
            (Tab(cut, "Trails").GetAttribute("aria-selected"), Tab(cut, "Map").GetAttribute("aria-selected")));
        var trails = cut.Find("[role=tabpanel][data-panel=trails]");
        Assert.True(trails.HasAttribute("inert"));
        Assert.False(trails.HasAttribute("tabindex"));
        Assert.Contains("tb-dock__panel--hidden", trails.ClassName);
        Assert.False(cut.Find("[role=tabpanel][data-panel=map]").HasAttribute("inert"));
        Assert.Equal("map", changed!.GroupOf("map")!.Active);
        Assert.DoesNotContain("dispose trails", _log);
        Assert.Equal(trails.Id, Tab(cut, "Trails").GetAttribute("aria-controls"));
    }

    [Fact]
    public async Task Select_ShouldRenderNoPanelsContentAgain_WhenATabIsClicked()
    {
        var cut = RenderDock(Hike, HikePanels);
        _log.Clear();

        await Tab(cut, "Map").ClickAsync();
        await Tab(cut, "Trails").ClickAsync();

        Assert.Equal(["init map", "render map"], _log);
    }

    [Fact]
    public async Task Select_ShouldRaiseNothing_WhenTheShowingTabIsClicked()
    {
        var changes = 0;
        var cut = RenderDock(Hike, HikePanels, _ => changes++);

        await Tab(cut, "Trails").ClickAsync();

        Assert.Equal(0, changes);
    }

    [Fact]
    public async Task Select_ShouldDisposeAHiddenPanelsContent_WhenItsKeepAliveIsOff()
    {
        var cut = RenderDock(Hike, [new("trails", "Trails", KeepAlive: false), .. HikePanels.Skip(1)]);

        await Tab(cut, "Map").ClickAsync();

        Assert.Contains("dispose trails", _log);
        Assert.Empty(cut.FindAll("[role=tabpanel][data-panel=trails]"));
        Assert.False(Tab(cut, "Trails").HasAttribute("aria-controls"));

        await Tab(cut, "Trails").ClickAsync();

        Assert.Equal(2, _log.Count(line => line == "init trails"));
    }

    [Fact]
    public async Task Select_ShouldShowTheTab_WhenTheScriptsArrowKeysReachIt()
    {
        var cut = RenderDock(Hike, HikePanels);

        await cut.InvokeAsync(() => Report().OnSelect("map"));

        Assert.Equal("true", Tab(cut, "Map").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task Close_ShouldRemoveTheTabAndDisposeItsContent_WhenItsMarkIsClicked()
    {
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);

        await Tab(cut, "Trails").QuerySelector("[data-part=close]")!.ClickAsync();

        Assert.Equal(["Map"], Titles(cut.FindAll("[data-part=group]")[0]));
        Assert.Equal("true", Tab(cut, "Map").GetAttribute("aria-selected"));
        Assert.Equal(["trails"], changed!.Closed);
        Assert.Contains("dispose trails", _log);
    }

    [Fact]
    public async Task Close_ShouldGiveTheGroupsSpaceToTheOthers_WhenItsLastPanelCloses()
    {
        var cut = RenderDock(Hike, HikePanels);

        await cut.InvokeAsync(() => Report().OnClose("weather", true));

        Assert.Equal([["Trails", "Map"], ["Notes"]], cut.FindAll("[data-part=group]").Select(Titles));
        Assert.Single(cut.FindAll("[role=separator]"));
        Assert.Equal(["group", "panel", "separator", "group", "panel"], Parts(cut));
    }

    [Fact]
    public async Task Close_ShouldFocusTheNextTabOrElseTheOneBefore_WhenATabOfAGroupCloses()
    {
        var cut = RenderDock(new DockLayout(new DockGroup("trails", "map", "notes")), HikePanels.Take(3).ToArray());
        var (trails, notes) = (Reference(cut, "Trails"), Reference(cut, "Notes"));

        await cut.InvokeAsync(() => Report().OnClose("map", true));

        Assert.Equal(notes, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);

        await cut.InvokeAsync(() => Report().OnClose("notes", true));

        Assert.Equal(trails, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke(2)[1].Arguments[0]!).Id);
    }

    [Fact]
    public async Task Close_ShouldFocusTheNextGroupsShowingTabOrElseTheOneBefore_WhenTheGroupGoes()
    {
        var cut = RenderDock(Hike, HikePanels);
        var trails = Reference(cut, "Trails");

        await cut.InvokeAsync(() => Report().OnClose("notes", true));

        // The weather's tab is drawn anew in the second group's place, so only a browser can tell it took the focus.
        JSInterop.VerifyFocusAsyncInvoke();

        await cut.InvokeAsync(() => Report().OnClose("weather", true));

        Assert.Equal(trails, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke(2)[1].Arguments[0]!).Id);
    }

    [Fact]
    public async Task Close_ShouldFocusTheDock_WhenItsLastPanelCloses()
    {
        var cut = RenderDock(null, new Panel("map", "Map"));
        Assert.False(cut.Find("[data-part=dock]").HasAttribute("tabindex"));

        await cut.InvokeAsync(() => Report().OnClose("map", true));

        // No tab is left to take the focus, and only a browser can tell that the dock is what took it.
        JSInterop.VerifyFocusAsyncInvoke();
        Assert.Empty(cut.FindAll("[role=tab]"));
        Assert.Equal("-1", cut.Find("[data-part=dock]").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task Close_ShouldLeaveTheFocusWhereItIs_WhenNeitherTheTabNorItsPanelHeldIt()
    {
        var cut = RenderDock(Hike, HikePanels);

        await cut.InvokeAsync(() => Report().OnClose("map", false));
        await cut.InvokeAsync(() => Report().OnClose("weather", false));

        Assert.Equal([["Trails"], ["Notes"]], cut.FindAll("[data-part=group]").Select(Titles));
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier.EndsWith("focus"));
    }

    [Fact]
    public async Task Close_ShouldMoveTheFocus_WhenTheMarkIsClickedAndTheScriptSaysTheTabHoldsIt()
    {
        var cut = RenderDock(new DockLayout(new DockGroup("trails", "map", "notes")), HikePanels.Take(3).ToArray());
        var (trails, notes) = (Tab(cut, "Trails").Id, Reference(cut, "Notes"));

        // The script finds the focus elsewhere, as after a press on the mark, which leaves it where it was.
        await Tab(cut, "Trails").QuerySelector("[data-part=close]")!.ClickAsync();
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier.EndsWith("focus"));
        Assert.Equal(trails, _handle.Invocations["holdsFocus"].Single().Arguments[0]);

        _handle.Setup<bool>("holdsFocus", _ => true).SetResult(true);
        await Tab(cut, "Map").QuerySelector("[data-part=close]")!.ClickAsync();

        Assert.Equal(notes, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Fact]
    public async Task Close_ShouldLeaveAPanelOpen_WhenItIsNotClosable()
    {
        var changes = 0;
        var cut = RenderDock(Hike, [new("trails", "Trails", Closable: false), .. HikePanels.Skip(1)], _ => changes++);

        var trails = Tab(cut, "Trails");
        Assert.Null(trails.QuerySelector("[data-part=close]"));
        Assert.False(trails.HasAttribute("aria-keyshortcuts"));
        Assert.False(trails.HasAttribute("aria-describedby"));

        await cut.InvokeAsync(() => Report().OnClose("trails", true));

        Assert.Equal(0, changes);
        Assert.NotNull(Tab(cut, "Trails"));
    }

    [Fact]
    public void Render_ShouldTellScreenReadersHowToCloseATab_WhenItIsClosable()
    {
        var cut = RenderDock(Hike, HikePanels);

        var map = Tab(cut, "Map");
        Assert.Equal("Delete Backspace", map.GetAttribute("aria-keyshortcuts"));
        var hint = cut.Find($"[id='{map.GetAttribute("aria-describedby")}']");
        Assert.Equal("Press Delete to close", hint.TextContent);
        Assert.True(hint.HasAttribute("hidden"));
        Assert.Equal("true", map.QuerySelector("[data-part=close]")!.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Render_ShouldPutANamedSeparatorOnEachSharedEdge_WhenGivenALayout()
    {
        var cut = RenderDock(Hike, HikePanels);

        var separators = cut.FindAll("[role=separator]");
        Assert.Equal(["Resize Trails", "Resize Notes"], separators.Select(each => each.GetAttribute("aria-label")));
        Assert.Equal(["vertical", "horizontal"], separators.Select(each => each.GetAttribute("aria-orientation")));
        Assert.Equal(["25", "75"], separators.Select(each => each.GetAttribute("aria-valuenow")));
        Assert.Equal(["0", "0"], separators.Select(each => each.GetAttribute("tabindex")));
        Assert.Equal(new[] { "trails", "notes" }.Select(id => cut.Find($"[role=tabpanel][data-panel={id}]").Id),
            separators.Select(each => each.GetAttribute("aria-controls")));
        Assert.Equal(["--tb-dock-x: 25; --tb-dock-y: 0; --tb-dock-w: 0; --tb-dock-h: 100;",
            "--tb-dock-x: 25; --tb-dock-y: 75; --tb-dock-w: 75; --tb-dock-h: 0;"],
            separators.Select(each => each.GetAttribute("style")));
        Assert.Equal(("0 0 25 100", "25 0 75 100"),
            (separators[0].GetAttribute("data-before"), separators[0].GetAttribute("data-after")));
        Assert.Equal("tb-dock__separator tb-dock__separator--vertical", separators[1].ClassName);
    }

    [Fact]
    public void Render_ShouldNameEachSeparatorByTheGroupJustBeforeIt_WhenASplitComesFirst()
    {
        // Trails above the map, beside the notes: the second separator stands after the map's group.
        var cut = RenderDock(new DockLayout(new DockSplit(
            new DockSplit(new DockGroup("trails"), new DockGroup("map")) { Vertical = true }, new DockGroup("notes"))),
            HikePanels.Take(3).ToArray());

        var separators = cut.FindAll("[role=separator]");
        Assert.Equal(["Resize Trails", "Resize Map"], separators.Select(each => each.GetAttribute("aria-label")));
        Assert.Equal(new[] { "trails", "map" }.Select(id => cut.Find($"[role=tabpanel][data-panel={id}]").Id),
            separators.Select(each => each.GetAttribute("aria-controls")));
    }

    [Fact]
    public async Task Resize_ShouldShareTheTwoPartsSpaceAnew_WhenTheScriptReportsASeparatorMoved()
    {
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);

        await cut.InvokeAsync(() => Report().OnResize(0, 0.4, 1));

        Assert.Contains("--tb-dock-w: 40;", cut.FindAll("[data-part=group]")[0].GetAttribute("style"));
        Assert.Contains("--tb-dock-x: 40;", cut.FindAll("[data-part=group]")[1].GetAttribute("style"));
        Assert.Equal("40", cut.FindAll("[role=separator]")[0].GetAttribute("aria-valuenow"));
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-report"));
        Assert.Equal([1.6, 2.4],
            Assert.IsType<DockSplit>(changed!.Root).Children.Select(child => Math.Round(child.Size, 4)));
    }

    [Theory]
    [InlineData(5, 0.5)]
    [InlineData(-1, 0.5)]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(0, double.NaN)]
    public async Task Resize_ShouldChangeNothingButTheReportsNumber_WhenTheReportMakesNoSense(int separator,
        double fraction)
    {
        var changes = 0;
        var cut = RenderDock(Hike, HikePanels, _ => changes++);

        await cut.InvokeAsync(() => Report().OnResize(separator, fraction, 3));

        Assert.Equal(0, changes);
        Assert.Equal("3", cut.Find("[data-part=dock]").GetAttribute("data-report"));
    }

    [Fact]
    public async Task Layout_ShouldReplaceWhatTheUserDid_WhenTheParentGivesANewOne()
    {
        var cut = RenderDock(Hike, HikePanels);
        await Tab(cut, "Map").ClickAsync();

        cut.Render(parameters => parameters.Add(dock => dock.Layout, new DockLayout(new DockGroup("weather", "trails"))));

        Assert.Equal([["Weather", "Trails", "Map", "Notes"]], cut.FindAll("[data-part=group]").Select(Titles));
    }

    [Fact]
    public async Task Layout_ShouldKeepWhatTheUserDid_WhenTheParentRendersTheSameOneAgain()
    {
        var cut = RenderDock(Hike, HikePanels);
        await Tab(cut, "Map").ClickAsync();

        cut.Render(parameters => parameters.Add(dock => dock.Layout, Hike));

        Assert.Equal("true", Tab(cut, "Map").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Layout_ShouldLeaveEveryRenderedPartWhereItWasAmongItsSiblings_WhenAPanelChangesGroup()
    {
        var cut = RenderDock(Hike, HikePanels);
        var before = cut.FindAll("[data-part=dock] > [data-part]").Select(Key).ToList();

        // The weather joins the first group and shows there, so its group and a separator go.
        cut.Render(parameters => parameters.Add(dock => dock.Layout, new DockLayout(new DockSplit(
            new DockGroup("trails", "map", "weather") { Active = "weather" }, new DockGroup("notes")))));

        Assert.Equal(["group", "trails", "separator", "group", "notes", "weather"],
            cut.FindAll("[data-part=dock] > [data-part]").Select(Key));
        Assert.Equal(["group", "trails", "separator", "group", "notes", "separator", "group", "weather"], before);
        Assert.Equal("0", cut.Find("[role=tabpanel][data-panel=weather]").GetAttribute("data-dock-group"));
        Assert.Equal(["init trails", "init notes", "init weather"], _log.Where(line => line.StartsWith("init")));
        Assert.DoesNotContain(_log, line => line.StartsWith("dispose"));
    }

    [Fact]
    public void ChildContent_ShouldRenderAPanelsContentAndTitleAgain_WhenTheParentRendersIt()
    {
        var cut = RenderDock(Hike, HikePanels);
        _log.Clear();

        cut.Render(parameters => parameters.Add(dock => dock.ChildContent,
            Panels([new("trails", "Trail list"), .. HikePanels.Skip(1)])));

        Assert.NotNull(Tab(cut, "Trail list"));
        Assert.Equal(["render trails", "render notes", "render weather"], _log);
    }

    [Fact]
    public void ChildContent_ShouldDropAPanel_WhenTheParentStopsRenderingIt()
    {
        var cut = RenderDock(Hike, HikePanels);

        cut.Render(parameters => parameters.Add(dock => dock.ChildContent, Panels([.. HikePanels.Skip(1)])));

        Assert.Equal(["Map"], Titles(cut.FindAll("[data-part=group]")[0]));
        Assert.Contains("dispose trails", _log);
    }

    [Fact]
    public void ChildContent_ShouldShowAPanelsNewContent_WhenTheParentMakesThePanelAnewUnderItsId()
    {
        RenderFragment Keyed(string key, string text) => builder =>
        {
            builder.OpenComponent<TbDockPanel>(0);
            builder.SetKey(key);
            builder.AddAttribute(1, nameof(TbDockPanel.Id), "map");
            builder.AddAttribute(2, nameof(TbDockPanel.Title), text);
            builder.AddAttribute(3, nameof(TbDockPanel.ChildContent),
                (RenderFragment)(content => content.AddContent(0, text)));
            builder.CloseComponent();
        };
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.ChildContent, Keyed("first", "Map")));

        // The new panel registers before the old one is disposed, and takes its place.
        cut.Render(parameters => parameters.Add(dock => dock.ChildContent, Keyed("second", "Chart")));

        Assert.Equal("Chart", cut.Find("[role=tabpanel]").TextContent);
        Assert.Equal(["Chart"], Titles(cut.Find("[data-part=group]")));
    }

    [Fact]
    public void Labels_ShouldWordTheGroupsSeparatorsAndCloseHint_WhenGiven()
    {
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels,
            new DockLabels { Group = "Group {0}", Resize = "Size of {0}", CloseHint = "Delete closes this tab" }));

        Assert.Equal("Group 1", cut.Find("[role=tablist]").GetAttribute("aria-label"));
        Assert.Equal("Size of Trails", cut.Find("[role=separator]").GetAttribute("aria-label"));
        Assert.Equal("Delete closes this tab", cut.Find("[data-part=dock] > span[hidden]").TextContent);
    }

    [Fact]
    public async Task TelemetryName_ShouldCountSelectingClosingAndResizing_WhenGiven()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));

        await Tab(cut, "Map").ClickAsync();
        await cut.InvokeAsync(() => Report().OnResize(0, 0.4, 1));
        await cut.InvokeAsync(() => Report().OnClose("map", true));

        Assert.Equal(["select", "resize", "close"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Driver_ShouldReadSelectAndCloseThePanelsByTheirTitles_WhenUsed()
    {
        var cut = RenderDock(Hike, [new("trails", "Trails", Closable: false), .. HikePanels.Skip(1)]);
        var dock = cut.Tb().Dock();

        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
        Assert.Equal(["Trails", "Notes", "Weather"], dock.Showing);

        await dock.SelectAsync("Map");
        await dock.CloseAsync("Weather");

        Assert.Equal(["Trails", "Map", "Notes"], dock.Panels);
        Assert.Equal(["Map", "Notes"], dock.Showing);
        Assert.Equal("The panel \"Trails\" can't be closed.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.CloseAsync("Trails"))).Message);
        Assert.Equal("No panel is titled \"Photos\".",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.SelectAsync("Photos"))).Message);
    }

    [Fact]
    public async Task Driver_ShouldReachIntoAPanelsContent_WhenThePanelHasShown()
    {
        var added = new List<string>();
        RenderFragment Adding(string id) => builder =>
        {
            builder.OpenComponent<TbDockPanel>(0);
            builder.AddAttribute(1, nameof(TbDockPanel.Id), id);
            builder.AddAttribute(2, nameof(TbDockPanel.Title), id);
            builder.AddAttribute(3, nameof(TbDockPanel.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<TbButton>(0);
                content.AddAttribute(1, nameof(TbButton.OnClick),
                    EventCallback.Factory.Create<MouseEventArgs>(this, () => added.Add(id)));
                content.AddAttribute(2, nameof(TbButton.ChildContent),
                    (RenderFragment)(text => text.AddContent(0, "Add")));
                content.CloseComponent();
            }));
            builder.CloseComponent();
        };
        var cut = Render<TbDockManager>(parameters => parameters
            .Add(dock => dock.Layout,
                new DockLayout(new DockSplit(new DockGroup("trails", "map"), new DockGroup("notes"))))
            .Add(dock => dock.ChildContent, (RenderFragment)(builder =>
            {
                Adding("trails")(builder);
                Adding("map")(builder);
                Adding("notes")(builder);
            })));
        var dock = cut.Tb().Dock();

        await dock.Panel("notes").Button("Add").ClickAsync();

        Assert.Equal(["notes"], added);
        Assert.Equal("Add", dock.PanelElement("trails").TextContent.Trim());
        Assert.Equal("The content of the panel \"map\" isn't in the markup; select the panel first.",
            Assert.Throws<InvalidOperationException>(() => dock.PanelElement("map")).Message);
    }

    [Fact]
    public void Driver_ShouldReadOnlyTheDocksOwnTabs_WhenAPanelHoldsTabsOfItsOwn()
    {
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.ChildContent,
            (RenderFragment)(builder =>
            {
                builder.OpenComponent<TbDockPanel>(0);
                builder.AddAttribute(1, nameof(TbDockPanel.Id), "map");
                builder.AddAttribute(2, nameof(TbDockPanel.Title), "Map");
                builder.AddAttribute(3, nameof(TbDockPanel.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenComponent<TbTabs>(0);
                    content.AddAttribute(1, nameof(TbTabs.ChildContent), (RenderFragment)(tabs =>
                    {
                        tabs.OpenComponent<TbTab>(0);
                        tabs.AddAttribute(1, nameof(TbTab.Text), "Roads");
                        tabs.CloseComponent();
                    }));
                    content.CloseComponent();
                }));
                builder.CloseComponent();
            })));

        Assert.Equal(["Map"], cut.Tb().Dock().Panels);
        Assert.Equal(["Map"], cut.Tb().Dock().Showing);
    }

    [Fact]
    public async Task Driver_ShouldMoveASeparatorAsItsKeysDo_WhenGivenAPercent()
    {
        var changes = 0;
        var cut = RenderDock(Hike, HikePanels, _ => changes++);
        var dock = cut.Tb().Dock();

        await dock.ResizeAsync("Resize Trails", 25);
        Assert.Equal(0, changes);
        await dock.ResizeAsync("Resize Trails", 40);
        await dock.ResizeAsync("Resize Notes", 60);

        Assert.Equal((40, 60, 2), (dock.Size("Resize Trails"), dock.Size("Resize Notes"), changes));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dock.ResizeAsync("Resize Trails", 100));
        Assert.Equal("No separator is named \"Resize Map\".",
            Assert.Throws<InvalidOperationException>(() => dock.Size("Resize Map")).Message);
    }

    [Fact]
    public void Driver_ShouldFindADockByItsName_WhenThePageHasTwo()
    {
        var cut = Render(builder =>
        {
            foreach (var name in new[] { "Hike planner", "Gear planner" })
            {
                builder.OpenComponent<TbDockManager>(0);
                builder.SetKey(name);
                builder.AddAttribute(1, nameof(TbDockManager.AriaLabel), name);
                builder.AddAttribute(2, nameof(TbDockManager.ChildContent),
                    Panels(new Panel(name == "Hike planner" ? "trails" : "tents", name)));
                builder.CloseComponent();
            }
        });

        Assert.Equal(["Gear planner"], cut.Tb().Dock("Gear planner").Panels);
        Assert.Equal("Expected one dock, found 2; name the one to use.",
            Assert.Throws<InvalidOperationException>(() => cut.Tb().Dock().Panels).Message);
        Assert.Equal("Expected one dock named \"Planner\", found 0.",
            Assert.Throws<InvalidOperationException>(() => cut.Tb().Dock("Planner").Panels).Message);
    }

    [Fact]
    public async Task Driver_ShouldMoveItsOwnDocksSeparator_WhenTwoDocksUseTheSamePanelIds()
    {
        var cut = Render(builder =>
        {
            foreach (var name in new[] { "Hike planner", "Gear planner" })
            {
                builder.OpenComponent<TbDockManager>(0);
                builder.SetKey(name);
                builder.AddAttribute(1, nameof(TbDockManager.AriaLabel), name);
                builder.AddAttribute(2, nameof(TbDockManager.Layout), Hike);
                builder.AddAttribute(3, nameof(TbDockManager.ChildContent), Panels(HikePanels));
                builder.CloseComponent();
            }
        });

        await cut.Tb().Dock("Hike planner").ResizeAsync("Resize Trails", 40);

        Assert.Equal((40, 25),
            (cut.Tb().Dock("Hike planner").Size("Resize Trails"), cut.Tb().Dock("Gear planner").Size("Resize Trails")));
    }

    [Fact]
    public void SplitterDriver_ShouldLeaveADocksSeparatorsAlone_WhenOneHasTheNameAskedFor()
    {
        var cut = RenderDock(Hike, HikePanels);

        Assert.Equal("No separator is named \"Resize Trails\".",
            Assert.Throws<InvalidOperationException>(() => cut.Tb().Splitter("Resize Trails").Size).Message);
    }

    [Fact]
    public void Panel_ShouldThrow_WhenItIsOutsideADock()
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => Render<TbDockPanel>(parameters => parameters
            .Add(panel => panel.Id, "map").Add(panel => panel.Title, "Map")));

        Assert.Equal("TbDockPanel must be inside a TbDockManager.", thrown.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("trail map")]
    public void Panel_ShouldThrow_WhenItsIdIsEmptyOrHasWhiteSpace(string id)
    {
        var thrown = Assert.Throws<InvalidOperationException>(() => RenderDock(null, new Panel(id, "Map")));

        Assert.Equal("A TbDockPanel needs an Id that isn't empty and has no white space in it.", thrown.Message);
    }

    [Fact]
    public void Panel_ShouldThrow_WhenAnotherPanelHasItsId()
    {
        var thrown = Assert.Throws<InvalidOperationException>(
            () => RenderDock(null, new Panel("map", "Map"), new Panel("map", "Trail map")));

        Assert.Equal("Two panels of a TbDockManager have the id 'map'.", thrown.Message);
    }

    [Fact]
    public void Panel_ShouldThrow_WhenItsIdChanges()
    {
        RenderFragment Named(string id) => builder =>
        {
            builder.OpenComponent<TbDockPanel>(0);
            builder.AddAttribute(1, nameof(TbDockPanel.Id), id);
            builder.AddAttribute(2, nameof(TbDockPanel.Title), "Map");
            builder.CloseComponent();
        };
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.ChildContent, Named("map")));

        var thrown = Assert.Throws<InvalidOperationException>(() => cut.Render(parameters => parameters
            .Add(dock => dock.ChildContent, Named("chart"))));

        Assert.Equal("A TbDockPanel's Id can't change.", thrown.Message);
    }

    private sealed record Panel(string Id, string Title, bool Closable = true, bool KeepAlive = true);

    private IRenderedComponent<TbDockManager> RenderDock(DockLayout? layout, params Panel[] panels) =>
        RenderDock(layout, panels, null);

    private IRenderedComponent<TbDockManager> RenderDock(DockLayout? layout, Panel[] panels,
        Action<DockLayout>? onChange) =>
        Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, layout)
            .Add(dock => dock.LayoutChanged, changed => onChange?.Invoke(changed))
            .Add(dock => dock.ChildContent, Panels(panels)));

    // Each panel holds a probe that logs when it starts, renders and is disposed.
    private RenderFragment Panels(params Panel[] panels) => builder =>
    {
        foreach (var panel in panels)
        {
            builder.OpenComponent<TbDockPanel>(0);
            builder.SetKey(panel.Id);
            builder.AddAttribute(1, nameof(TbDockPanel.Id), panel.Id);
            builder.AddAttribute(2, nameof(TbDockPanel.Title), panel.Title);
            builder.AddAttribute(3, nameof(TbDockPanel.Closable), panel.Closable);
            builder.AddAttribute(4, nameof(TbDockPanel.KeepAlive), panel.KeepAlive);
            builder.AddAttribute(5, nameof(TbDockPanel.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<Probe>(0);
                content.AddAttribute(1, nameof(Probe.Name), panel.Id);
                content.AddAttribute(2, nameof(Probe.Log), _log);
                content.CloseComponent();
            }));
            builder.CloseComponent();
        }
    };

    private DockReport Report() =>
        ((DotNetObjectReference<DockReport>)_module.Invocations["attachDock"].Single().Arguments[1]!).Value;

    private static AngleSharp.Dom.IElement Tab(IRenderedComponent<TbDockManager> cut, string title) =>
        cut.FindAll("[role=tab]").Single(tab => tab.TextContent.Trim() == title);

    // bUnit shows an element's reference only in the render that made the element, so a test reads it before the
    // dock renders again.
    private static string? Reference(IRenderedComponent<TbDockManager> cut, string title) =>
        Tab(cut, title).GetAttribute("blazor:elementreference");

    private static IEnumerable<string> Titles(AngleSharp.Dom.IElement group) =>
        group.QuerySelectorAll("[role=tab]").Select(tab => tab.TextContent.Trim());

    private static IEnumerable<string?> Parts(IRenderedComponent<TbDockManager> cut) =>
        cut.FindAll("[data-part=dock] > [data-part]").Select(part => part.GetAttribute("data-part"));

    // A panel by its id, and a group or a separator by its part.
    private static string Key(AngleSharp.Dom.IElement part) =>
        part.GetAttribute("data-panel") ?? part.GetAttribute("data-part")!;

    private sealed class Probe : ComponentBase, IDisposable
    {
        [Parameter]
        public string Name { get; set; } = "";

        [Parameter]
        public List<string> Log { get; set; } = null!;

        protected override void OnInitialized() => Log.Add($"init {Name}");

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Log.Add($"render {Name}");
            builder.AddContent(0, Name);
        }

        public void Dispose() => Log.Add($"dispose {Name}");
    }
}
