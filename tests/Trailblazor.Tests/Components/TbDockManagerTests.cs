using System.Diagnostics.Metrics;
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

        await cut.InvokeAsync(() => Report().OnMove("map", "weather", "Top"));

        Assert.Equal([["Trails"], ["Notes"], ["Map"], ["Weather"]], cut.FindAll("[data-part=group]").Select(Titles));
        Assert.Equal(Hike.Move("map", "weather", DockPosition.Top), changed);
        Assert.Equal(["Map moved to panel group 3."], Services.GetRequiredService<FakeAnnouncer>().Polite);
        Assert.Equal(Reference(cut, "Map"), ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
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
        Assert.Equal(["back false", "forward true", "maximize false"], Items());
        await cut.Find(".tb-dock__tab-area").TriggerEventAsync("ontbcontextmenuopen",
            new ContextMenuOpenEventArgs { Key = "trails", Request = 2 });
        Assert.Equal(["back true", "forward false", "maximize false", "close false"], Items());
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
        }));

        var menu = cut.Find("[data-part=group-menu]");
        Assert.Equal("Move Trails", menu.QuerySelector("button")!.GetAttribute("aria-label"));
        Assert.Equal("For this tab", cut.Find(".tb-dock__tab-area > [role=menu]").GetAttribute("aria-label"));
        Assert.Equal(["Earlier", "Later", "Fill", "Shut"],
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
