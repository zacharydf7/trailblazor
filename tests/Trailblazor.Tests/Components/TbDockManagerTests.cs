using System.Diagnostics.Metrics;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Testing;
using Trailblazor.Internal.Events;

namespace Trailblazor.Tests.Components;

public class TbDockManagerTests : BunitContext
{
    // Trails and Map share a group beside the notes, which sit above the weather.
    private static readonly DockLayout Hike = new(new DockSplit(
        new DockGroup("trails", "map") { Size = 1 },
        new DockSplit(new DockGroup("notes") { Size = 3 }, new DockGroup("weather")) { Vertical = true, Size = 3 }));

    // The dock's children that the script keeps in reading order, which leaves out the mark for a dragged tab.
    private const string OrderedParts = "[data-part=dock] > [data-part]:not([data-part=drop-mark])";

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

        Assert.Equal(["group", "panel", "separator", "grip", "group", "panel", "separator", "grip", "group", "panel"],
            Parts(cut));
    }

    [Fact]
    public void Render_ShouldPutEveryPanelInOneGroup_WhenGivenNoLayout()
    {
        var cut = RenderDock(null, HikePanels);

        Assert.Equal(["Trails", "Map", "Notes", "Weather"], Titles(cut.Find("[data-part=group]")));
        Assert.Empty(cut.FindAll(".tb-dock__separator"));
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
        Assert.Equal(("group", "Hike planner", "tb-dock", "120"),
            (root.GetAttribute("role"), root.GetAttribute("aria-label"), root.ClassName,
                root.GetAttribute("data-min-size")));
        // The frame around the dock's area and its strips is the dock's outermost element.
        var frame = cut.Find("[data-part=frame]");
        Assert.Equal(("tb-dock-frame planner", "height: 30rem", "dock"),
            (frame.ClassName, frame.GetAttribute("style"), frame.Id));
        Assert.Equal("frame", root.ParentElement!.GetAttribute("data-part"));
        Assert.Empty(cut.FindAll("[data-part=strip]"));
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
        Assert.Single(cut.FindAll(".tb-dock__separator"));
        Assert.Equal(["group", "panel", "separator", "grip", "group", "panel"], Parts(cut));
    }

    [Fact]
    public async Task Close_ShouldFocusTheNextTabOrElseTheOneBefore_WhenATabOfAGroupCloses()
    {
        // The map shows, and the notes show once it has closed.
        var cut = RenderDock(new DockLayout(new DockGroup("trails", "map", "notes") { Active = "map" }),
            HikePanels.Take(3).ToArray());
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
    public async Task TabMenu_ShouldMoveTheFocusOnlyWhenTheClosedTabHeldIt_WhenCloseIsChosen()
    {
        var cut = RenderDock(new DockLayout(new DockGroup("trails", "map", "notes", "weather")), HikePanels);
        var dock = cut.Tb().Dock();
        var trails = Reference(cut, "Trails");

        // The script finds the focus elsewhere, as after a right-click, which leaves it where it was.
        await dock.ChooseFromTabMenuAsync("Weather", "Close");
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier.EndsWith("focus"));

        // A tab that isn't showing can hold the focus, and then the row's showing tab takes it, not Notes, the tab
        // beside the closed one.
        _handle.Setup<bool>("holdsFocus", _ => true).SetResult(true);
        await dock.ChooseFromTabMenuAsync("Map", "Close");

        Assert.Equal(["Trails", "Notes"], dock.Panels);
        Assert.Equal(trails, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
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

        var separators = cut.FindAll(".tb-dock__separator");
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

        var separators = cut.FindAll(".tb-dock__separator");
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
        Assert.Equal("40", cut.FindAll(".tb-dock__separator")[0].GetAttribute("aria-valuenow"));
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-report"));
        Assert.Equal([1.6, 2.4],
            Assert.IsType<DockSplit>(changed!.Root).Children.Select(child => Math.Round(child.Size, 4)));
    }

    [Fact]
    public async Task Resize_ShouldNumberTheRenderThatFirstShowsTheNewSizes_WhenTheAppBindsTheLayout()
    {
        IRenderedComponent<TbDockManager> cut = null!;
        var renders = new List<(string? Report, string? FirstGroup)>();
        // As @bind-Layout does, the app hands the new layout back, which renders the dock before OnResize is done.
        cut = RenderDock(Hike, HikePanels, layout =>
        {
            cut.Render(parameters => parameters.Add(dock => dock.Layout, layout));
            renders.Add((cut.Find("[data-part=dock]").GetAttribute("data-report"),
                cut.FindAll("[data-part=group]")[0].GetAttribute("style")));
        });

        await cut.InvokeAsync(() => Report().OnResize(0, 0.4, 1));

        // The script shows its own boxes over the app's until it sees the report's number, so the two arrive together.
        var (report, firstGroup) = Assert.Single(renders);
        Assert.Contains("--tb-dock-w: 40;", firstGroup);
        Assert.Equal("1", report);
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
        var before = cut.FindAll(OrderedParts).Select(Key).ToList();

        // The weather joins the first group and shows there, so its group and a separator go.
        cut.Render(parameters => parameters.Add(dock => dock.Layout, new DockLayout(new DockSplit(
            new DockGroup("trails", "map", "weather") { Active = "weather" }, new DockGroup("notes")))));

        Assert.Equal(["group", "trails", "separator", "grip", "group", "notes", "weather"],
            cut.FindAll(OrderedParts).Select(Key));
        Assert.Equal(["group", "trails", "separator", "grip", "group", "notes", "separator", "grip", "group", "weather"],
            before);
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
        Assert.Equal("Size of Trails", cut.Find(".tb-dock__separator").GetAttribute("aria-label"));
        Assert.Equal("Delete closes this tab", cut.Find("[data-part=dock] > span[hidden]").TextContent);
    }

    [Fact]
    public void Menu_ShouldOfferWhatItsGroupsShowingPanelCanDo_WhenRendered()
    {
        var cut = RenderDock(Hike, [new("trails", "Trails"), new("map", "Map"), new("notes", "Notes", Closable: false),
            new("weather", "Weather")]);

        var menus = cut.FindAll("[data-part=group-menu]");
        Assert.Equal(["Options for Trails", "Options for Notes", "Options for Weather"],
            menus.Select(menu => menu.QuerySelector("button")!.GetAttribute("aria-label")));
        // Trails is the first of two tabs, so it can go forward but not back.
        Assert.Equal(("true", null), (menus[0].QuerySelector("[data-move=back]")!.GetAttribute("aria-disabled"),
            menus[0].QuerySelector("[data-move=forward]")!.GetAttribute("aria-disabled")));
        Assert.Equal(["Group 2: Notes", "Group 3: Weather"],
            menus[0].QuerySelectorAll("[data-move-group]").Select(item => item.TextContent.Trim()));
        Assert.Equal(["At the start", "At the end", "Above", "Below"],
            menus[0].QuerySelectorAll("[data-move-beside]").Select(item => item.TextContent.Trim()));
        Assert.Equal(["At the start", "At the end", "At the top", "At the bottom"],
            menus[0].QuerySelectorAll("[data-move-edge]").Select(item => item.TextContent.Trim()));
        Assert.Equal("Close", menus[0].QuerySelector("[data-move=close]")!.TextContent.Trim());
        // The notes are alone in their group, with no group of their own to split, and they can't close.
        Assert.Empty(menus[1].QuerySelectorAll("[data-move-beside]"));
        Assert.Equal(4, menus[1].QuerySelectorAll("[data-move-edge]").Length);
        Assert.Null(menus[1].QuerySelector("[data-move=close]"));
    }

    [Fact]
    public void Menu_ShouldOfferNoMoves_WhenTheDockIsLocked()
    {
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Locked, true));

        var menu = cut.FindAll("[data-part=group-menu]")[0];
        Assert.Empty(menu.QuerySelectorAll(
            "[data-move=back], [data-move=forward], [data-move=float], [data-move=pop-out], [data-move=dock], "
            + "[data-move-group], [data-move-beside], [data-move-edge], [data-move-hide]"));
        Assert.Equal(["Trails", "Map"], menu.QuerySelectorAll("[data-go-to]").Select(item => item.TextContent.Trim()));
        Assert.NotNull(menu.QuerySelector("[data-move=maximize]"));
        Assert.NotNull(menu.QuerySelector("[data-move=close]"));
        Assert.Equal("", cut.Find("[data-part=dock]").GetAttribute("data-locked"));
    }

    [Fact]
    public void Menu_ShouldNotShow_WhenTheDockIsLockedAndItsOnlyPanelCantCloseOrFillTheDock()
    {
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Locked, true)
            .Add(dock => dock.Layout, new DockLayout(new DockGroup("notes")))
            .Add(dock => dock.ChildContent, Panels(new Panel("notes", "Notes", Closable: false))));

        Assert.Empty(cut.FindAll("[data-part=group-menu]"));
        Assert.True(Assert.Single(cut.FindComponents<TbContextMenu>()).Instance.Disabled);
    }

    [Fact]
    public async Task Helpers_ShouldRefuseToMoveAPanelButStillMaximize_WhenTheDockIsLocked()
    {
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Locked, true));
        var dock = cut.Tb().Dock();

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => dock.FloatAsync("Trails"));
        await dock.MaximizeAsync("Trails");

        Assert.True(dock.IsLocked);
        Assert.Contains("Locked", refused.Message);
        Assert.NotEmpty(cut.FindAll("[data-part=restore]"));
    }

    [Fact]
    public void Tab_ShouldShowItsEndAfterTheTitleAndNameTheTabByBoth_WhenThePanelHasTabEnd()
    {
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.ChildContent, (RenderFragment)(builder =>
            {
                foreach (var (id, title) in new[] { ("trails", "Trails"), ("map", "Map"), ("notes", "Notes") })
                {
                    builder.OpenComponent<TbDockPanel>(0);
                    builder.SetKey(id);
                    builder.AddComponentParameter(1, nameof(TbDockPanel.Id), id);
                    builder.AddComponentParameter(2, nameof(TbDockPanel.Title), title);
                    if (id == "trails")
                    {
                        builder.AddComponentParameter(3, nameof(TbDockPanel.TabEnd),
                            (RenderFragment)(end => end.AddContent(0, "2 new")));
                    }

                    builder.CloseComponent();
                }
            })));

        var trails = cut.Find("[role=tab][data-panel=trails]");
        var title = trails.QuerySelector("[data-part=title]")!;
        var end = trails.QuerySelector("[data-part=title] + [data-part=tab-end]")!;
        Assert.Equal("2 new", end.TextContent);
        Assert.Null(end.GetAttribute("aria-hidden"));
        Assert.Null(trails.GetAttribute("aria-labelledby"));
        Assert.Equal(title.Id, cut.Find($"[role=tabpanel][id='{trails.GetAttribute("aria-controls")}']")
            .GetAttribute("aria-labelledby"));
        Assert.Null(Tab(cut, "Map").QuerySelector("[data-part=title]")!.GetAttribute("id"));
        Assert.Equal(["Trails", "Map", "Notes"], cut.Tb().Dock().Panels);
    }

    [Fact]
    public async Task ActivePanel_ShouldBeTheEngagedGroupsShowingPanel_WhenTheScriptReportsAPressOrTheFocus()
    {
        var reported = new List<string?>();
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.ActivePanelChanged, (string? panel) => reported.Add(panel))
            .Add(dock => dock.ChildContent, Panels(HikePanels)));
        Assert.Null(cut.Tb().Dock().ActivePanel);

        await cut.InvokeAsync(() => Report().OnEngage("notes"));
        await cut.InvokeAsync(() => Report().OnEngage("photos"));

        Assert.Equal(["notes"], reported);
        Assert.Equal("Notes", cut.Tb().Dock().ActivePanel);
        Assert.Equal("notes", cut.Find("[data-part=dock]").GetAttribute("data-active-panel"));
    }

    [Fact]
    public async Task ActivePanel_ShouldFollowItsGroupsShowingTab_WhenAnotherTabIsSelectedOrItCloses()
    {
        var reported = new List<string?>();
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.ActivePanelChanged, (string? panel) => reported.Add(panel))
            .Add(dock => dock.ChildContent, Panels(HikePanels)));
        var dock = cut.Tb().Dock();
        await cut.InvokeAsync(() => Report().OnEngage("trails"));

        await dock.SelectAsync("Map");
        await dock.SelectAsync("Notes");
        await dock.CloseAsync("Map");

        Assert.Equal(["trails", "map", "trails"], reported);
        Assert.Equal("Trails", dock.ActivePanel);
    }

    [Fact]
    public void ActivePanel_ShouldBeTheAppsPanel_WhenTheAppSetsIt()
    {
        var changes = 0;
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.LayoutChanged, _ => changes++)
            .Add(dock => dock.ActivePanel, "weather").Add(dock => dock.ChildContent, Panels(HikePanels)));

        Assert.Equal("Weather", cut.Tb().Dock().ActivePanel);
        Assert.Equal(0, changes);
        Assert.DoesNotContain(JSInterop.Invocations,
            call => call.Identifier.Contains("focus", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ActivePanel_ShouldFollowWhatTheDockShows_WhenTheAppsLayoutOrACompactDockMovesIt()
    {
        var reported = new List<string?>();
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.ActivePanelChanged, (string? panel) => reported.Add(panel))
            .Add(dock => dock.ChildContent, Panels(HikePanels)));
        var dock = cut.Tb().Dock();
        await cut.InvokeAsync(() => Report().OnEngage("trails"));

        // The app shows another tab of the active group, then closes that tab.
        cut.Render(parameters => parameters.Add(dock => dock.Layout, Hike.Open("map")));
        cut.Render(parameters => parameters.Add(dock => dock.Layout, Hike.Close("map")));
        // A compact dock shows every docked panel in one group, so another of its tabs takes the part.
        cut.Render(parameters => parameters.Add(dock => dock.CompactBelow, Breakpoint.Xl));
        await dock.NarrowAsync();
        await dock.SelectAsync("Notes");
        await dock.NarrowAsync(false);
        await cut.InvokeAsync(() => Report().OnEngage("weather"));
        await dock.CloseAsync("Weather");

        Assert.Equal(["trails", "map", "trails", "notes", "weather", null], reported);
        Assert.Null(dock.ActivePanel);
    }

    [Fact]
    public async Task GoToTab_ShouldShowThatPanel_WhenChosenInTheGroupsMenu()
    {
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);

        await cut.Find("[data-part=group-menu] [data-go-to=map]").ClickAsync();

        Assert.Equal("map", changed!.GroupOf("map")!.Active);
        Assert.Equal("true", Tab(cut, "Map").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task Drop_ShouldKeepTheLayoutAndStillCount_WhenTheDockIsLocked()
    {
        var changes = 0;
        var cut = RenderDock(Hike, HikePanels, _ => changes++);
        cut.Render(parameters => parameters.Add(dock => dock.Locked, true));

        await cut.InvokeAsync(() => Report().OnMove("map", "weather", "Top"));
        await cut.InvokeAsync(() => Report().OnPullOut("map", 10, 10, 50, 50));
        await cut.InvokeAsync(() => Report().OnAutoHide("map", "Start"));

        Assert.Equal(0, changes);
        Assert.Equal([["Trails", "Map"], ["Notes"], ["Weather"]], cut.FindAll("[data-part=group]").Select(Titles));
        Assert.Equal("3", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task Menu_ShouldMoveTheTabAlongItsRowAndSayWhereItWent_WhenForwardIsChosen()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var trails = Reference(cut, "Trails");

        await cut.Find("[data-part=group-menu] [data-move=forward]").ClickAsync();

        Assert.Equal(["Map", "Trails"], Titles(cut.FindAll("[data-part=group]")[0]));
        Assert.Equal(Hike.Move("trails", "map", DockPosition.After), changed);
        Assert.Equal(["Trails moved to panel group 1."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(trails, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
        Assert.Equal(["move"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Drop_ShouldMoveThePanelBesideTheTargetAndSayWhereItWent_WhenTheScriptReportsADroppedTab()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        // The mark the script shows where the tab would land is in the markup from the start, hidden.
        Assert.Equal("true", cut.Find("[data-part=dock] > [data-part=drop-mark]").GetAttribute("aria-hidden"));

        var map = ReferenceOnceMade(cut, "Map");

        await cut.InvokeAsync(() => Report().OnMove("map", "weather", "Top"));

        Assert.Equal([["Trails"], ["Notes"], ["Map"], ["Weather"]], cut.FindAll("[data-part=group]").Select(Titles));
        Assert.Equal(Hike.Move("map", "weather", DockPosition.Top), changed);
        Assert.Equal(["Map moved to panel group 3."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(map(), ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
        Assert.Equal(["move"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Drop_ShouldMoveThePanelAlongTheDocksEdge_WhenTheScriptReportsNoTarget()
    {
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);

        await cut.InvokeAsync(() => Report().OnMove("notes", null, "Start"));

        Assert.Equal(Hike.MoveToEdge("notes", DockPosition.Start), changed);
        Assert.Equal(["Notes", "Trails", "Map", "Weather"], cut.Tb().Dock().Panels);
    }

    [Theory]
    [InlineData("photos", "notes", "After")]
    [InlineData("map", "photos", "After")]
    [InlineData("map", "notes", "Sideways")]
    [InlineData("map", "notes", "9")]
    [InlineData("map", null, "Before")]
    public async Task Drop_ShouldChangeNothing_WhenTheReportNamesNoSuchPanelOrPlace(string panel, string? target,
        string position)
    {
        var changes = 0;
        var cut = RenderDock(Hike, HikePanels, _ => changes++);

        await cut.InvokeAsync(() => Report().OnMove(panel, target, position));

        Assert.Equal(0, changes);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], cut.Tb().Dock().Panels);
        // The drop still counts as finished, so a test that waits for it goes on.
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task PullOut_ShouldFloatThePanelInTheBoxTheScriptGives_WhenATabIsDroppedOnItsOwnGroup()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var changes = new List<DockLayout>();
        var cut = RenderDock(Hike, HikePanels, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var notes = ReferenceOnceMade(cut, "Notes");

        await cut.InvokeAsync(() => Report().OnPullOut("notes", 41.252191, 37.5, 50, 50));

        // The layout keeps the four decimals the window's style shows.
        Assert.Equal([Hike.FloatAt("notes", 41.2522, 37.5, 50, 50)], changes);
        Assert.Equal(["Notes"], cut.Tb().Dock().Floating);
        Assert.Equal((41.2522, 37.5, 50, 50), cut.Tb().Dock().Window("Notes"));
        // As after Float in a menu: the tab takes the focus, and the dock says and counts what happened.
        Assert.Equal(notes(), ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
        Assert.Equal(["Notes floats in a window."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(["float"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task PullOut_ShouldChangeNothingButCountTheDrop_WhenTheReportNamesNoSuchPanel()
    {
        var changes = 0;
        var cut = RenderDock(Hike, HikePanels, _ => changes++);

        await cut.InvokeAsync(() => Report().OnPullOut("lake", 10, 10, 50, 50));

        Assert.Equal(0, changes);
        Assert.Empty(cut.Tb().Dock().Floating);
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task TabMenu_ShouldOfferWhatTheTabItOpenedOnCanDo_WhenOpenedOnATabOrOnTheRestOfItsRow()
    {
        var cut = RenderDock(Hike, [new("trails", "Trails"), new("map", "Map", Closable: false), .. HikePanels[2..]]);
        var area = cut.Find(".tb-dock__tab-area");
        string[] Items() => [.. cut.Find(".tb-dock__tab-area > [role=menu]").QuerySelectorAll("[data-move]")
            .Select(item => $"{item.GetAttribute("data-move")} {item.GetAttribute("aria-disabled") ?? "false"}")];

        // Every tab carries its panel's id for the menu, and the row carries the showing panel's.
        Assert.Equal(["trails", "trails", "map"],
            area.QuerySelectorAll("[data-context-key]").Select(keyed => keyed.GetAttribute("data-context-key")));
        Assert.Equal("Tab options", cut.Find(".tb-dock__tab-area > [role=menu]").GetAttribute("aria-label"));
        // Nothing is drawn until the menu opens on a tab.
        Assert.Empty(Items());

        // The map is the last of two tabs and can't close.
        await area.TriggerEventAsync("ontbcontextmenuopen", new ContextMenuOpenEventArgs { Key = "map", Request = 1 });
        Assert.Equal(["back false", "forward true", "float false", "maximize false"], Items());
        await cut.Find(".tb-dock__tab-area").TriggerEventAsync("ontbcontextmenuopen",
            new ContextMenuOpenEventArgs { Key = "trails", Request = 2 });
        Assert.Equal(["back true", "forward false", "float false", "maximize false", "close false"], Items());
        // A panel of another group has no menu in this row.
        await cut.Find(".tb-dock__tab-area").TriggerEventAsync("ontbcontextmenuopen",
            new ContextMenuOpenEventArgs { Key = "notes", Request = 3 });
        Assert.Empty(Items());
    }

    [Fact]
    public async Task TabMenu_ShouldMoveOrCloseTheTabItOpenedOn_WhenThatTabIsNotTheShowingOne()
    {
        var changes = new List<DockLayout>();
        var cut = RenderDock(Hike, HikePanels, changes.Add);
        var dock = cut.Tb().Dock();

        // The map isn't showing, and its own menu moves it without selecting it first.
        await dock.ChooseFromTabMenuAsync("Map", "Move to group", "Group 3: Weather");

        Assert.Equal([Hike.Move("map", "weather", DockPosition.After)], changes);
        Assert.Equal(["Trails", "Notes", "Weather", "Map"], dock.Panels);
        await dock.ChooseFromTabMenuAsync("Weather", "Close");
        Assert.Equal(["Trails", "Notes", "Map"], dock.Panels);
        Assert.Equal(["Trails", "Notes", "Map"], dock.Showing);
        Assert.Equal("No panel is titled \"Photos\".", (await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.ChooseFromTabMenuAsync("Photos", "Close"))).Message);
    }

    [Fact]
    public async Task Render_ShouldLeaveOutTheMenusAndGrips_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderDock(Hike, HikePanels);

        // Their items need the server, so a static page shows only the tabs, panels and separators.
        Assert.Empty(cut.FindAll("[data-part=group-menu], [data-part=grip]"));
        Assert.Equal(2, cut.FindAll("[role=separator]").Count);
        // The script states a separator's limits, so a static page has none.
        Assert.DoesNotContain(cut.FindAll("[role=separator]"),
            separator => separator.HasAttribute("aria-valuemin") || separator.HasAttribute("aria-valuemax"));
        Assert.Equal("The dock is on a static page, where it has no menus; render it on an interactive page.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Dock().MoveToGroupAsync("Trails", 2)))
                .Message);
    }

    [Fact]
    public async Task Menu_ShouldStillSayWhereThePanelWent_WhenTheAppTakesThePanelAwayWhileItHearsOfTheMove()
    {
        var heard = new TaskCompletionSource();
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.LayoutChanged, _ => heard.Task)
            .Add(dock => dock.ChildContent, Panels(HikePanels)));

        var click = cut.Find("[data-part=group-menu] [data-move-group='3']").ClickAsync();
        cut.Render(parameters => parameters.Add(dock => dock.ChildContent, Panels(HikePanels[1..])));
        heard.SetResult();
        await click;

        Assert.Equal(["Trails moved to panel group 3."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(["Map", "Notes", "Weather"], cut.FindAll("[role=tab]").Select(tab => tab.TextContent.Trim()));
    }

    [Fact]
    public async Task Menu_ShouldNumberTheGroupAsItsTabListIsNamed_WhenTheLayoutHoldsAGroupThatDoesNotShow()
    {
        // The app has no Trails or Map panel, so their group doesn't show and Notes has the first group.
        var cut = RenderDock(Hike, HikePanels[2..]);

        await cut.FindAll("[data-part=group-menu]")[1].QuerySelector("[data-move-group='1']")!.ClickAsync();

        Assert.Equal(["Weather moved to panel group 1."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal("Panel group 1", cut.Find("[role=tablist]").GetAttribute("aria-label"));
    }

    [Fact]
    public async Task Menu_ShouldMoveThePanelToTheEndOfAnotherGroup_WhenThatGroupIsChosen()
    {
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);

        await cut.Find("[data-part=group-menu] [data-move-group='3']").ClickAsync();

        Assert.Equal([["Map"], ["Notes"], ["Weather", "Trails"]], cut.FindAll("[data-part=group]").Select(Titles));
        Assert.Equal("true", Tab(cut, "Trails").GetAttribute("aria-selected"));
        Assert.Equal(Hike.Move("trails", "weather", DockPosition.After), changed);
        Assert.Equal(["Trails moved to panel group 3."], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Theory]
    [InlineData("start", DockPosition.Start)]
    [InlineData("end", DockPosition.End)]
    [InlineData("top", DockPosition.Top)]
    [InlineData("bottom", DockPosition.Bottom)]
    public async Task Menu_ShouldGiveThePanelANewGroup_WhenASideOfItsGroupOrOfTheDockIsChosen(string side,
        DockPosition position)
    {
        var changes = new List<DockLayout>();

        await RenderDock(Hike, HikePanels, changes.Add).Find($"[data-part=group-menu] [data-move-beside={side}]")
            .ClickAsync();
        await RenderDock(Hike, HikePanels, changes.Add).Find($"[data-part=group-menu] [data-move-edge={side}]")
            .ClickAsync();

        Assert.Equal([Hike.Move("trails", "map", position), Hike.MoveToEdge("trails", position)], changes);
    }

    [Fact]
    public async Task Menu_ShouldCloseItsGroupsShowingPanel_WhenCloseIsChosen()
    {
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);

        await cut.FindAll("[data-part=group-menu]")[1].QuerySelector("[data-move=close]")!.ClickAsync();

        Assert.Equal(["notes"], changed!.Closed);
        Assert.Equal([["Trails", "Map"], ["Weather"]], cut.FindAll("[data-part=group]").Select(Titles));
    }

    [Fact]
    public void Menu_ShouldHaveNoWayToMove_WhenTheDockHoldsOnePanel()
    {
        var cut = RenderDock(null, new Panel("map", "Map"));

        var menu = cut.Find("[data-part=group-menu]");
        Assert.Empty(menu.QuerySelectorAll("[data-move-group], [data-move-beside], [data-move-edge]"));
        Assert.Equal(["true", "true"], menu.QuerySelectorAll("[data-move=back], [data-move=forward]")
            .Select(item => item.GetAttribute("aria-disabled")));
    }

    [Fact]
    public async Task Grip_ShouldStepItsSeparatorThroughTheScript_WhenLargerOrSmallerIsChosen()
    {
        var cut = RenderDock(Hike, HikePanels);

        var grips = cut.FindAll("[data-part=grip]");
        Assert.Equal(["Size of Trails", "Size of Notes"],
            grips.Select(grip => grip.QuerySelector("button")!.GetAttribute("aria-label")));
        // The separator's keys do what the grip's menu does, so the grip stays out of the tab order.
        Assert.All(grips, grip => Assert.Equal("-1", grip.QuerySelector("button")!.GetAttribute("tabindex")));
        Assert.Equal(["Make Trails larger", "Make Trails smaller"],
            grips[0].QuerySelectorAll("[role=menuitem]").Select(item => item.TextContent.Trim()));
        Assert.Equal("tb-menu tb-dock__grip tb-dock__grip--vertical", grips[1].ClassName);

        await grips[0].QuerySelector("[data-step=larger]")!.ClickAsync();
        await cut.FindAll("[data-part=grip]")[1].QuerySelector("[data-step=smaller]")!.ClickAsync();

        Assert.Equal([[0, 10], [1, -10]], _handle.Invocations["step"].Select(call => call.Arguments.Cast<int>()));
    }

    [Fact]
    public void PersistenceKey_ShouldShowTheSavedLayoutAndHandItToTheApp_WhenTheBrowserHoldsOne()
    {
        var saved = Hike.Open("map").Close("weather");
        _handle.Setup<string?>("saved", _ => true).SetResult(saved.ToJson());
        var changes = new List<DockLayout>();

        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.PersistenceKey, "hike").Add(dock => dock.LayoutChanged, changes.Add)
            .Add(dock => dock.ChildContent, Panels(HikePanels)));

        Assert.Equal("hike", _handle.Invocations["saved"].Single().Arguments[0]);
        Assert.Equal([saved], changes);
        Assert.Equal("true", Tab(cut, "Map").GetAttribute("aria-selected"));
        Assert.Equal([["Trails", "Map"], ["Notes"]], cut.FindAll("[data-part=group]").Select(Titles));
        // What was read isn't written back.
        Assert.Empty(_handle.Invocations["save"]);
    }

    [Fact]
    public async Task PersistenceKey_ShouldNotHandTheSavedLayoutToTheApp_WhenTheBrowserAnswersAfterTheDockWent()
    {
        var answer = _handle.Setup<string?>("saved", _ => true);
        var changes = new List<DockLayout>();
        Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.PersistenceKey, "hike").Add(dock => dock.LayoutChanged, changes.Add)
            .Add(dock => dock.ChildContent, Panels(HikePanels)));

        await DisposeComponentsAsync();
        answer.SetResult(Hike.Close("weather").ToJson());
        await Renderer.Dispatcher.InvokeAsync(() => { });

        Assert.Empty(changes);
    }

    [Fact]
    public void RestoreLayout_ShouldChangeTheSavedLayoutBeforeItShowsAndSaveItsAnswer_WhenTheBrowserHoldsOne()
    {
        // The browser holds a layout from when the app still had a Weather panel.
        var saved = Hike.Close("weather");
        _handle.Setup<string?>("saved", _ => true).SetResult(saved.ToJson());
        var changes = new List<DockLayout>();
        string[] kept = ["trails", "map", "notes"];

        Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.PersistenceKey, "hike").Add(dock => dock.LayoutChanged, changes.Add)
            .Add(dock => dock.RestoreLayout, layout => layout.Retain(kept))
            .Add(dock => dock.ChildContent, Panels(HikePanels[..3])));

        // The app hears the layout as the hook left it, and so does the browser, which forgets the panel too.
        var restored = Assert.Single(changes);
        Assert.Equal(saved.Retain(kept), restored);
        Assert.Empty(restored.Closed);
        Assert.Equal(restored.ToJson(), _handle.Invocations["save"].Single().Arguments[1]);
    }

    [Fact]
    public void RestoreLayout_ShouldSaveNothing_WhenItLeavesTheSavedLayoutAsItWas()
    {
        var saved = Hike.Open("map");
        _handle.Setup<string?>("saved", _ => true).SetResult(saved.ToJson());
        var changes = new List<DockLayout>();

        Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.PersistenceKey, "hike").Add(dock => dock.LayoutChanged, changes.Add)
            .Add(dock => dock.RestoreLayout, layout => layout.Retain(HikePanels.Select(panel => panel.Id)))
            .Add(dock => dock.ChildContent, Panels(HikePanels)));

        // Nothing in the saved layout was stale, so the app hears it once and the browser is left alone.
        Assert.Equal([saved], changes);
        Assert.Empty(_handle.Invocations["save"]);
    }

    [Fact]
    public void RestoreLayout_ShouldNotBeAsked_WhenTheBrowserHoldsNoLayout()
    {
        _handle.Setup<string?>("saved", _ => true).SetResult(null);
        var asked = 0;

        Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.PersistenceKey, "hike").Add(dock => dock.RestoreLayout, layout =>
            {
                asked++;
                return layout;
            }).Add(dock => dock.ChildContent, Panels(HikePanels)));

        Assert.Equal(0, asked);
        Assert.Empty(_handle.Invocations["save"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not a layout")]
    public void PersistenceKey_ShouldKeepTheAppsLayout_WhenTheBrowserHoldsNoneOrSomethingElse(string? saved)
    {
        _handle.Setup<string?>("saved", _ => true).SetResult(saved);
        var changes = 0;

        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.PersistenceKey, "hike").Add(dock => dock.LayoutChanged, _ => changes++)
            .Add(dock => dock.ChildContent, Panels(HikePanels)));

        Assert.Equal(0, changes);
        Assert.Equal(3, cut.FindAll("[data-part=group]").Count);
        Assert.Empty(_handle.Invocations["save"]);
    }

    [Fact]
    public async Task PersistenceKey_ShouldSaveTheLayout_WhenTheUserOrTheAppChangesIt()
    {
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.PersistenceKey, "hike").Add(dock => dock.ChildContent, Panels(HikePanels)));

        await Tab(cut, "Map").ClickAsync();
        var reset = new DockLayout(new DockGroup("trails", "map", "notes", "weather"));
        cut.Render(parameters => parameters.Add(dock => dock.Layout, reset));

        Assert.Equal([$"hike: {Hike.Open("map").ToJson()}", $"hike: {reset.ToJson()}"],
            _handle.Invocations["save"].Select(call => $"{call.Arguments[0]}: {call.Arguments[1]}"));
    }

    [Fact]
    public async Task PersistenceKey_ShouldSaveEachChangeOnce_WhenTheLayoutIsBound()
    {
        DockLayout? bound = null;
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.PersistenceKey, "hike").Add(dock => dock.LayoutChanged, layout => bound = layout)
            .Add(dock => dock.ChildContent, Panels(HikePanels)));

        await Tab(cut, "Map").ClickAsync();
        // A binding hands the layout back to the dock as the app renders again.
        cut.Render(parameters => parameters.Add(dock => dock.Layout, bound));

        Assert.Single(_handle.Invocations["save"]);
    }

    [Fact]
    public async Task PersistenceKey_ShouldReadAndSaveUnderTheNewKey_WhenTheKeyArrivesOrChangesLater()
    {
        var saved = Hike.Close("weather");
        _handle.Setup<string?>("saved", call => Equals(call.Arguments[0], "trip")).SetResult(saved.ToJson());
        var changes = new List<DockLayout>();
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.LayoutChanged, changes.Add).Add(dock => dock.ChildContent, Panels(HikePanels)));

        // The key arrives after the dock attached, as one read from the user's profile does.
        cut.Render(parameters => parameters.Add(dock => dock.PersistenceKey, "trip"));
        Assert.Equal([saved], changes);
        await Tab(cut, "Map").ClickAsync();
        // Nothing is saved under the next key, so the layout stays and later changes go there.
        cut.Render(parameters => parameters.Add(dock => dock.PersistenceKey, "walk"));
        await Tab(cut, "Trails").ClickAsync();

        Assert.Equal(["trip", "walk"], _handle.Invocations["saved"].Select(call => call.Arguments[0]));
        Assert.Equal([$"trip: {saved.Open("map").ToJson()}", $"walk: {saved.Open("trails").ToJson()}"],
            _handle.Invocations["save"].Select(call => $"{call.Arguments[0]}: {call.Arguments[1]}"));
    }

    [Fact]
    public async Task PersistenceKey_ShouldSaveNothing_WhenNoKeyIsGiven()
    {
        var cut = RenderDock(Hike, HikePanels);

        await Tab(cut, "Map").ClickAsync();
        cut.Render(parameters => parameters.Add(dock => dock.Layout, new DockLayout(new DockGroup("trails"))));

        Assert.Empty(_handle.Invocations["saved"]);
        Assert.Empty(_handle.Invocations["save"]);
    }

    [Fact]
    public void Labels_ShouldWordTheMenusAndGrips_WhenGiven()
    {
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels
        {
            Menu = "Move {0}", TabMenu = "For this tab", MoveTabBack = "Earlier", MoveTabForward = "Later",
            MoveToGroup = "Other group",
            GroupItem = "No. {0} ({1})", NewGroup = "Split off", NewGroupAtEdge = "Dock edge", AtStart = "Start",
            AtEnd = "End", Above = "Over", Below = "Under", AtTop = "Top", AtBottom = "Bottom", Close = "Shut",
            SizeMenu = "Resize {0}…", Larger = "Grow {0}", Smaller = "Shrink {0}", Maximize = "Fill",
            Float = "Loose",
        }));

        var menu = cut.Find("[data-part=group-menu]");
        Assert.Equal("Move Trails", menu.QuerySelector("button")!.GetAttribute("aria-label"));
        Assert.Equal("For this tab", cut.Find(".tb-dock__tab-area > [role=menu]").GetAttribute("aria-label"));
        Assert.Equal(["Earlier", "Later", "Loose", "Fill", "Shut"],
            menu.QuerySelectorAll("[data-move]").Select(item => item.TextContent.Trim()));
        Assert.Equal(["No. 2 (Notes)", "No. 3 (Weather)"],
            menu.QuerySelectorAll("[data-move-group]").Select(item => item.TextContent.Trim()));
        Assert.Equal(["Start", "End", "Over", "Under", "Start", "End", "Top", "Bottom"],
            menu.QuerySelectorAll("[data-move-beside], [data-move-edge]").Select(item => item.TextContent.Trim()));
        Assert.Contains("Other group", menu.TextContent);
        Assert.Contains("Split off", menu.TextContent);
        Assert.Contains("Dock edge", menu.TextContent);
        var grip = cut.Find("[data-part=grip]");
        Assert.Equal("Resize Trails…", grip.QuerySelector("button")!.GetAttribute("aria-label"));
        Assert.Equal(["Grow Trails", "Shrink Trails"],
            grip.QuerySelectorAll("[role=menuitem]").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public async Task Maximize_ShouldFillTheDockWithTheGroupAndCoverTheOthers_WhenChosenInAGroupsMenu()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var (dock, notes) = (cut.Tb().Dock(), Reference(cut, "Notes"));

        await dock.MaximizeAsync("Notes");

        Assert.Equal(Hike.Maximize("notes"), changed);
        var groups = cut.FindAll("[data-part=group]");
        const string full = "--tb-dock-x: 0; --tb-dock-y: 0; --tb-dock-w: 100; --tb-dock-h: 100;";
        Assert.Equal(full, groups[1].GetAttribute("style"));
        Assert.Equal([false, true, false], groups.Select(group => group.HasAttribute("data-maximized")));
        Assert.Equal([true, false, true],
            groups.Select(group => group.ClassList.Contains("tb-dock__group--covered")));
        Assert.StartsWith(full, cut.Find("[role=tabpanel][data-panel=notes]").GetAttribute("style"));
        // The covered panels keep their content, out of reach, and no separator shows.
        Assert.Equal(["trails", "weather"],
            cut.FindAll("[role=tabpanel][inert]").Select(panel => panel.GetAttribute("data-panel")));
        Assert.DoesNotContain(_log, line => line.StartsWith("dispose", StringComparison.Ordinal));
        Assert.Empty(cut.FindAll(".tb-dock__separator, [data-part=grip]"));
        Assert.Equal(["Notes"], dock.Showing);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
        Assert.Equal(["Panel group 2 fills the dock."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(notes, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
        Assert.Equal(["maximize"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Maximize_ShouldOfferRestoreInPlaceOfMaximize_WhenAGroupFillsTheDock()
    {
        var cut = RenderDock(Hike, HikePanels);
        string?[] Moves(int group) => [.. cut.FindAll("[data-part=group-menu]")[group]
            .QuerySelectorAll("[data-move=maximize], [data-move=restore]")
            .Select(item => item.GetAttribute("data-move"))];
        Assert.Equal(["maximize"], Moves(1));
        Assert.Empty(cut.FindAll("[data-part=restore]"));

        await cut.Tb().Dock().MaximizeAsync("Notes");

        Assert.Equal(["restore"], Moves(1));
        Assert.Equal("Restore", cut.Find("[data-part=group][data-maximized] [data-part=restore]")
            .GetAttribute("aria-label"));
    }

    [Fact]
    public void Maximize_ShouldNotBeOffered_WhenTheDockHasOneGroup()
    {
        var cut = RenderDock(null, HikePanels);

        Assert.Empty(cut.FindAll("[data-move=maximize], [data-move=restore], [data-part=restore]"));
    }

    [Fact]
    public async Task Restore_ShouldShowEveryGroupWhereItWasAndFocusTheShowingTab_WhenTheButtonIsClicked()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var changes = new List<DockLayout>();
        var cut = RenderDock(Hike, HikePanels, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var (dock, notes) = (cut.Tb().Dock(), Reference(cut, "Notes"));
        var before = cut.FindAll("[data-part=group]").Select(group => group.GetAttribute("style")).ToList();
        await dock.MaximizeAsync("Notes");

        await dock.RestoreAsync();

        Assert.Equal(Hike, changes[^1]);
        Assert.Equal(before, cut.FindAll("[data-part=group]").Select(group => group.GetAttribute("style")));
        Assert.Empty(cut.FindAll(".tb-dock__group--covered, [data-maximized], [data-part=restore]"));
        Assert.Equal(2, cut.FindAll(".tb-dock__separator").Count);
        Assert.Equal(["Trails", "Notes", "Weather"], dock.Showing);
        Assert.Equal(["Panel group 2 fills the dock.", "All panel groups show again."],
            Services.GetRequiredService<FakeAnnouncer>().Polite);
        // The button goes with the click, so the focus it held goes to the group's showing tab.
        Assert.Equal(notes, ((ElementReference)JSInterop.Invocations.Last(
            call => call.Identifier.EndsWith("focus", StringComparison.Ordinal)).Arguments[0]!).Id);
        Assert.Equal(["maximize", "restore"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Restore_ShouldFocusTheGroupsShowingTab_WhenChosenInTheMenuOfATabThatIsNotShowing()
    {
        var cut = RenderDock(Hike, HikePanels);
        var (dock, trails) = (cut.Tb().Dock(), Reference(cut, "Trails"));
        await dock.MaximizeAsync("Map");
        await dock.SelectAsync("Trails");

        // The map's tab isn't showing now, and Restore doesn't show it, so the row's one tab stop takes the focus.
        await dock.ChooseFromTabMenuAsync("Map", "Restore");

        Assert.Equal(["Trails", "Notes", "Weather"], dock.Showing);
        Assert.Equal(trails, ((ElementReference)JSInterop.Invocations.Last(
            call => call.Identifier.EndsWith("focus", StringComparison.Ordinal)).Arguments[0]!).Id);
    }

    [Fact]
    public void Render_ShouldLeaveOutTheRestoreButton_WhenThePageIsStaticAndAGroupFillsTheDock()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderDock(Hike.Maximize("notes"), HikePanels);

        // The group fills the dock as the layout says, and the button that restores comes with interactivity.
        Assert.Single(cut.FindAll("[data-part=group][data-maximized]"));
        Assert.Equal(2, cut.FindAll(".tb-dock__group--covered").Count);
        Assert.Empty(cut.FindAll("[data-part=restore]"));
    }

    [Fact]
    public async Task Maximize_ShouldShowTheTabAndFillTheDockThenRestore_WhenTheScriptReportsDoubleClicks()
    {
        var changes = new List<DockLayout>();
        var cut = RenderDock(Hike, HikePanels, changes.Add);

        // The map isn't showing, and a double click on its tab shows it as its group fills the dock.
        await cut.InvokeAsync(() => Report().OnMaximize("map"));

        Assert.Equal(Hike.Maximize("map"), changes[^1]);
        Assert.Equal(["Map"], cut.Tb().Dock().Showing);

        // A double click on any tab of the group that fills the dock shows every group again.
        await cut.InvokeAsync(() => Report().OnMaximize("trails"));

        Assert.Equal(Hike.Open("map"), changes[^1]);
        Assert.Equal(["Map", "Notes", "Weather"], cut.Tb().Dock().Showing);
    }

    [Fact]
    public async Task Maximize_ShouldChangeNothingButCountTheReport_WhenADoubleClickCanDoNothing()
    {
        var changes = 0;
        var cut = RenderDock(null, HikePanels, _ => changes++);
        var commits = cut.Find("[data-part=dock]").GetAttribute("data-commits");

        // A dock with one group has nothing to cover, and the dock has no panel by the other name.
        await cut.InvokeAsync(() => Report().OnMaximize("map"));
        await cut.InvokeAsync(() => Report().OnMaximize("photos"));

        Assert.Equal(0, changes);
        Assert.Empty(cut.FindAll("[data-maximized]"));
        Assert.Equal((int.Parse(commits!) + 2).ToString(), cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task Maximize_ShouldDropACoveredPanelsContent_WhenItsKeepAliveIsOff()
    {
        var cut = RenderDock(Hike, [new("trails", "Trails", KeepAlive: false), .. HikePanels.Skip(1)]);

        await cut.Tb().Dock().MaximizeAsync("Notes");

        Assert.Contains("dispose trails", _log);
        Assert.Empty(cut.FindAll("[role=tabpanel][data-panel=trails]"));

        await cut.Tb().Dock().RestoreAsync();

        Assert.Equal(2, _log.Count(line => line == "init trails"));
    }

    [Fact]
    public async Task Maximize_ShouldEnd_WhenThePanelMovesToAnotherGroupOrTheGroupGoes()
    {
        var cut = RenderDock(Hike, HikePanels);
        var dock = cut.Tb().Dock();

        // The other groups are still in the menu, and a move to one shows where the panel went.
        await dock.MaximizeAsync("Trails");
        await dock.MoveToGroupAsync("Trails", 3);

        Assert.Empty(cut.FindAll(".tb-dock__group--covered, [data-maximized]"));
        Assert.Equal(["Map", "Notes", "Trails"], dock.Showing);

        // The notes are alone in their group, which goes when they close.
        await dock.MaximizeAsync("Notes");
        await dock.CloseAsync("Notes");

        Assert.Empty(cut.FindAll(".tb-dock__group--covered, [data-maximized]"));
        Assert.Equal(["Map", "Trails"], dock.Showing);
    }

    [Fact]
    public async Task Driver_ShouldSayWhyItCannotClick_WhenThePanelIsBehindTheGroupThatFillsTheDock()
    {
        var cut = RenderDock(Hike, HikePanels);
        var dock = cut.Tb().Dock();
        Assert.Equal("No group fills the dock.",
            (await Assert.ThrowsAsync<InvalidOperationException>(dock.RestoreAsync)).Message);
        await dock.MaximizeAsync("Notes");

        const string behind = "The panel \"Trails\" is behind the group that fills the dock; restore the groups first.";
        Assert.Equal(behind, (await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.SelectAsync("Trails"))).Message);
        Assert.Equal(behind, (await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.CloseAsync("Trails"))).Message);
        Assert.Equal(behind, (await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.MaximizeAsync("Trails"))).Message);
        Assert.Equal(behind, (await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.ChooseFromTabMenuAsync("Trails", "Close"))).Message);
        Assert.Equal("The group of \"Notes\" is the dock's only one, or fills the dock already.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MaximizeAsync("Notes"))).Message);
    }

    [Fact]
    public async Task Labels_ShouldWordMaximizingAndRestoring_WhenGiven()
    {
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels
        {
            Maximize = "Fill", Restore = "Back", Maximized = "Group {0} is alone", Restored = "All back",
        }));
        Assert.Equal("Fill", cut.Find("[data-part=group-menu] [data-move=maximize]").TextContent.Trim());

        await cut.Tb().Dock().MaximizeAsync("Notes");

        Assert.Equal("Back", cut.Find("[data-part=group-menu] [data-move=restore]").TextContent.Trim());
        Assert.Equal("Back", cut.Find("[data-part=restore]").GetAttribute("aria-label"));

        await cut.Tb().Dock().RestoreAsync();

        Assert.Equal(["Group 2 is alone", "All back"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Float_ShouldDrawThePanelInAWindowAboveTheGroups_WhenChosenInAGroupsMenu()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        DockLayout? changed = null;
        var cut = RenderDock(Hike, HikePanels, layout => changed = layout);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var (dock, notes) = (cut.Tb().Dock(), ReferenceOnceMade(cut, "Notes"));

        await dock.FloatAsync("Notes");

        Assert.Equal(Hike.Float("notes"), changed);
        // The window is numbered after the docked groups, and drawn half as large as the dock over its middle.
        var window = cut.Find("[data-part=group][data-floating]");
        const string box = "--tb-dock-x: 25; --tb-dock-y: 25; --tb-dock-w: 50; --tb-dock-h: 50; --tb-dock-z: 0;";
        Assert.Equal(("2", box, "Floating panel group 3"), (window.GetAttribute("data-group"),
            window.GetAttribute("style"), window.QuerySelector("[role=tablist]")!.GetAttribute("aria-label")));
        Assert.Contains("tb-dock__group--floating", window.ClassList);
        var panel = cut.Find("[role=tabpanel][data-panel=notes]");
        Assert.StartsWith(box, panel.GetAttribute("style"));
        Assert.Equal(("2", true), (panel.GetAttribute("data-dock-group"), panel.HasAttribute("data-floating")));
        Assert.Contains("tb-dock__panel--floating", panel.ClassList);
        Assert.Equal(["Notes"], dock.Floating);
        Assert.Equal(["Trails", "Map", "Weather", "Notes"], dock.Panels);
        // The panel kept its content, and its tab, made anew in the window's row, takes the focus.
        Assert.DoesNotContain(_log, line => line.StartsWith("dispose", StringComparison.Ordinal));
        Assert.Equal(["Notes floats in a window."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(notes(), ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
        Assert.Equal(["float"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Float_ShouldOfferAWindowWhatItCanDo_WhenItsMenuIsRendered()
    {
        var cut = RenderDock(Hike, HikePanels);
        await cut.Tb().Dock().FloatAsync("Notes");
        await cut.Tb().Dock().FloatAsync("Weather");

        var menu = cut.Find("[data-part=group][data-floating] [data-part=group-menu]");

        // A window docks, moves to a group, another window or an edge, and closes; it has no group to split or to
        // fill the dock.
        Assert.Equal(["back", "forward", "dock", "close"],
            menu.QuerySelectorAll("[data-move]").Select(item => item.GetAttribute("data-move")));
        Assert.Equal(["Group 1: Trails", "Group 3: Weather"],
            menu.QuerySelectorAll("[data-move-group]").Select(item => item.TextContent.Trim()));
        Assert.Equal(4, menu.QuerySelectorAll("[data-move-edge]").Length);
        Assert.Empty(menu.QuerySelectorAll("[data-move-beside]"));
        // A docked group can move a panel into a window, by its number.
        Assert.Equal(["Group 2: Notes", "Group 3: Weather"], cut.FindAll("[data-part=group-menu]")[0]
            .QuerySelectorAll("[data-move-group]").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public async Task Float_ShouldGiveAPanelAWindowOfItsOwn_WhenChosenInAWindowItShares()
    {
        var cut = RenderDock(Hike.Float("notes").Move("weather", "notes", DockPosition.After), HikePanels);
        var dock = cut.Tb().Dock();
        string[] Moves() => [.. cut.Find("[data-part=group][data-floating] [data-part=group-menu]")
            .QuerySelectorAll("[data-move]").Select(item => item.GetAttribute("data-move")!)];
        Assert.Equal(["back", "forward", "float", "dock", "close"], Moves());

        await dock.FloatAsync("Weather");

        Assert.Equal(2, cut.FindAll("[data-part=group][data-floating]").Count);
        Assert.Equal(["Notes", "Weather"], dock.Floating);
        Assert.Equal("Weather floats in a window.", Services.GetRequiredService<FakeAnnouncer>().Polite[^1]);
        // Alone in its window, a panel has a window to itself already.
        Assert.Equal(["back", "forward", "dock", "close"], Moves());
    }

    [Fact]
    public async Task Dock_ShouldPutAFloatingPanelInTheLargestGroup_WhenDockIsChosenInItsWindowsMenu()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var dock = cut.Tb().Dock();
        await dock.FloatAsync("Map");

        await dock.DockAsync("Map");

        // The notes' group is the largest docked one.
        Assert.Empty(dock.Floating);
        Assert.Empty(cut.FindAll("[data-floating]"));
        Assert.Equal(["Trails", "Notes", "Map", "Weather"], dock.Panels);
        Assert.Equal(["Trails", "Map", "Weather"], dock.Showing);
        Assert.Equal("Map moved to panel group 2.", Services.GetRequiredService<FakeAnnouncer>().Polite[^1]);
        Assert.Equal(["select", "float", "dock"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task Float_ShouldTakeAPanelIntoAWindow_WhenMoveToGroupNamesTheWindow()
    {
        var cut = RenderDock(Hike, HikePanels);
        var dock = cut.Tb().Dock();
        await dock.FloatAsync("Notes");

        await dock.MoveToGroupAsync("Weather", 3);

        Assert.Equal(["Notes", "Weather"], dock.Floating);
        Assert.Equal("Weather floats in a window.", Services.GetRequiredService<FakeAnnouncer>().Polite[^1]);
        // A window with two tabs still has no group of its own to split.
        Assert.Empty(cut.FindAll("[data-floating] [data-move-beside]"));
    }

    [Fact]
    public async Task Float_ShouldOfferTheDocksEdges_WhenAWindowWithOneTabIsAllTheDockHas()
    {
        var cut = RenderDock(new DockLayout(null, null, [new DockFloat(new DockGroup("notes"), 10, 10, 50, 50)]),
            new Panel("notes", "Notes"));
        var dock = cut.Tb().Dock();
        Assert.Equal(4, cut.FindAll("[data-floating] [data-move-edge]").Count);

        await dock.MoveToEdgeAsync("Notes", DockPosition.Start);

        Assert.Empty(dock.Floating);
        Assert.Equal(["Notes"], dock.Showing);
    }

    [Fact]
    public async Task Menu_ShouldLeaveOutTheDocksEdges_WhenTheOnlyDockedPanelHasOnlyWindowsBesideIt()
    {
        var cut = RenderDock(
            new DockLayout(new DockGroup("trails"), null, [new DockFloat(new DockGroup("notes"), 10, 10, 50, 50)]),
            new Panel("trails", "Trails"), new Panel("notes", "Notes"));
        var docked = cut.Find("[data-part=group]:not([data-floating]) [data-part=group-menu]");

        // An edge would give the trails the group they have, though they can still join the window or float.
        Assert.Empty(docked.QuerySelectorAll("[data-move-edge]"));
        Assert.Equal(["Group 2: Notes"],
            docked.QuerySelectorAll("[data-move-group]").Select(item => item.TextContent.Trim()));
        Assert.Equal("The panel \"Trails\" is the only one docked, so it has nowhere to move.",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => cut.Tb().Dock().MoveToEdgeAsync("Trails", DockPosition.Start))).Message);
    }

    [Fact]
    public async Task Driver_ShouldListTheDockedGroupsBeforeTheWindows_WhenAWindowIsOlderThanAGroup()
    {
        var cut = RenderDock(Hike, HikePanels);
        var dock = cut.Tb().Dock();
        await dock.FloatAsync("Notes");

        // The map's new group is rendered after the window's row, and still comes before it.
        await dock.MoveToNewGroupAsync("Map", DockPosition.Bottom);

        Assert.Equal(["Trails", "Map", "Weather", "Notes"], dock.Panels);
        Assert.Equal(["0", "1", "3", "2"],
            cut.FindAll("[data-part=group]").Select(group => group.GetAttribute("data-group")));
    }

    [Fact]
    public async Task Raise_ShouldDrawAWindowInFrontAndKeepItsNumber_WhenTheScriptReportsAPressInIt()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var start = Hike.Float("map").Float("weather");
        var changes = new List<DockLayout>();
        var cut = RenderDock(start, HikePanels, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        string[] Layers() => [.. cut.FindAll("[data-part=group][data-floating]").Select(window =>
            $"{window.QuerySelector("[role=tablist]")!.GetAttribute("aria-label")}: "
                + window.GetAttribute("style")![^15..])];
        Assert.Equal(["Floating panel group 3: --tb-dock-z: 0;", "Floating panel group 4: --tb-dock-z: 1;"], Layers());
        var commits = cut.Find("[data-part=dock]").GetAttribute("data-commits");

        await cut.InvokeAsync(() => Report().OnRaise("map"));

        // The windows keep their numbers and trade layers.
        Assert.Equal(["Floating panel group 3: --tb-dock-z: 1;", "Floating panel group 4: --tb-dock-z: 0;"], Layers());
        Assert.Equal([start.Raise("map")], changes);
        // Coming forward isn't counted as use, or as an action a test would wait for.
        Assert.Empty(used.GetMeasurementSnapshot());
        Assert.Equal(commits, cut.Find("[data-part=dock]").GetAttribute("data-commits"));

        // The window in front has nowhere to come.
        await cut.InvokeAsync(() => Report().OnRaise("map"));
        Assert.Single(changes);
    }

    [Fact]
    public async Task Close_ShouldFocusAGroupsShowingTab_WhenAWindowGoesWithItsOnlyTab()
    {
        var cut = RenderDock(Hike.Float("map"), HikePanels);
        var weather = Reference(cut, "Weather");

        await cut.InvokeAsync(() => Report().OnClose("map", true));

        Assert.Empty(cut.FindAll("[data-floating]"));
        Assert.Equal(weather, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Fact]
    public async Task Close_ShouldFocusTheNextWindowsTab_WhenAWindowGoesBeforeAnotherWindow()
    {
        var cut = RenderDock(Hike.Float("map").Float("weather"), HikePanels);
        var weather = Reference(cut, "Weather");

        await cut.InvokeAsync(() => Report().OnClose("map", true));

        Assert.Single(cut.FindAll("[data-part=group][data-floating]"));
        Assert.Equal(weather, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Fact]
    public async Task Close_ShouldFocusTheDock_WhenTheLastTabThatShowsClosesBesideAStrip()
    {
        var cut = RenderDock(new DockLayout(new DockGroup("trails"), autoHidden: [new("notes", DockPosition.Start)]),
            HikePanels[0], HikePanels[2]);
        var button = cut.Find("[data-part=strip-button]").GetAttribute("blazor:elementreference");

        await cut.InvokeAsync(() => Report().OnClose("trails", true));

        // The notes stay on their strip, and the dock's area, empty now, takes the focus, not the strip's button.
        Assert.Equal(["Notes"], cut.Tb().Dock().AutoHidden);
        Assert.Equal("-1", cut.Find("[data-part=dock]").GetAttribute("tabindex"));
        Assert.NotEqual(button, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Fact]
    public async Task Close_ShouldFocusTheGroupThatFillsTheDock_WhenAWindowGoesBesideAGroupItCovers()
    {
        // The trails' group fills the dock and covers the notes, the last docked group, which the window follows.
        var cut = RenderDock(Hike.Float("weather").Maximize("trails"), HikePanels);
        var trails = Reference(cut, "Trails");

        await cut.InvokeAsync(() => Report().OnClose("weather", true));

        Assert.Empty(cut.FindAll("[data-floating]"));
        Assert.Contains("tb-dock__group--covered", cut.FindAll("[data-part=group]")[1].ClassList);
        Assert.Equal(trails, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Fact]
    public async Task Maximize_ShouldLeaveTheWindowsAboveTheGroupThatFillsTheDock_WhenAGroupIsMaximized()
    {
        var cut = RenderDock(Hike.Float("map"), HikePanels);
        var dock = cut.Tb().Dock();

        await dock.MaximizeAsync("Notes");

        Assert.Equal(["Notes", "Map"], dock.Showing);
        Assert.DoesNotContain("tb-dock__group--covered", cut.Find("[data-part=group][data-floating]").ClassList);
        // A window isn't a group to maximize, from its menu or with a double click.
        Assert.Empty(cut.FindAll("[data-floating] [data-move=maximize], [data-floating] [data-move=restore]"));
        await cut.InvokeAsync(() => Report().OnMaximize("map"));
        Assert.Equal(["Notes", "Map"], dock.Showing);
    }

    [Fact]
    public void Render_ShouldDrawTheWindowsOfItsLayout_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderDock(Hike.FloatAt("map", 10, 20, 30, 40), HikePanels);

        Assert.Equal("--tb-dock-x: 10; --tb-dock-y: 20; --tb-dock-w: 30; --tb-dock-h: 40; --tb-dock-z: 0;",
            cut.Find("[data-part=group][data-floating]").GetAttribute("style"));
        Assert.Equal(["Map"], cut.Tb().Dock().Floating);
    }

    [Fact]
    public async Task Driver_ShouldSayWhy_WhenAPanelFloatsOrIsDockedAlready()
    {
        var cut = RenderDock(Hike, HikePanels);
        var dock = cut.Tb().Dock();
        Assert.Equal("The panel \"Notes\" is docked already.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.DockAsync("Notes"))).Message);

        await dock.FloatAsync("Notes");

        Assert.Equal("The panel \"Notes\" floats already.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.FloatAsync("Notes"))).Message);
    }

    [Fact]
    public async Task Labels_ShouldWordFloatingAndDocking_WhenGiven()
    {
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels
        {
            Float = "Loose", Dock = "Pin", Floated = "{0} is loose", FloatingGroup = "Window {0}",
        }));
        Assert.Equal("Loose", cut.Find("[data-part=group-menu] [data-move=float]").TextContent.Trim());

        await cut.Tb().Dock().FloatAsync("Notes");

        var window = cut.Find("[data-part=group][data-floating]");
        Assert.Equal("Window 3", window.QuerySelector("[role=tablist]")!.GetAttribute("aria-label"));
        Assert.Equal("Pin", window.QuerySelector("[data-move=dock]")!.TextContent.Trim());
        Assert.Equal(["Notes is loose"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Window_ShouldTakeTheBoxTheScriptReports_WhenItIsMovedOrResized()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var changes = new List<DockLayout>();
        var cut = RenderDock(Hike.Float("notes"), HikePanels, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var dock = cut.Find("[data-part=dock]");
        var commits = int.Parse(dock.GetAttribute("data-commits")!);

        await cut.InvokeAsync(() => Report().OnFloat("notes", 10, 20, 50, 50, false, 1));

        const string moved = "--tb-dock-x: 10; --tb-dock-y: 20; --tb-dock-w: 50; --tb-dock-h: 50; --tb-dock-z: 0;";
        Assert.Equal(Hike.FloatAt("notes", 10, 20, 50, 50), changes[^1]);
        Assert.Equal(moved, cut.Find("[data-part=group][data-floating]").GetAttribute("style"));
        Assert.StartsWith(moved, cut.Find("[role=tabpanel][data-panel=notes]").GetAttribute("style"));
        Assert.Equal(moved, cut.Find("[data-part=window-grip]").GetAttribute("style"));
        // The report's number lets the script drop the box it showed meanwhile.
        Assert.Equal(("1", (commits + 1).ToString()),
            (cut.Find("[data-part=dock]").GetAttribute("data-report"),
                cut.Find("[data-part=dock]").GetAttribute("data-commits")));

        await cut.InvokeAsync(() => Report().OnFloat("notes", 10, 20, 30, 40, true, 2));

        Assert.Equal(Hike.FloatAt("notes", 10, 20, 30, 40), changes[^1]);
        Assert.Equal(["move_window", "resize_window"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
        Assert.Equal(["The window is 10% across and 20% down.", "The window is 30% wide and 40% tall."],
            Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Window_ShouldKeepTheBoxItsStyleShows_WhenADragReportsMoreDecimalsThanItShows()
    {
        var changes = new List<DockLayout>();
        var cut = RenderDock(Hike.Float("notes"), HikePanels, changes.Add);

        // A drag reports every decimal. A step against the dock's edge then reports the box the style shows.
        await cut.InvokeAsync(() => Report().OnFloat("notes", 13.528912, 0, 50, 50, false, 1));
        await cut.InvokeAsync(() => Report().OnFloat("notes", 13.5289, 0, 50, 50, false, 2));

        Assert.Equal([Hike.FloatAt("notes", 13.5289, 0, 50, 50)], changes);
    }

    [Fact]
    public async Task Window_ShouldChangeNothingButCountTheReport_WhenTheScriptNamesNoWindowOrTheBoxItHas()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var changes = 0;
        var cut = RenderDock(Hike.Float("notes"), HikePanels, _ => changes++);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var commits = int.Parse(cut.Find("[data-part=dock]").GetAttribute("data-commits")!);

        // The trails are docked, and a step against the dock's edge reports the box the window has already.
        await cut.InvokeAsync(() => Report().OnFloat("trails", 10, 20, 50, 50, false, 1));
        await cut.InvokeAsync(() => Report().OnFloat("notes", 25, 25, 50, 50, false, 2));

        Assert.Equal(0, changes);
        Assert.Empty(used.GetMeasurementSnapshot());
        Assert.Empty(Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal((commits + 2).ToString(), cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task Commits_ShouldCountAnActionOnlyOnceItsFocusHasMoved_WhenTheActionMovesTheFocus()
    {
        // The browser hasn't yet answered the dock's call to focus the moved tab.
        var focus = JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        var cut = RenderDock(Hike, HikePanels);

        await cut.Tb().Dock().MoveToGroupAsync("Trails", 2);

        // The move shows, but the count waits for the focus, since keys pressed now would go astray.
        Assert.Equal(["Map", "Notes", "Trails", "Weather"], cut.Tb().Dock().Panels);
        Assert.Equal("0", cut.Find("[data-part=dock]").GetAttribute("data-commits"));

        focus.SetVoidResult();

        cut.WaitForAssertion(() => Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits")));
    }

    [Fact]
    public async Task Window_ShouldHaveAMoveButtonAndAGripThatAskTheScriptForSteps_WhenRendered()
    {
        var cut = RenderDock(Hike.Float("notes"), HikePanels);
        var (move, grip) = (cut.Find("[data-floating] > [data-part=window-move]"),
            cut.Find("[data-part=dock] > [data-part=window-grip]"));

        Assert.Equal(("Move the Notes window", "Resize the Notes window"),
            (move.QuerySelector("button")!.GetAttribute("aria-label"),
                grip.QuerySelector("button")!.GetAttribute("aria-label")));
        Assert.Contains("Arrow keys move the window.", move.TextContent);
        Assert.Contains("Arrow keys resize the window.", grip.TextContent);
        Assert.Equal(["Left", "Right", "Up", "Down"],
            move.QuerySelectorAll("[data-step]").Select(item => item.TextContent.Trim()));
        Assert.Equal(["Wider", "Narrower", "Taller", "Shorter"],
            grip.QuerySelectorAll("[data-step]").Select(item => item.TextContent.Trim()));
        // The grip is the window's last part, with the window's number, layer and box.
        const string box = "--tb-dock-x: 25; --tb-dock-y: 25; --tb-dock-w: 50; --tb-dock-h: 50; --tb-dock-z: 0;";
        Assert.Equal(("2", true, box),
            (grip.GetAttribute("data-group"), grip.HasAttribute("data-floating"), grip.GetAttribute("style")));
        // A docked group has neither.
        Assert.Equal(2, cut.FindAll("[data-part=window-move], [data-part=window-grip]").Count);

        await move.QuerySelector("[data-step=left]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
        await grip.QuerySelector("[data-step=taller]")!.ClickAsync(new MouseEventArgs { Detail = 1 });

        // The script makes the step, so that the window stays inside the dock.
        Assert.Equal(["2 left", "2 taller"],
            _handle.Invocations["stepWindow"].Select(call => $"{call.Arguments[0]} {call.Arguments[1]}"));
    }

    [Fact]
    public void Render_ShouldLeaveOutAWindowsMoveButtonAndGrip_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderDock(Hike.Float("notes"), HikePanels);

        Assert.Single(cut.FindAll("[data-part=group][data-floating]"));
        Assert.Empty(cut.FindAll("[data-part=window-move], [data-part=window-grip]"));
    }

    [Fact]
    public async Task Driver_ShouldMoveAndResizeAWindowInsideTheDock_WhenGivenPercents()
    {
        var cut = RenderDock(Hike.Float("notes"), HikePanels);
        var dock = cut.Tb().Dock();
        Assert.Equal((25, 25, 50, 50), dock.Window("Notes"));

        await dock.MoveWindowAsync("Notes", 10, -5);
        Assert.Equal((35, 20, 50, 50), dock.Window("Notes"));

        await dock.ResizeWindowAsync("Notes", -20, 10);
        Assert.Equal((35, 20, 30, 60), dock.Window("Notes"));

        // The window stops at the dock's edges, and keeps a tenth of the dock's width.
        await dock.MoveWindowAsync("Notes", 90, 90);
        Assert.Equal("The window is 70% across and 40% down.", Services.GetRequiredService<FakeAnnouncer>().Polite[^1]);
        await dock.ResizeWindowAsync("Notes", -90, 90);
        Assert.Equal((70, 40, 10, 60), dock.Window("Notes"));
        Assert.Equal("The panel \"Trails\" isn't in a floating window.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MoveWindowAsync("Trails", 1, 1))).Message);
        Assert.Equal("The panel \"Trails\" isn't in a floating window.",
            Assert.Throws<InvalidOperationException>(() => dock.Window("Trails")).Message);
    }

    [Fact]
    public void Labels_ShouldWordAWindowsMoveButtonAndGrip_WhenGiven()
    {
        var cut = RenderDock(Hike.Float("notes"), HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels
        {
            MoveWindow = "Shift {0}", MoveWindowHint = "Arrows shift it", ResizeWindow = "Size {0}",
            ResizeWindowHint = "Arrows size it", Left = "West", Right = "East", Up = "North", Down = "South",
            Wider = "Broader", Narrower = "Slimmer", Taller = "Higher", Shorter = "Lower",
        }));

        var (move, grip) = (cut.Find("[data-part=window-move]"), cut.Find("[data-part=window-grip]"));
        Assert.Equal(("Shift Notes", "Size Notes"), (move.QuerySelector("button")!.GetAttribute("aria-label"),
            grip.QuerySelector("button")!.GetAttribute("aria-label")));
        Assert.Contains("Arrows shift it", move.TextContent);
        Assert.Contains("Arrows size it", grip.TextContent);
        Assert.Equal(["West", "East", "North", "South", "Broader", "Slimmer", "Higher", "Lower"],
            cut.FindAll("[data-step]:not([data-step=larger]):not([data-step=smaller])")
                .Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public async Task Labels_ShouldWordWhatIsSaidAfterAWindowMovesOrResizes_WhenGiven()
    {
        var cut = RenderDock(Hike.Float("notes"), HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels
        {
            MovedWindow = "At {0} by {1}", ResizedWindow = "Now {0} by {1}",
        }));

        await cut.InvokeAsync(() => Report().OnFloat("notes", 10.4, 20.6, 50, 50, false, 1));
        await cut.InvokeAsync(() => Report().OnFloat("notes", 10.4, 20.6, 30, 40, true, 2));

        // Whole percents, as for a separator.
        Assert.Equal(["At 10 by 21", "Now 30 by 40"], Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Labels_ShouldWordWhatIsSaidAfterAMove_WhenGiven()
    {
        var cut = RenderDock(Hike, HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels { Moved = "{0} is in group {1}" }));

        await cut.Find("[data-part=group-menu] [data-move-group='2']").ClickAsync();

        Assert.Equal(["Trails is in group 2"], Services.GetRequiredService<FakeAnnouncer>().Polite);
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
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dock.ResizeAsync("Resize Trails", 0));
        // A separator between groups reaches 1% and 99%; a slid-out panel's edge stops sooner.
        await dock.ResizeAsync("Resize Trails", 1);
        Assert.Equal(1, dock.Size("Resize Trails"));
        await dock.ResizeAsync("Resize Trails", 99);
        Assert.Equal(99, dock.Size("Resize Trails"));
        Assert.Equal("No separator is named \"Resize Map\".",
            Assert.Throws<InvalidOperationException>(() => dock.Size("Resize Map")).Message);
    }

    [Fact]
    public async Task Driver_ShouldMoveAPanelAsItsGroupsMenuDoes_WhenAMoveHelperIsUsed()
    {
        var changes = new List<DockLayout>();
        var cut = RenderDock(Hike, HikePanels, changes.Add);
        var dock = cut.Tb().Dock();

        // The map isn't showing, so the helper selects it before it moves it.
        await dock.MoveTabAsync("Map", -1);
        Assert.Equal(["Map", "Trails", "Notes", "Weather"], dock.Panels);
        await dock.MoveTabAsync("Map", 1);
        await dock.MoveTabAsync("Map", 0);
        await dock.MoveToGroupAsync("Map", 3);
        Assert.Equal(["Trails", "Notes", "Weather", "Map"], dock.Panels);
        Assert.Equal(["Trails", "Notes", "Map"], dock.Showing);
        await dock.MoveToNewGroupAsync("Weather", DockPosition.Top);
        await dock.MoveToEdgeAsync("Trails", DockPosition.End);

        Assert.Equal(["Notes", "Weather", "Map", "Trails"], dock.Panels);
        Assert.Equal(["Notes", "Weather", "Map", "Trails"], dock.Showing);
        Assert.Equal(changes[^2].MoveToEdge("trails", DockPosition.End), changes[^1]);
        Assert.Equal(changes[^3].Move("weather", "map", DockPosition.Top), changes[^2]);
        // The map and the weather were each selected first, and the move by no places changed nothing.
        Assert.Equal(7, changes.Count);
    }

    [Fact]
    public async Task Driver_ShouldSayWhyAPanelCantMove_WhenItsMenuHasNoSuchItem()
    {
        var dock = RenderDock(Hike, HikePanels).Tb().Dock();

        Assert.Equal("The tab of \"Trails\" is at the end of its row.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MoveTabAsync("Trails", 2))).Message);
        Assert.Equal("The tab of \"Map\" is at the start of its row.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MoveTabAsync("Map", -2))).Message);
        Assert.Equal("The dock has no other group 1 to move \"Trails\" to.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MoveToGroupAsync("Trails", 1))).Message);
        Assert.Equal("The dock has no other group 4 to move \"Trails\" to.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.MoveToGroupAsync("Trails", 4))).Message);
        Assert.Equal("The panel \"Notes\" is alone in its group, so it has no group to split.",
            (await Assert.ThrowsAsync<InvalidOperationException>(
                () => dock.MoveToNewGroupAsync("Notes", DockPosition.Start))).Message);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => dock.MoveToNewGroupAsync("Trails", DockPosition.Before));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dock.MoveToEdgeAsync("Trails", DockPosition.After));
        Assert.Equal("The panel \"Map\" is the only one docked, so it has nowhere to move.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => RenderDock(null, new Panel("map", "Map")).Tb()
                .Dock().MoveToEdgeAsync("Map", DockPosition.Top))).Message);
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
    public async Task Driver_ShouldRestoreOnlyItsOwnDock_WhenAPanelHoldsADockWhoseGroupFillsIt()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<TbDockManager>(0);
            builder.AddAttribute(1, nameof(TbDockManager.AriaLabel), "Hike planner");
            builder.AddAttribute(2, nameof(TbDockManager.Layout),
                new DockLayout(new DockSplit(new DockGroup("trails"), new DockGroup("map"))));
            builder.AddAttribute(3, nameof(TbDockManager.ChildContent), (RenderFragment)(panels =>
            {
                panels.OpenComponent<TbDockPanel>(0);
                panels.AddAttribute(1, nameof(TbDockPanel.Id), "trails");
                panels.AddAttribute(2, nameof(TbDockPanel.Title), "Trails");
                panels.AddAttribute(3, nameof(TbDockPanel.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenComponent<TbDockManager>(0);
                    content.AddAttribute(1, nameof(TbDockManager.AriaLabel), "Gear planner");
                    content.AddAttribute(2, nameof(TbDockManager.Layout), new DockLayout(
                        new DockSplit(new DockGroup("tents") { Maximized = true }, new DockGroup("stoves"))));
                    content.AddAttribute(3, nameof(TbDockManager.ChildContent),
                        Panels(new Panel("tents", "Tents"), new Panel("stoves", "Stoves")));
                    content.CloseComponent();
                }));
                panels.CloseComponent();
                panels.OpenComponent<TbDockPanel>(4);
                panels.AddAttribute(5, nameof(TbDockPanel.Id), "map");
                panels.AddAttribute(6, nameof(TbDockPanel.Title), "Map");
                panels.CloseComponent();
            }));
            builder.CloseComponent();
        });
        var (outer, inner) = (cut.Tb().Dock("Hike planner"), cut.Tb().Dock("Gear planner"));

        // Only the dock inside the trails' panel has a group that fills it.
        Assert.Equal("No group fills the dock.",
            (await Assert.ThrowsAsync<InvalidOperationException>(outer.RestoreAsync)).Message);
        Assert.Equal(["Tents"], inner.Showing);

        await inner.RestoreAsync();

        Assert.Equal(["Tents", "Stoves"], inner.Showing);
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

    private sealed record Panel(string Id, string Title, bool Closable = true, bool KeepAlive = true,
        bool AllowPopOut = true, TimeSpan? KeepWindowFor = null, bool Modified = false);

    [Fact]
    public async Task AutoHide_ShouldPutThePanelOnAStripWithTheFocusOnItsButton_WhenChosenInAGroupsMenu()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var changes = new List<DockLayout>();
        var cut = RenderDock(Hike, HikePanels, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var button = OnceMade(cut, "[data-part=strip-button][data-panel=trails]");

        // The first group's menu acts on its showing panel, the trails.
        await cut.Find("[data-part=group-menu] [data-move-hide=start]").ClickAsync();

        Assert.Equal([Hike.AutoHide("trails", DockPosition.Start)], changes);
        var strip = cut.Find("[data-part=strip]");
        Assert.Equal(("group", "start", "Auto-hidden panels (At the start)", "tb-dock__strip tb-dock__strip--start"),
            (strip.GetAttribute("role"), strip.GetAttribute("data-edge"), strip.GetAttribute("aria-label"),
                strip.ClassName));
        // The strip comes before the dock's area in the page, as it does on the screen.
        Assert.Equal("strip", cut.Find("[data-part=frame]").FirstElementChild!.GetAttribute("data-part"));
        var made = cut.Find("[data-part=strip-button]");
        Assert.Equal(("Trails", "false"), (made.TextContent.Trim(), made.GetAttribute("aria-expanded")));
        // The panel keeps its content where it is, hidden and out of reach, and has no tab.
        var panel = cut.Find("[data-part=panel][data-panel=trails]");
        Assert.Equal((made.GetAttribute("aria-controls"), true, true),
            (panel.Id, panel.HasAttribute("inert"), panel.ClassList.Contains("tb-dock__panel--hidden")));
        Assert.DoesNotContain(_log, line => line.StartsWith("dispose", StringComparison.Ordinal));
        Assert.Equal(["Map", "Notes", "Weather"], cut.Tb().Dock().Panels);
        Assert.Equal(button(), ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
        Assert.Equal(["Trails is auto-hidden."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(["auto_hide"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task Strip_ShouldSlideItsPanelOutOverTheDockAndBack_WhenItsButtonIsClicked()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var layout = Hike.Float("weather").AutoHide("notes", DockPosition.End);
        var changes = new List<DockLayout>();
        var cut = RenderDock(layout, HikePanels, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var tab = OnceMade(cut, "[role=tab][data-panel=notes]");
        // A panel that never showed isn't rendered until it slides out.
        Assert.Empty(cut.FindAll("[data-panel=notes]:not([data-part=strip-button])"));
        Assert.Null(cut.Find("[data-part=strip-button]").GetAttribute("aria-controls"));

        await cut.Find("[data-part=strip-button]").ClickAsync();

        // It is drawn as a window with one tab across the edge, in front of the weather's window and numbered after
        // it, a quarter of the dock deep.
        Assert.Equal(layout.Open("notes"), changes[^1]);
        const string box = "--tb-dock-x: 75; --tb-dock-y: 0; --tb-dock-w: 25; --tb-dock-h: 100; --tb-dock-z: 1;";
        var row = cut.Find("[data-part=group][data-edge]");
        Assert.Equal(("end", "2", box, true, "Auto-hidden panel"), (row.GetAttribute("data-edge"),
            row.GetAttribute("data-group"), row.GetAttribute("style"), row.HasAttribute("data-floating"),
            row.QuerySelector("[role=tablist]")!.GetAttribute("aria-label")));
        Assert.Contains("tb-dock__group--revealed", row.ClassList);
        Assert.Equal(["Notes"], Titles(row));
        var panel = cut.Find("[data-part=panel][data-panel=notes]");
        Assert.Equal(("end", "2", false), (panel.GetAttribute("data-edge"), panel.GetAttribute("data-dock-group"),
            panel.HasAttribute("inert")));
        Assert.StartsWith(box, panel.GetAttribute("style"));
        // It stays against its edge: it has neither a window's Move button nor its grip.
        Assert.Empty(row.QuerySelectorAll("[data-part=window-move]"));
        Assert.Single(cut.FindAll("[data-part=window-grip]"));
        var button = cut.Find("[data-part=strip-button]");
        Assert.Equal(("true", panel.Id), (button.GetAttribute("aria-expanded"), button.GetAttribute("aria-controls")));
        Assert.Equal(tab(), ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);

        await cut.Find("[data-part=strip-button]").ClickAsync();

        Assert.Equal(layout, changes[^1]);
        Assert.Empty(cut.FindAll("[data-part=group][data-edge]"));
        panel = cut.Find("[data-part=panel][data-panel=notes]");
        Assert.Equal((true, false), (panel.HasAttribute("inert"), panel.HasAttribute("data-edge")));
        Assert.Equal("false", cut.Find("[data-part=strip-button]").GetAttribute("aria-expanded"));
        Assert.DoesNotContain(_log, line => line.StartsWith("dispose", StringComparison.Ordinal));
        // Sliding out is counted as use, sliding back isn't, and each click is a finished action.
        Assert.Equal(["reveal"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
        Assert.Equal("2", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task SlideSeparator_ShouldResizeTheSlidOutPanel_WhenTheScriptReportsItsEdgeOrItsGripsMenuSteps()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var layout = Hike.AutoHide("notes", DockPosition.End).Open("notes");
        var changes = new List<DockLayout>();
        var cut = RenderDock(layout, HikePanels, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));

        // The panel's inner edge is a separator like the ones between groups, with the share of the dock it takes.
        var edge = cut.Find("[data-part=slide-separator]");
        Assert.Equal(("separator", "0", "vertical", "Resize Notes", "end"),
            (edge.GetAttribute("role"), edge.GetAttribute("tabindex"), edge.GetAttribute("aria-orientation"),
                edge.GetAttribute("aria-label"), edge.GetAttribute("data-edge")));
        Assert.Equal(("25", "10", "90"), (edge.GetAttribute("aria-valuenow"), edge.GetAttribute("aria-valuemin"),
            edge.GetAttribute("aria-valuemax")));
        Assert.Equal(cut.Find("[data-part=panel][data-panel=notes]").Id, edge.GetAttribute("aria-controls"));
        Assert.Equal("tb-dock__slide-separator tb-dock__slide-separator--end", edge.ClassName);
        const string box = "--tb-dock-x: 75; --tb-dock-y: 0; --tb-dock-w: 25; --tb-dock-h: 100; --tb-dock-z: 0;";
        var grip = cut.Find("[data-part=slide-grip]");
        Assert.Equal((box, box, "2", "2"), (edge.GetAttribute("style"), grip.GetAttribute("style"),
            edge.GetAttribute("data-group"), grip.GetAttribute("data-group")));
        // The grip's button stays out of the tab order, since the edge's keys do the same as its menu.
        var button = grip.QuerySelector("button")!;
        Assert.Equal(("Size of Notes", "-1"), (button.GetAttribute("aria-label"), button.GetAttribute("tabindex")));
        Assert.Equal(["Make Notes larger", "Make Notes smaller"],
            grip.QuerySelectorAll("[data-step]").Select(item => item.TextContent.Trim()));

        // The layout keeps the size to the four decimals the panel's style shows.
        await cut.InvokeAsync(() => Report().OnSlide("notes", 33.333333, 1));

        Assert.Equal([layout.ResizeAutoHidden("notes", 33.3333)], changes);
        Assert.Equal("33", cut.Find("[data-part=slide-separator]").GetAttribute("aria-valuenow"));
        Assert.StartsWith("--tb-dock-x: 66.6667; --tb-dock-y: 0; --tb-dock-w: 33.3333;",
            cut.Find("[data-part=group][data-edge]").GetAttribute("style"));
        Assert.Equal(("1", "1"), (cut.Find("[data-part=dock]").GetAttribute("data-report"),
            cut.Find("[data-part=dock]").GetAttribute("data-commits")));
        Assert.Equal(["resize_auto_hidden"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));

        // A step at a limit reports the size the panel has, and a panel that isn't on a strip has no edge: neither
        // changes anything, and both count as finished.
        await cut.InvokeAsync(() => Report().OnSlide("notes", 33.3333, 2));
        await cut.InvokeAsync(() => Report().OnSlide("trails", 50, 3));

        Assert.Single(changes);
        Assert.Equal("3", cut.Find("[data-part=dock]").GetAttribute("data-commits"));

        // The grip's menu asks the script for a step, so that the panel stays within its limits.
        await cut.Find("[data-part=slide-grip] [data-step=larger]").ClickAsync(new MouseEventArgs { Detail = 1 });
        await cut.Find("[data-part=slide-grip] [data-step=smaller]").ClickAsync(new MouseEventArgs { Detail = 1 });

        Assert.Equal(["10", "-10"], _handle.Invocations["stepSlide"].Select(call => $"{call.Arguments[0]}"));
    }

    [Fact]
    public async Task Driver_ShouldReadAndSetTheSizeOfASlidOutPanel_WhenGivenItsEdgesName()
    {
        var cut = RenderDock(Hike.AutoHide("notes", DockPosition.Top).Open("notes"), HikePanels);
        var dock = cut.Tb().Dock();
        Assert.Equal("horizontal", cut.Find("[data-part=slide-separator]").GetAttribute("aria-orientation"));
        Assert.Equal(25, dock.Size("Resize Notes"));

        await dock.ResizeAsync("Resize Notes", 40);

        Assert.Equal(40, dock.Size("Resize Notes"));
        Assert.StartsWith("--tb-dock-x: 0; --tb-dock-y: 0; --tb-dock-w: 100; --tb-dock-h: 40;",
            cut.Find("[data-part=group][data-edge]").GetAttribute("style"));
        // A docked separator keeps its own name and its own share.
        Assert.Equal(25, dock.Size("Resize Trails"));

        // The panel takes from a tenth to nine tenths of the dock, and no keys take its edge further.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dock.ResizeAsync("Resize Notes", 9));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dock.ResizeAsync("Resize Notes", 91));
        await dock.ResizeAsync("Resize Notes", 10);
        Assert.Equal(10, dock.Size("Resize Notes"));
        await dock.ResizeAsync("Resize Notes", 90);
        Assert.Equal(90, dock.Size("Resize Notes"));
    }

    [Fact]
    public void Render_ShouldRoundAHalfPercentUp_WhenAShareLiesHalfwayBetweenTwoWholePercents()
    {
        // The script rounds a half up when it states a separator's limits, so the page's value does too.
        var eighth = new DockLayout(new DockSplit(
            new DockGroup("trails", "map") { Size = 1 }, new DockGroup("notes", "weather") { Size = 7 }));
        var slid = Hike.AutoHide("notes", DockPosition.Start).ResizeAutoHidden("notes", 12.5).Open("notes");

        Assert.Equal("13", RenderDock(eighth, HikePanels).Find("[data-part=separator]").GetAttribute("aria-valuenow"));
        Assert.Equal("13",
            RenderDock(slid, HikePanels).Find("[data-part=slide-separator]").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public async Task Strip_ShouldTakeAPanel_WhenTheScriptReportsADropOnIt()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var layout = Hike.AutoHide("notes", DockPosition.Start);
        var changes = new List<DockLayout>();
        var cut = RenderDock(layout, HikePanels, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));

        // The script names the edge as the strip's data-edge does, in lower case.
        await cut.InvokeAsync(() => Report().OnAutoHide("trails", "start"));

        Assert.Equal([layout.AutoHide("trails", DockPosition.Start)], changes);
        Assert.Equal(["Notes", "Trails"],
            cut.FindAll("[data-part=strip-button]").Select(button => button.TextContent.Trim()));
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
        Assert.Equal("Trails is auto-hidden.", Services.GetRequiredService<FakeAnnouncer>().Polite[^1]);
        Assert.Equal(["auto_hide"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));

        // A drop the dock can't use changes nothing and still counts: a panel it doesn't have, or a name that isn't
        // one of the four edges.
        await cut.InvokeAsync(() => Report().OnAutoHide("nowhere", "start"));
        await cut.InvokeAsync(() => Report().OnAutoHide("weather", "before"));
        await cut.InvokeAsync(() => Report().OnAutoHide("weather", "middle"));

        Assert.Single(changes);
        Assert.Equal("4", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task Strip_ShouldSlideItsPanelBack_WhenTheScriptReportsEscapeOrAPressElsewhere()
    {
        var layout = Hike.AutoHide("notes", DockPosition.Bottom);
        var changes = new List<DockLayout>();
        var cut = RenderDock(layout.Open("notes"), HikePanels, changes.Add);
        var button = cut.Find("[data-part=strip-button]").GetAttribute("blazor:elementreference");

        // Escape gives the focus back to the panel's button and counts as a finished action.
        await cut.InvokeAsync(() => Report().OnConceal(true));

        Assert.Equal([layout], changes);
        Assert.Empty(cut.FindAll("[data-edge]:not([data-part=strip])"));
        Assert.Equal(button, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits"));

        // A press elsewhere leaves the focus where it went and isn't counted, like a window coming forward.
        await cut.Find("[data-part=strip-button]").ClickAsync();
        var focused = JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count;
        await cut.InvokeAsync(() => Report().OnConceal(false));

        Assert.Equal(layout, changes[^1]);
        Assert.Equal(focused, JSInterop.Invocations["Blazor._internal.domWrapper.focus"].Count);
        Assert.Equal("2", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
        // With nothing slid out, a report changes nothing.
        await cut.InvokeAsync(() => Report().OnConceal(true));
        Assert.Equal(3, changes.Count);
        Assert.Equal("2", cut.Find("[data-part=dock]").GetAttribute("data-commits"));
    }

    [Fact]
    public async Task AutoHide_ShouldOfferASlidOutPanelWhatItCanDo_WhenItsMenuIsRendered()
    {
        var cut = RenderDock(Hike.Float("weather").AutoHide("notes", DockPosition.Top).Open("notes"), HikePanels);
        var menu = cut.Find("[data-part=group][data-edge] [data-part=group-menu]");

        // It docks, floats, goes to a group, a window, an edge of the dock or another strip, and closes; it has no
        // group to split or to fill the dock.
        Assert.Equal(["back", "forward", "float", "dock", "close"],
            menu.QuerySelectorAll("[data-move]").Select(item => item.GetAttribute("data-move")));
        Assert.Equal(["start", "end", "bottom"],
            menu.QuerySelectorAll("[data-move-hide]").Select(item => item.GetAttribute("data-move-hide")));
        Assert.Equal(["Group 1: Trails", "Group 2: Weather"],
            menu.QuerySelectorAll("[data-move-group]").Select(item => item.TextContent.Trim()));
        Assert.Equal(4, menu.QuerySelectorAll("[data-move-edge]").Length);
        Assert.Empty(menu.QuerySelectorAll("[data-move-beside]"));
        // A docked group offers the four strips, and can't send a panel to the slid-out one.
        var docked = cut.FindAll("[data-part=group-menu]")[0];
        Assert.Equal(4, docked.QuerySelectorAll("[data-move-hide]").Length);
        Assert.Equal(["Group 2: Weather"],
            docked.QuerySelectorAll("[data-move-group]").Select(item => item.TextContent.Trim()));

        await menu.QuerySelector("[data-move=dock]")!.ClickAsync();

        // With its last panel gone, the strip goes too.
        Assert.Empty(cut.FindAll("[data-part=strip], [data-edge]"));
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], cut.Tb().Dock().Panels);
        Assert.Equal("Notes moved to panel group 1.", Services.GetRequiredService<FakeAnnouncer>().Polite[^1]);
    }

    [Fact]
    public async Task Driver_ShouldPutPanelsAwaySlideThemOutAndBringThemBack_WhenTheStripHelpersAreUsed()
    {
        var cut = RenderDock(Hike, HikePanels);
        var dock = cut.Tb().Dock();

        await dock.AutoHideAsync("Notes", DockPosition.End);
        await dock.AutoHideAsync("Map", DockPosition.Top);

        // The top strip's panels come first. A panel on a strip has no tab, so what it holds can't be reached yet.
        Assert.Equal(["Map", "Notes"], dock.AutoHidden);
        Assert.Equal(["Trails", "Weather"], dock.Panels);
        Assert.Equal("The panel \"Map\" is on a strip; slide it out with RevealAsync first.",
            Assert.Throws<InvalidOperationException>(() => dock.PanelElement("Map")).Message);
        Assert.Equal("The panel \"Map\" isn't in a floating window.",
            Assert.Throws<InvalidOperationException>(() => dock.Window("Map")).Message);

        await dock.RevealAsync("Notes");
        await dock.RevealAsync("Notes");

        // A slid-out panel is drawn as a window, but it isn't one. It has a tab, which shows.
        Assert.Equal("end", cut.Find("[data-part=group][data-edge]").GetAttribute("data-edge"));
        Assert.Empty(dock.Floating);
        Assert.Equal(["Trails", "Weather", "Notes"], dock.Panels);
        Assert.Equal(["Trails", "Weather", "Notes"], dock.Showing);
        Assert.Equal("The panel \"Notes\" isn't in a floating window.",
            Assert.Throws<InvalidOperationException>(() => dock.Window("Notes")).Message);
        Assert.Equal("The panel \"Notes\" is on that strip already.", (await Assert.ThrowsAsync<
            InvalidOperationException>(() => dock.AutoHideAsync("Notes", DockPosition.End))).Message);

        await dock.ConcealAsync();

        Assert.Empty(cut.FindAll("[data-part=group][data-edge]"));
        Assert.Equal("No auto-hidden panel is slid out.",
            (await Assert.ThrowsAsync<InvalidOperationException>(dock.ConcealAsync)).Message);
        Assert.Equal("The panel \"Trails\" isn't on a strip.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.RevealAsync("Trails"))).Message);

        // A helper that needs the panel's tab slides the panel out first, from a group's menu or the tab's own.
        await dock.FloatAsync("Notes");
        await dock.ChooseFromTabMenuAsync("Map", "Dock");

        Assert.Equal(["Notes"], dock.Floating);
        Assert.Empty(dock.AutoHidden);
        Assert.Equal(["Trails", "Weather", "Map", "Notes"], dock.Panels);

        // Closing one on a strip slides it out and clicks the close mark on its tab.
        await dock.AutoHideAsync("Weather", DockPosition.Bottom);
        await dock.CloseAsync("Weather");

        Assert.Empty(dock.AutoHidden);
        Assert.Equal(["Trails", "Map", "Notes"], dock.Panels);
    }

    [Fact]
    public async Task Close_ShouldCloseAPanelOnAStripAndMoveTheFocusOn_WhenItsMenuOrItsKeyClosesIt()
    {
        var layout = Hike.AutoHide("notes", DockPosition.End).AutoHide("map", DockPosition.End)
            .AutoHide("weather", DockPosition.End);
        var changes = new List<DockLayout>();
        var cut = RenderDock(layout.Open("map"), HikePanels, changes.Add);
        var dock = cut.Tb().Dock();
        string? Made(string selector) => cut.Find(selector).GetAttribute("blazor:elementreference");
        string Focused() => ((ElementReference)JSInterop.Invocations["Blazor._internal.domWrapper.focus"][^1]
            .Arguments[0]!).Id;
        var (trails, notes, weather) = (Made("[role=tab][data-panel=trails]"),
            Made("[data-part=strip-button][data-panel=notes]"), Made("[data-part=strip-button][data-panel=weather]"));

        // Close in the slid-out panel's menu gives the focus to the next button on its strip.
        await cut.Find("[data-part=group][data-edge] [data-part=group-menu] [data-move=close]").ClickAsync();

        Assert.Equal(layout.Close("map"), changes[^1]);
        Assert.Equal(["Notes", "Weather"], dock.AutoHidden);
        Assert.Empty(cut.FindAll("[data-edge]:not([data-part=strip])"));
        Assert.Equal(weather, Focused());
        Assert.Equal("1", cut.Find("[data-part=dock]").GetAttribute("data-commits"));

        // Delete on the tab of the strip's last panel gives it to the button before.
        await dock.RevealAsync("Weather");
        await cut.InvokeAsync(() => Report().OnClose("weather", true));

        Assert.Equal(["Notes"], dock.AutoHidden);
        Assert.Equal(notes, Focused());

        // With the strip's only panel gone, the strip goes too, and the first showing tab takes the focus.
        await dock.RevealAsync("Notes");
        await cut.InvokeAsync(() => Report().OnClose("notes", true));

        Assert.Empty(cut.FindAll("[data-part=strip], [data-edge]"));
        Assert.Equal(["map", "weather", "notes"], changes[^1].Closed);
        Assert.Equal(["Trails"], dock.Panels);
        Assert.Equal(trails, Focused());
    }

    [Fact]
    public async Task Close_ShouldGiveTheDockTheFocus_WhenTheLastPanelItShowsClosesFromAStrip()
    {
        var cut = RenderDock(new DockLayout(autoHidden: [new("notes", DockPosition.Start)], revealed: "notes"),
            HikePanels[2]);

        await cut.InvokeAsync(() => Report().OnClose("notes", true));

        // Nothing is left to show, so the dock's area, which can take the focus when it is empty, takes it.
        Assert.Equal("-1", cut.Find("[data-part=dock]").GetAttribute("tabindex"));
        Assert.Single(JSInterop.Invocations["Blazor._internal.domWrapper.focus"]);
        Assert.Empty(cut.FindAll("[role=tab], [data-part=strip-button]"));
    }

    [Fact]
    public async Task Close_ShouldGiveTheFocusToTheGroupThatFillsTheDock_WhenAStripsOnlyPanelClosesOverIt()
    {
        var cut = RenderDock(Hike.AutoHide("map", DockPosition.Start).Maximize("notes").Open("map"), HikePanels);
        var notes = cut.Find("[role=tab][data-panel=notes]").GetAttribute("blazor:elementreference");

        await cut.InvokeAsync(() => Report().OnClose("map", true));

        // The first group is covered, so its tab can't take the focus.
        Assert.Contains("tb-dock__group--covered", cut.Find("[data-part=group]").ClassList);
        Assert.Equal(notes, ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Fact]
    public async Task Render_ShouldShowTheEmptyContent_WhenEveryPanelIsOnAStrip()
    {
        var cut = RenderDock(Hike, HikePanels);
        var dock = cut.Tb().Dock();
        Assert.Empty(cut.FindAll("[data-part=empty]"));

        foreach (var title in new[] { "Trails", "Map", "Notes", "Weather" })
        {
            await dock.AutoHideAsync(title, DockPosition.Start);
        }

        // The panels that showed are kept hidden in the dock's area, which is empty all the same and can take focus.
        Assert.NotEmpty(cut.FindAll("[data-part=panel]"));
        Assert.Single(cut.FindAll("[data-part=empty]"));
        Assert.Equal("-1", cut.Find("[data-part=dock]").GetAttribute("tabindex"));

        // A panel slides out over the empty area.
        await dock.RevealAsync("Notes");
        Assert.Single(cut.FindAll("[data-part=empty]"));
        Assert.Equal("-1", cut.Find("[data-part=dock]").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task AutoHide_ShouldDisposeThePanelsContent_WhenThePanelIsNotKeptAlive()
    {
        var cut = RenderDock(Hike, [.. HikePanels[..2], new("notes", "Notes", KeepAlive: false), HikePanels[3]]);

        await cut.Tb().Dock().AutoHideAsync("Notes", DockPosition.Bottom);

        Assert.Contains("dispose notes", _log);
        Assert.Empty(cut.FindAll("[data-part=panel][data-panel=notes]"));
        Assert.Null(cut.Find("[data-part=strip-button]").GetAttribute("aria-controls"));
    }

    [Fact]
    public void Render_ShouldDrawTheStripsAndASlidOutPanel_WhenThePageIsStatic()
    {
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = RenderDock(Hike.AutoHide("notes", DockPosition.Start).AutoHide("map", DockPosition.Bottom)
            .AutoHide("weather", DockPosition.Start).Open("map"), HikePanels);

        // The strips before the area in the page are the top and the start ones, and the others come after it.
        Assert.Equal(["strip", "dock", "strip"],
            cut.Find("[data-part=frame]").Children.Select(child => child.GetAttribute("data-part")));
        Assert.Equal([("start", "Notes Weather"), ("bottom", "Map")], cut.FindAll("[data-part=strip]").Select(strip =>
            (strip.GetAttribute("data-edge"), string.Join(' ', strip.Children.Select(Text)))));
        Assert.Equal("bottom", cut.Find("[data-part=panel][data-panel=map]").GetAttribute("data-edge"));
        // The slid-out panel's edge is drawn, and its grip's menu waits for the page to be interactive.
        Assert.Single(cut.FindAll("[data-part=slide-separator]"));
        Assert.Empty(cut.FindAll("[data-part=slide-grip]"));
    }

    [Fact]
    public async Task Labels_ShouldWordTheStripsTheirMenuAndWhatIsSaid_WhenGiven()
    {
        var cut = RenderDock(Hike.AutoHide("notes", DockPosition.Start).Open("notes"), HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels
        {
            AutoHide = "Put away", Strip = "Put away: {0}", RevealedGroup = "Out for now",
            AutoHidden = "{0} is put away",
        }));

        Assert.Equal("Put away: At the start", cut.Find("[data-part=strip]").GetAttribute("aria-label"));
        Assert.Equal("Out for now", cut.Find("[data-edge] [role=tablist]").GetAttribute("aria-label"));
        var menu = cut.FindAll("[data-part=group-menu]")[0];
        Assert.Contains("Put away", menu.TextContent);

        await menu.QuerySelector("[data-move-hide=top]")!.ClickAsync();

        Assert.Equal("Trails is put away", Services.GetRequiredService<FakeAnnouncer>().Polite[^1]);
    }

    [Fact]
    public void Labels_ShouldWordASlidOutPanelsEdgeAndItsGrip_WhenGiven()
    {
        var cut = RenderDock(Hike.AutoHide("notes", DockPosition.Start).Open("notes"), HikePanels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels
        {
            Resize = "Edge of {0}", SizeMenu = "Sizes for {0}", Larger = "Grow {0}", Smaller = "Shrink {0}",
        }));

        Assert.Equal("Edge of Notes", cut.Find("[data-part=slide-separator]").GetAttribute("aria-label"));
        var grip = cut.Find("[data-part=slide-grip]");
        Assert.Equal("Sizes for Notes", grip.QuerySelector("button")!.GetAttribute("aria-label"));
        Assert.Equal(["Grow Notes", "Shrink Notes"],
            grip.QuerySelectorAll("[data-step]").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public async Task PopOut_ShouldBeOfferedInEachGroupsMenu_WhenAllowPopOutIsOn()
    {
        var cut = RenderDock(Hike, HikePanels);
        Assert.Empty(cut.FindAll("[data-move=pop-out]"));
        Assert.Equal("The panel \"Notes\" can't open in a window of its own: the dock's AllowPopOut is off, or the "
            + "panel's is.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Dock().PopOutAsync("Notes"))).Message);

        cut.Render(parameters => parameters.Add(dock => dock.AllowPopOut, true));

        var items = cut.FindAll("[data-part=group-menu] [data-move=pop-out]");
        Assert.Equal(3, items.Count);
        Assert.All(items, item => Assert.Equal("Open in new window", item.TextContent.Trim()));
    }

    [Fact]
    public async Task PopOut_ShouldBeOfferedForEveryPanelButOne_WhenThatPanelsAllowPopOutIsOff()
    {
        var cut = RenderPoppable(Hike, panels: [.. HikePanels.Select(Staying)]);

        // The notes have a group to themselves, and the other two groups still offer it.
        Assert.Equal(2, cut.FindAll("[data-part=group-menu] [data-move=pop-out]").Count);
        Assert.Equal("The panel \"Notes\" can't open in a window of its own: the dock's AllowPopOut is off, or the "
            + "panel's is.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => cut.Tb().Dock().PopOutAsync("Notes"))).Message);
    }

    [Fact]
    public async Task PopOut_ShouldEnd_WhenThePanelsAllowPopOutTurnsOff()
    {
        var cut = RenderPoppable(Hike);
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Notes");

        cut.Render(parameters => parameters.Add(each => each.ChildContent, Panels([.. HikePanels.Select(Staying)])));

        // The panel comes back where it was docked.
        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
        Assert.Empty((string[])_handle.Invocations["popped"][^1].Arguments[0]!);
    }

    // The hike's panels with the notes kept on the page.
    private static Panel Staying(Panel panel) => panel with { AllowPopOut = panel.Id != "notes" };

    [Fact]
    public async Task PopOut_ShouldDrawThePanelAsItIsInItsOwnWindow_WhenOpenInNewWindowIsChosen()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        DockLayout? changed = null;
        var cut = RenderPoppable(Hike, layout => changed = layout);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var dock = cut.Tb().Dock();

        var popped = await dock.PopOutAsync("Notes");

        // The layout keeps a floating window for the panel while it is away.
        Assert.Equal(["notes"], changed!.Floating.Single().Group.Panels);
        Assert.Equal(["Notes"], dock.PoppedOut);
        var panel = cut.Find("[data-part=panel][data-panel=notes]");
        Assert.Equal(panel.Id, popped.Id);
        Assert.Equal(("region", "Notes", null),
            (panel.GetAttribute("role"), panel.GetAttribute("aria-label"), panel.GetAttribute("aria-labelledby")));
        Assert.Contains("tb-dock__panel--popped", panel.ClassName);
        Assert.Equal("Return to the dock",
            panel.QuerySelector("[data-part=return-bar] [data-part=return]")!.TextContent.Trim());
        Assert.Equal(panel.Id, dock.PanelElement("Notes").Id);
        // Its tab, its window's row and its grip are gone until it comes back, and its content was never disposed.
        Assert.Equal(["Trails", "Map", "Weather"], dock.Panels);
        Assert.Empty(cut.FindAll("[data-part=group][data-floating], [data-part=window-grip]"));
        Assert.DoesNotContain("dispose notes", _log);
        // The script hears of the panel as it is asked to move it, and isn't told again.
        Assert.Equal(panel.Id, _handle.Invocations["popOut"].Single().Arguments.Single());
        Assert.Empty(_handle.Invocations["popped"]);
        Assert.Equal(["Notes opened in a new window."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(["pop_out"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task PopOut_ShouldKeepTheWindowThePanelHasToItself_WhenThePanelFloatsAlone()
    {
        var layout = Hike.FloatAt("notes", 10, 20, 30, 40);
        DockLayout? changed = null;
        var cut = RenderPoppable(layout, each => changed = each);

        await cut.Tb().Dock().PopOutAsync("Notes");

        Assert.Equal(layout, changed);
        Assert.Equal(["Notes"], cut.Tb().Dock().PoppedOut);

        // It floated before it went, so that window is where it comes back to.
        await cut.Tb().Dock().ReturnAsync("Notes");

        Assert.Equal(layout, changed);
        Assert.Equal(["Notes"], cut.Tb().Dock().Floating);
    }

    [Fact]
    public async Task PopOut_ShouldSaveTheLayoutWithThePanelDockedWhereItWas_WhenThePanelIsInAWindowOfItsOwn()
    {
        DockLayout? changed = null;
        var cut = RenderPoppable(Hike, layout => changed = layout);
        cut.Render(parameters => parameters.Add(dock => dock.PersistenceKey, "hike"));
        await cut.Tb().Dock().PopOutAsync("Notes");

        // The app hears of the floating window the panel has while it is away. What is kept has no such window,
        // since no page can open the browser's window again as it loads.
        Assert.Equal(["notes"], changed!.Floating.Single().Group.Panels);
        Assert.Equal(Hike, cut.Instance.LayoutToSave);
        Assert.Equal(Hike.ToJson(), _handle.Invocations["save"][^1].Arguments[1]);
    }

    [Fact]
    public async Task LayoutToSave_ShouldPutEachPanelBackWhereItWas_WhenSeveralAreAwayOrOneFloatedAlready()
    {
        var floating = Hike.Float("weather");
        var cut = RenderPoppable(floating);
        var dock = cut.Tb().Dock();

        await dock.PopOutAsync("Notes");
        await dock.PopOutAsync("Trails");
        // A panel that had a floating window to itself keeps it in what is saved.
        await dock.PopOutAsync("Weather");

        Assert.Equal(floating.ToJson(), cut.Instance.LayoutToSave.ToJson());

        // What changes while panels are away is kept, with the panels that are still away docked.
        await dock.ReturnAsync("Notes");
        await dock.CloseAsync("Notes");

        var kept = cut.Instance.LayoutToSave;
        Assert.Equal(["notes"], kept.Closed);
        Assert.Equal(["weather"], kept.Floating.SelectMany(window => window.Group.Panels));
        Assert.Equal(["trails", "map"], kept.Groups.Single().Panels);
    }

    [Fact]
    public async Task PopOut_ShouldPutThePanelBackWhereItWasDocked_WhenTheBrowserRefusesTheWindow()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        _handle.Setup<string?>("popOut", _ => true).SetResult("blocked");
        DockLayout? changed = null;
        var cut = RenderPoppable(Hike, layout => changed = layout);
        cut.Render(parameters => parameters.Add(each => each.TelemetryName, "hike-planner"));
        var dock = cut.Tb().Dock();

        await dock.PopOutAsync("Notes");

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        Assert.Equal(Hike, changed);
        Assert.Equal("tabpanel", cut.Find("[data-part=panel][data-panel=notes]").GetAttribute("role"));
        Assert.Empty(cut.FindAll("[data-part=return-bar]"));
        Assert.Equal(["The browser blocked the new window for Notes."],
            Services.GetRequiredService<FakeAnnouncer>().Polite);
        // The script counted the panel as away from the moment it was asked to move it, and is told it isn't.
        Assert.Empty((string[])_handle.Invocations["popped"].Single().Arguments[0]!);
        // The choice is counted whether or not the browser opens the window.
        Assert.Equal(["pop_out"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task PopOut_ShouldPutThePanelBackAndSayItsWindowClosedBeforeThePanelWasReady_WhenTheDockAsksTooLate()
    {
        _handle.Setup<string?>("popOut", _ => true).SetResult("lapsed");
        var cut = RenderPoppable(Hike);
        var dock = cut.Tb().Dock();

        await dock.PopOutAsync("Notes");

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        // The browser refused nothing, so the user isn't told it did.
        Assert.Equal(["The new window for Notes closed before the panel was ready. Try again."],
            Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task PopOut_ShouldPutThePanelBackSayNothingAndLeaveTheFocus_WhenNoClickOpenedAWindowForIt()
    {
        _handle.Setup<string?>("popOut", _ => true).SetResult("unasked");
        var cut = RenderPoppable(Hike);
        var dock = cut.Tb().Dock();

        await dock.PopOutAsync("Notes");

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
        Assert.Empty(Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.DoesNotContain(JSInterop.Invocations, call => call.Identifier.EndsWith("focus"));
    }

    [Fact]
    public void ConnectionLost_ShouldGiveTheScriptItsText_WhenPanelsCanOpenInWindowsAndTheDockRunsOnTheServer()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var cut = RenderPoppable(Hike);

        Assert.Equal("The page lost its connection to the server. This panel works again when the page reconnects.",
            cut.Find("[data-part=dock]").GetAttribute("data-connection-lost"));

        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels { ConnectionLost = "Offline." }));

        Assert.Equal("Offline.", cut.Find("[data-part=dock]").GetAttribute("data-connection-lost"));

        // A dock that runs in the browser goes on working when the page's connection to the server is lost.
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels { ConnectionLost = "Gone." }));

        Assert.False(cut.Find("[data-part=dock]").HasAttribute("data-connection-lost"));

        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        cut.Render(parameters => parameters.Add(dock => dock.AllowPopOut, false));

        Assert.False(cut.Find("[data-part=dock]").HasAttribute("data-connection-lost"));
    }

    [Fact]
    public void PopOut_ShouldGiveEachGroupsItemItsShowingPanelsId_WhenAllowPopOutIsOn()
    {
        var cut = RenderPoppable(Hike);

        // The script keeps the window a click opens for that panel, so two quick clicks don't share one.
        Assert.All(cut.FindAll("[data-part=group]"), group =>
        {
            var showing = group.QuerySelector("[role=tab][aria-selected=true]")!.GetAttribute("data-panel");
            Assert.Equal(cut.Find($"[data-part=panel][data-panel={showing}]").Id,
                group.QuerySelector("[data-part=group-menu] [data-move=pop-out]")!.GetAttribute("data-pop"));
        });
    }

    [Fact]
    public async Task PopOut_ShouldPutThePanelBackAndSayNothing_WhenTheWindowClosesBeforeThePanelGetsThere()
    {
        _handle.Setup<string?>("popOut", _ => true).SetResult("closed");
        var cut = RenderPoppable(Hike);
        var dock = cut.Tb().Dock();

        await dock.PopOutAsync("Notes");

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
        Assert.Empty(cut.FindAll("[data-part=return-bar]"));
        Assert.Empty(Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task PopOut_ShouldLeaveThePanelAway_WhenTheScriptSaysALaterAskTookOver()
    {
        _handle.Setup<string?>("popOut", _ => true).SetResult("replaced");
        var cut = RenderPoppable(Hike);
        var dock = cut.Tb().Dock();

        await dock.PopOutAsync("Notes");

        Assert.Equal(["Notes"], dock.PoppedOut);
        Assert.Empty(Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Return_ShouldTellTheScriptThePanelIsBack_WhenItsWindowIsStillLoading()
    {
        // The script's answer stays out, as it does while the panel's window waits for its style sheets.
        _handle.Setup<string?>("popOut", _ => true);
        var cut = RenderPoppable(Hike);
        await cut.Tb().Dock().PopOutAsync("Notes");

        await cut.Instance.ReturnAsync("notes");

        // The script closes a window that is still loading when it is told the panel isn't away.
        cut.WaitForAssertion(() => Assert.Empty((string[])_handle.Invocations["popped"][^1].Arguments[0]!));
    }

    [Fact]
    public async Task PopOut_ShouldNeitherTellTheAppNorCallTheScript_WhenTheScriptAnswersAfterTheDockWent()
    {
        var answer = _handle.Setup<string?>("popOut", _ => true);
        var changes = new List<DockLayout>();
        var cut = RenderPoppable(Hike, changes.Add);
        cut.Render(parameters => parameters.Add(dock => dock.PersistenceKey, "hike"));
        await cut.Tb().Dock().PopOutAsync("Notes");
        var (changed, saved) = (changes.Count, _handle.Invocations["save"].Count);

        await DisposeComponentsAsync();
        // The script closes the window that was loading as it is disposed, and says so.
        answer.SetResult("closed");
        await Renderer.Dispatcher.InvokeAsync(() => { });

        Assert.Equal(changed, changes.Count);
        Assert.Equal(saved, _handle.Invocations["save"].Count);
        Assert.Empty(_handle.Invocations["popped"]);
    }

    [Fact]
    public async Task PopOut_ShouldStopWithoutFailing_WhenTheAppTakesARefusedPanelsLayoutAfterTheDockWent()
    {
        _handle.Setup<string?>("popOut", _ => true).SetResult("blocked");
        var taken = new TaskCompletionSource();
        var cut = RenderPoppable(Hike);
        // The app is slow over the layout that puts the panel back, as one that saves it to a server is.
        cut.Render(parameters => parameters.Add(dock => dock.LayoutChanged,
            layout => layout.Equals(Hike) ? taken.Task : Task.CompletedTask));
        await cut.Tb().Dock().PopOutAsync("Notes");

        await DisposeComponentsAsync();
        taken.SetResult();
        await Renderer.Dispatcher.InvokeAsync(() => { });

        Assert.False(Renderer.UnhandledException.IsCompleted);
        Assert.Empty(_handle.Invocations["popped"]);
    }

    [Fact]
    public async Task Away_ShouldKeepANamedButtonForThePanel_WhenItShowsInAWindowOfItsOwn()
    {
        var cut = RenderPoppable(Hike);
        Assert.Empty(cut.FindAll("[data-part=away]"));

        await cut.Tb().Dock().PopOutAsync("Notes");

        var away = cut.Find("[data-part=away]");
        Assert.Equal(("group", "Panels in their own windows"),
            (away.GetAttribute("role"), away.GetAttribute("aria-label")));
        var button = Assert.Single(away.QuerySelectorAll("[data-part=away-menu] [data-part=trigger]"));
        Assert.Equal("Notes", button.TextContent.Trim());
        Assert.Equal("In its own browser window",
            cut.Find($"[id='{button.GetAttribute("aria-describedby")}']").TextContent);
        // The script brings the window forward from the first item, by the panel's element id.
        Assert.Equal(cut.Find("[data-part=panel][data-panel=notes]").Id,
            away.QuerySelector("[data-away=show]")!.GetAttribute("data-window"));
        Assert.Equal(["Show its window", "Return to the dock"],
            away.QuerySelectorAll("[role=menuitem]").Select(item => item.TextContent.Trim()));
    }

    [Fact]
    public async Task Away_ShouldMarkTheButtonAndTellScreenReaders_WhenThePanelInItsOwnWindowIsModified()
    {
        var cut = RenderPoppable(Hike, panels: [.. HikePanels.Select(panel => panel with { Modified = true })]);

        await cut.Tb().Dock().PopOutAsync("Notes");

        var button = cut.Find("[data-part=away-menu] [data-part=trigger]");
        Assert.True(button.HasAttribute("data-modified"));
        Assert.Equal("true", button.QuerySelector("[data-part=modified]")!.GetAttribute("aria-hidden"));
        Assert.Equal(["In its own browser window", "Unsaved changes"], button.GetAttribute("aria-describedby")!
            .Split(' ').Select(id => cut.Find($"[id='{id}']").TextContent));
        // Its window's return button says so too, from a hint that goes with it into the window.
        var back = cut.Find("[data-panel=notes] [data-part=return]");
        Assert.True(back.HasAttribute("data-modified"));
        Assert.NotNull(back.QuerySelector("[data-part=modified]"));
        Assert.Equal("Unsaved changes", cut.Find($"[data-panel=notes] [id='{back.GetAttribute("aria-describedby")}']")
            .TextContent.Trim());
    }

    [Fact]
    public async Task Away_ShouldLeaveTheButtonUnmarked_WhenThePanelInItsOwnWindowIsntModified()
    {
        var cut = RenderPoppable(Hike);

        await cut.Tb().Dock().PopOutAsync("Notes");

        var button = cut.Find("[data-part=away-menu] [data-part=trigger]");
        Assert.False(button.HasAttribute("data-modified"));
        Assert.Null(button.QuerySelector("[data-part=modified]"));
        Assert.Single(button.GetAttribute("aria-describedby")!.Split(' '));
        Assert.Null(cut.Find("[data-panel=notes] [data-part=return]").GetAttribute("aria-describedby"));
    }

    [Fact]
    public async Task Away_ShouldBringThePanelBackWithTheFocusOnItsTab_WhenReturnIsChosenInItsMenu()
    {
        var cut = RenderPoppable(Hike);
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Notes");
        var tab = OnceMade(cut, "[role=tab][data-panel=notes]");

        await cut.Find("[data-part=away] [data-away=return]").ClickAsync(new());

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        Assert.Empty(cut.FindAll("[data-part=away]"));
        Assert.Equal(tab(), ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    [Fact]
    public async Task PoppedOut_ShouldTellTheAppWhichPanelsAreAway_WhenOneGoesAndTheAppBringsItBack()
    {
        var changes = new List<IReadOnlyList<string>>();
        var cut = RenderPoppable(Hike);
        cut.Render(parameters => parameters.Add(each => each.PoppedOutChanged, changes.Add));
        Assert.Empty(cut.Instance.PoppedOut);

        await cut.Tb().Dock().PopOutAsync("Notes");

        Assert.Equal(["notes"], cut.Instance.PoppedOut);
        Assert.Equal([["notes"]], changes);

        // A panel that isn't away changes nothing.
        await cut.Instance.ReturnAsync("map");
        Assert.Equal(["notes"], cut.Instance.PoppedOut);
        Assert.Equal([["notes"]], changes);

        await cut.Instance.ReturnAsync("notes");

        cut.WaitForAssertion(() => Assert.Equal([["notes"], []], changes));
        Assert.Empty(cut.Instance.PoppedOut);
        Assert.Empty(cut.Tb().Dock().Floating);
    }

    [Fact]
    public async Task PoppedOut_ShouldStayEmptyAndTellTheAppNothing_WhenTheBrowserRefusesTheWindow()
    {
        _handle.Setup<string?>("popOut", _ => true).SetResult("blocked");
        var changes = new List<IReadOnlyList<string>>();
        var cut = RenderPoppable(Hike);
        cut.Render(parameters => parameters.Add(each => each.PoppedOutChanged, changes.Add));

        await cut.Tb().Dock().PopOutAsync("Notes");

        Assert.Empty(cut.Instance.PoppedOut);
        Assert.Empty(changes);
        Assert.Empty(cut.FindAll("[data-part=away]"));
    }

    [Fact]
    public async Task Return_ShouldPutThePanelBackWhereItWasDocked_WhenItsButtonIsPressedOrItsBrowserWindowCloses()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        DockLayout? changed = null;
        var cut = RenderPoppable(Hike, layout => changed = layout);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));
        var dock = cut.Tb().Dock();
        IReadOnlyList<string> Announced() => Services.GetRequiredService<FakeAnnouncer>().Polite;
        await dock.PopOutAsync("Notes");
        var tab = OnceMade(cut, "[role=tab][data-panel=notes]");

        await dock.ReturnAsync("Notes");

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        // Nothing else changed while it was away, so the layout is the one it left, sizes and all.
        Assert.Equal(Hike, changed);
        Assert.Equal("Notes is back in the dock.", Announced()[^1]);
        Assert.Empty((string[])_handle.Invocations["popped"][^1].Arguments[0]!);
        // The button's press leaves the focus on the panel's tab.
        Assert.Equal(tab(), ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
        Assert.DoesNotContain("dispose notes", _log);

        // The script brings the panel back itself when its window closes, and only tells the dock.
        await dock.PopOutAsync("Notes");
        await cut.InvokeAsync(() => Report().OnReturn("notes"));

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        Assert.Equal(Hike, changed);
        Assert.Equal(2, Announced().Count(message => message == "Notes is back in the dock."));
        // The focus stays where the user put it, so the earlier call is still the only one.
        JSInterop.VerifyFocusAsyncInvoke();
        Assert.Equal(["pop_out", "return", "pop_out", "return"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
        Assert.Equal("The panel \"Notes\" isn't in a browser window of its own.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.ReturnAsync("Notes"))).Message);
    }

    [Fact]
    public async Task PopOut_ShouldEnd_WhenTheAppTakesThePanelElsewhereOrTurnsAllowPopOutOff()
    {
        var cut = RenderPoppable(Hike);
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Notes");

        // The app docks the panel beside the trails.
        cut.Render(parameters => parameters.Add(each => each.Layout, Hike.Move("notes", "trails", DockPosition.After)));

        Assert.Empty(dock.PoppedOut);
        Assert.Equal(["Trails", "Notes", "Map", "Weather"], dock.Panels);
        Assert.Empty((string[])_handle.Invocations["popped"][^1].Arguments[0]!);

        // A panel that may no longer be in a window goes back where it was, beside the trails.
        await dock.PopOutAsync("Notes");
        cut.Render(parameters => parameters.Add(each => each.AllowPopOut, false));

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        Assert.Equal(["Trails", "Notes", "Map", "Weather"], dock.Panels);
        Assert.Empty((string[])_handle.Invocations["popped"][^1].Arguments[0]!);
    }

    [Fact]
    public async Task Return_ShouldPutThePanelBesideAnOldNeighbor_WhenTheLayoutChangedWhileItWasAway()
    {
        DockLayout? changed = null;
        var cut = RenderPoppable(Hike, layout => changed = layout);
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Map");

        // The weather closes while the map is away, so the layout the map left can't be put back as it was.
        await dock.CloseAsync("Weather");
        await dock.ReturnAsync("Map");

        Assert.Empty(dock.Floating);
        Assert.Equal(["Trails", "Map", "Notes"], dock.Panels);
        Assert.Equal(["trails", "map"], changed!.GroupOf("map")!.Panels);
        Assert.Equal(["weather"], changed.Closed);
    }

    [Fact]
    public async Task PopOut_ShouldPutEveryPanelBackInTheLayoutTheyLeft_WhenSeveralEndTogether()
    {
        DockLayout? changed = null;
        var cut = RenderPoppable(Hike, layout => changed = layout);
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Trails");
        await dock.PopOutAsync("Map");

        // Both go back at once. The map went last, so it goes back first, and each finds the layout it left.
        cut.Render(parameters => parameters.Add(each => each.AllowPopOut, false));

        cut.WaitForAssertion(() => Assert.Equal(Hike, changed));
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
    }

    [Fact]
    public async Task Return_ShouldPutTwoTabsBackInTheirGroup_WhenTheyComeBackInTheOrderTheyWent()
    {
        DockLayout? changed = null;
        var cut = RenderPoppable(Hike, layout => changed = layout);
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Trails");
        await dock.PopOutAsync("Map");

        // The map left a layout in which the trails floated, so it has to learn where the trails went back to.
        await dock.ReturnAsync("Trails");
        await dock.ReturnAsync("Map");

        Assert.Empty(dock.Floating);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
        Assert.Equal(["trails", "map"], changed!.GroupOf("map")!.Panels);
        Assert.Equal(Hike.Open("map"), changed);
    }

    [Fact]
    public async Task Return_ShouldFocusThePanelsButtonOnItsStrip_WhenThePanelGoesBackToAStrip()
    {
        var cut = RenderPoppable(Hike.AutoHide("notes", DockPosition.End));
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Notes");
        var button = OnceMade(cut, "[data-part=strip-button][data-panel=notes]");

        // The weather closes while the notes are away, so they go back to their strip and not out over the dock.
        await dock.CloseAsync("Weather");
        await cut.Find("[data-part=away] [data-away=return]").ClickAsync(new());

        Assert.Equal(["Notes"], dock.AutoHidden);
        // The last focus the dock moved, after the one the closed tab's neighbor took.
        Assert.Equal(button(), ((ElementReference)JSInterop.Invocations
            .Last(call => call.Identifier.EndsWith("focus")).Arguments[0]!).Id);
    }

    [Fact]
    public async Task Panel_ShouldComeBackWhereItWasDocked_WhenTheAppTakesItAwayInItsWindowAndMakesItAgain()
    {
        DockLayout? changed = null;
        var cut = RenderPoppable(Hike, layout => changed = layout);
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Notes");

        // The app takes the panel away, as when a task's form is done, and shows it again later.
        cut.Render(parameters => parameters.Add(each => each.ChildContent,
            Panels([.. HikePanels.Where(panel => panel.Id != "notes")])));
        cut.WaitForAssertion(() => Assert.Equal(Hike, changed));
        cut.Render(parameters => parameters.Add(each => each.ChildContent, Panels(HikePanels)));

        Assert.Empty(dock.PoppedOut);
        Assert.Empty(dock.Floating);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
        // No second window is asked for: a page can't open one unless the user has just acted.
        Assert.Single(_handle.Invocations["popOut"]);
    }

    // The hike's panels, with a time for the notes' browser window to stay open after the app takes them away.
    private static Panel[] WithNotesKept(TimeSpan time, bool allowPopOut = true) => [.. HikePanels.Select(panel =>
        panel.Id == "notes" ? panel with { KeepWindowFor = time, AllowPopOut = allowPopOut } : panel)];

    // Pops the notes out, has the app take them away, and gives what the script was told to keep.
    private async Task<JsonElement> TakeNotesAwayInTheirWindowAsync(IRenderedComponent<TbDockManager> cut,
        Panel[] panels)
    {
        await cut.Tb().Dock().PopOutAsync("Notes");
        cut.Render(parameters => parameters.Add(each => each.ChildContent,
            Panels([.. panels.Where(panel => panel.Id != "notes")])));
        cut.WaitForAssertion(() => Assert.Empty((string[])_handle.Invocations["popped"][^1].Arguments[0]!));
        return JsonSerializer.SerializeToElement(_handle.Invocations["popped"][^1].Arguments[1],
            JsonSerializerOptions.Web);
    }

    // -10,000 ticks are Timeout.InfiniteTimeSpan, and the others are longer than a browser's timer can wait.
    [Theory]
    [InlineData(-10_000L)]
    [InlineData(25 * TimeSpan.TicksPerDay)]
    [InlineData(long.MaxValue)]
    public async Task KeepWindowFor_ShouldGiveTheScriptNoTime_WhenGivenAnEndlessOrVeryLongTime(long ticks)
    {
        var panels = WithNotesKept(TimeSpan.FromTicks(ticks));
        var cut = RenderPoppable(Hike, panels: panels);

        var kept = Assert.Single((await TakeNotesAwayInTheirWindowAsync(cut, panels)).EnumerateArray());

        Assert.Equal(JsonValueKind.Null, kept.GetProperty("lifetime").ValueKind);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-TimeSpan.TicksPerSecond)]
    public async Task KeepWindowFor_ShouldKeepNoWindow_WhenGivenNoTimeOrLess(long ticks)
    {
        var panels = WithNotesKept(TimeSpan.FromTicks(ticks));
        var cut = RenderPoppable(Hike, panels: panels);

        Assert.Empty((await TakeNotesAwayInTheirWindowAsync(cut, panels)).EnumerateArray());
    }

    [Fact]
    public void KeepWindowFor_ShouldKeepNoWindow_WhenThePanelIsTakenAwayWhileDocked()
    {
        var panels = WithNotesKept(TimeSpan.FromMinutes(10));
        var cut = RenderPoppable(Hike, panels: panels);

        cut.Render(parameters => parameters.Add(each => each.ChildContent,
            Panels([.. panels.Where(panel => panel.Id != "notes")])));
        cut.Render(parameters => parameters.Add(each => each.ChildContent, Panels(panels)));

        Assert.Empty(cut.Tb().Dock().PoppedOut);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], cut.Tb().Dock().Panels);
        Assert.Empty(_handle.Invocations["popOut"]);
    }

    [Fact]
    public async Task KeepWindowFor_ShouldCloseTheKeptWindow_WhenTheDockStopsAllowingWindowsWhileThePanelIsAway()
    {
        var panels = WithNotesKept(TimeSpan.FromMinutes(10));
        var cut = RenderPoppable(Hike, panels: panels);
        await TakeNotesAwayInTheirWindowAsync(cut, panels);

        cut.Render(parameters => parameters.Add(dock => dock.AllowPopOut, false));

        cut.WaitForAssertion(() => Assert.EndsWith("-panel-notes",
            (string)Assert.Single(_handle.Invocations["unkeep"]).Arguments[0]!));
    }

    [Fact]
    public async Task KeepWindowFor_ShouldKeepTheWindowAndShowThePanelInItAgain_WhenTheAppTakesThePanelAwayAndBack()
    {
        using var used = new MetricCollector<long>(Services.GetRequiredService<IMeterFactory>(), "Trailblazor",
            "trailblazor.control.used");
        var panels = WithNotesKept(TimeSpan.FromMinutes(10));
        var changes = new List<DockLayout>();
        var cut = RenderPoppable(Hike, changes.Add, panels);
        cut.Render(parameters => parameters.Add(dock => dock.TelemetryName, "hike-planner"));

        var kept = Assert.Single((await TakeNotesAwayInTheirWindowAsync(cut, panels)).EnumerateArray());

        // The script hears which panel's window stays, what it says and for how long.
        Assert.EndsWith("-panel-notes", kept.GetProperty("id").GetString());
        Assert.Equal("Notes isn't open right now. This window shows it again when it is.",
            kept.GetProperty("text").GetString());
        Assert.Equal(600000, kept.GetProperty("lifetime").GetDouble());

        cut.Render(parameters => parameters.Add(each => each.ChildContent, Panels(panels)));

        // The panel goes to the window that waited for it, with no click: the app hears of the one layout that
        // floats it, and only the user's own choice of Open in new window was counted.
        cut.WaitForAssertion(() => Assert.Equal(["Notes"], cut.Tb().Dock().PoppedOut));
        Assert.Equal(2, _handle.Invocations["popOut"].Count);
        Assert.Equal([changes[0], Hike, changes[0]], changes);
        Assert.Equal(["pop_out"],
            used.GetMeasurementSnapshot().Select(measurement => measurement.Tags["trailblazor.action"]));
    }

    [Fact]
    public async Task KeepWindowFor_ShouldShowThePanelWhereItWasDocked_WhenTheKeptWindowIsGone()
    {
        var panels = WithNotesKept(TimeSpan.FromMinutes(10));
        var cut = RenderPoppable(Hike, panels: panels);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels { EmptyWindow = "{0} is away." }));

        var kept = Assert.Single((await TakeNotesAwayInTheirWindowAsync(cut, panels)).EnumerateArray());

        // The app's label replaces the library's text.
        Assert.Equal("Notes is away.", kept.GetProperty("text").GetString());

        // The script says that the window closed: its time was up, or the user closed it.
        await cut.InvokeAsync(() => Report().OnKeptWindowClosed("notes"));
        cut.Render(parameters => parameters.Add(each => each.ChildContent, Panels(panels)));

        Assert.Empty(cut.Tb().Dock().PoppedOut);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], cut.Tb().Dock().Panels);
        Assert.Single(_handle.Invocations["popOut"]);
    }

    [Fact]
    public async Task KeepWindowFor_ShouldCloseTheKeptWindow_WhenThePanelComesBackUnableToShowInOne()
    {
        var panels = WithNotesKept(TimeSpan.FromMinutes(10));
        var cut = RenderPoppable(Hike, panels: panels);
        await TakeNotesAwayInTheirWindowAsync(cut, panels);

        cut.Render(parameters => parameters.Add(each => each.ChildContent,
            Panels(WithNotesKept(TimeSpan.FromMinutes(10), allowPopOut: false))));

        cut.WaitForAssertion(() => Assert.EndsWith("-panel-notes",
            (string)Assert.Single(_handle.Invocations["unkeep"]).Arguments[0]!));
        Assert.Single(_handle.Invocations["popOut"]);
    }

    [Fact]
    public async Task Close_ShouldGiveTheFocusToTheDock_WhenTheOnlyOtherPanelIsInABrowserWindowOfItsOwn()
    {
        var cut = RenderPoppable(new DockLayout(new DockGroup("trails", "notes")),
            panels: [new("trails", "Trails"), new("notes", "Notes")]);
        var dock = cut.Tb().Dock();
        await dock.PopOutAsync("Notes");

        await cut.InvokeAsync(() => Report().OnClose("trails", true));

        // The popped panel has no tab in the dock, so the dock is empty and can take the focus. Only a browser can
        // tell that the dock is what took it.
        Assert.Equal(["Notes"], dock.PoppedOut);
        JSInterop.VerifyFocusAsyncInvoke();
        Assert.Equal("-1", cut.Find("[data-part=dock]").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task Labels_ShouldWordThePopOutItemTheReturnButtonAndWhatIsSaid_WhenGiven()
    {
        _handle.Setup<string?>("popOut", call => ((string)call.Arguments[0]!).EndsWith("-panel-map"))
            .SetResult("blocked");
        var cut = RenderPoppable(Hike);
        cut.Render(parameters => parameters.Add(dock => dock.Labels, new DockLabels
        {
            PopOut = "Own window", Return = "Back", PoppedOut = "{0} left", Returned = "{0} came back",
            PopOutBlocked = "No window for {0}",
        }));
        var dock = cut.Tb().Dock();
        Assert.All(cut.FindAll("[data-move=pop-out]"), item => Assert.Equal("Own window", item.TextContent.Trim()));

        await dock.PopOutAsync("Notes");
        Assert.Equal("Back", cut.Find("[data-part=return]").TextContent.Trim());
        await dock.ReturnAsync("Notes");
        await dock.PopOutAsync("Map");

        Assert.Equal(["Notes left", "Notes came back", "No window for Map"],
            Services.GetRequiredService<FakeAnnouncer>().Polite);
    }

    [Fact]
    public async Task Close_ShouldHandAFailingLayoutChangedToTheRenderer_WhenTheScriptStartsIt()
    {
        var cut = RenderDock(Hike, HikePanels, Fail);

        await cut.InvokeAsync(() => Report().OnClose("map", true));

        var failure = await Renderer.UnhandledException.WaitAsync(TimeSpan.FromSeconds(5),
            Xunit.TestContext.Current.CancellationToken);
        Assert.Equal("The app's handler failed.", failure.Message);
    }

    [Fact]
    public async Task Close_ShouldShowTheErrorBoundarysContent_WhenTheScriptStartsItAndLayoutChangedThrows()
    {
        var cut = Render(FailingInABoundary());

        await cut.InvokeAsync(() => Report().OnClose("map", true));

        Assert.Equal("The app's handler failed.", cut.Markup);
    }

    [Fact]
    public async Task Helpers_ShouldLeaveTheFailureToTheErrorBoundary_WhenOneIsAroundTheDock()
    {
        var cut = Render(FailingInABoundary());

        await cut.Tb().Dock().ResizeAsync("Resize Trails", 40);

        Assert.Equal("The app's handler failed.", cut.Markup);
    }

    [Fact]
    public async Task Helpers_ShouldThrowEachFailureOnce_WhenTwoCallsInOneTestFail()
    {
        var cut = RenderDock(Hike, HikePanels, Fail);
        var dock = cut.Tb().Dock();

        await Assert.ThrowsAsync<InvalidOperationException>(() => dock.ResizeAsync("Resize Trails", 40));
        await Assert.ThrowsAsync<InvalidOperationException>(() => dock.ResizeAsync("Resize Trails", 30));

        // bUnit throws a failure it still holds at the next render.
        cut.Render();
    }

    // A dock whose LayoutChanged throws, inside an error boundary that shows what it caught.
    private RenderFragment FailingInABoundary() => builder =>
    {
        builder.OpenComponent<ErrorBoundary>(0);
        builder.AddAttribute(1, nameof(ErrorBoundary.ChildContent), (RenderFragment)(inner =>
        {
            inner.OpenComponent<TbDockManager>(0);
            inner.AddAttribute(1, nameof(TbDockManager.Layout), Hike);
            inner.AddAttribute(2, nameof(TbDockManager.LayoutChanged),
                EventCallback.Factory.Create<DockLayout>(this, (Action<DockLayout>)Fail));
            inner.AddAttribute(3, nameof(TbDockManager.ChildContent), Panels(HikePanels));
            inner.CloseComponent();
        }));
        builder.AddAttribute(2, nameof(ErrorBoundary.ErrorContent), (RenderFragment<Exception>)(failure =>
            content => content.AddContent(0, failure.Message)));
        builder.CloseComponent();
    };

    [Fact]
    public async Task Helpers_ShouldThrowTheAppsFailureWhereTheTestCalled_WhenLayoutChangedThrowsAfterAScriptsCall()
    {
        var cut = RenderDock(Hike, HikePanels, Fail);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => cut.Tb().Dock().ResizeAsync("Resize Trails", 40));

        Assert.Equal("The app's handler failed.", failure.Message);
    }

    private static void Fail(DockLayout layout) => throw new InvalidOperationException("The app's handler failed.");

    private IRenderedComponent<TbDockManager> RenderPoppable(DockLayout layout, Action<DockLayout>? onChange = null,
        Panel[]? panels = null)
    {
        var cut = RenderDock(layout, panels ?? HikePanels, onChange);
        cut.Render(parameters => parameters.Add(dock => dock.AllowPopOut, true));
        return cut;
    }

    [Theory]
    [InlineData(TabPanelFocus.Auto, "0", null)]
    [InlineData(TabPanelFocus.Always, "0", "always")]
    [InlineData(TabPanelFocus.Never, "-1", "never")]
    public void Render_ShouldSetEachShowingPanelsStopAndMarkTheDock_WhenGivenEachFocusablePanelsChoice(
        TabPanelFocus stops, string tabindex, string? mark)
    {
        var cut = Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, Hike)
            .Add(dock => dock.FocusablePanels, stops).Add(dock => dock.ChildContent, Panels(HikePanels)));

        // The script decides which panels keep the stop, so a test here sees the one the server renders.
        Assert.All(cut.FindAll("[role=tabpanel]:not([inert])"),
            panel => Assert.Equal(tabindex, panel.GetAttribute("tabindex")));
        Assert.Equal(mark, cut.Find("[data-part=dock]").GetAttribute("data-focusable-panels"));
    }

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
            builder.AddAttribute(5, nameof(TbDockPanel.AllowPopOut), panel.AllowPopOut);
            builder.AddAttribute(6, nameof(TbDockPanel.KeepWindowFor), panel.KeepWindowFor);
            builder.AddAttribute(7, nameof(TbDockPanel.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<Probe>(0);
                content.AddAttribute(1, nameof(Probe.Name), panel.Id);
                content.AddAttribute(2, nameof(Probe.Log), _log);
                content.CloseComponent();
            }));
            builder.AddAttribute(8, nameof(TbDockPanel.Modified), panel.Modified);
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

    // The reference a tab gets when it is made anew in another row. bUnit shows it only in that render, and the dock
    // renders once more after an action, to show its count, so this keeps the last one seen.
    private static Func<string?> ReferenceOnceMade(IRenderedComponent<TbDockManager> cut, string title)
    {
        string? seen = null;
        cut.OnMarkupUpdated += (_, _) => seen = Reference(cut, title) is { Length: > 0 } made ? made : seen;
        return () => seen;
    }

    // The reference of the element a selector finds, which bUnit shows only in the render that makes the element.
    private static Func<string?> OnceMade(IRenderedComponent<TbDockManager> cut, string selector)
    {
        string? seen = null;
        cut.OnMarkupUpdated += (_, _) => seen = cut.FindAll(selector).FirstOrDefault()
            ?.GetAttribute("blazor:elementreference") is { Length: > 0 } made ? made : seen;
        return () => seen;
    }

    private static string Text(AngleSharp.Dom.IElement element) => element.TextContent.Trim();

    private static IEnumerable<string> Titles(AngleSharp.Dom.IElement group) =>
        group.QuerySelectorAll("[role=tab]").Select(tab => tab.TextContent.Trim());

    private static IEnumerable<string?> Parts(IRenderedComponent<TbDockManager> cut) =>
        cut.FindAll(OrderedParts).Select(part => part.GetAttribute("data-part"));

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
