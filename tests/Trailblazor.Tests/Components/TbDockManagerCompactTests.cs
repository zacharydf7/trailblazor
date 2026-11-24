using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Trailblazor.Testing;

namespace Trailblazor.Tests.Components;

public class TbDockManagerCompactTests : BunitContext
{
    // Trails and Map share a group beside the notes, which sit above the weather.
    private static readonly DockLayout Hike = new(new DockSplit(
        new DockGroup("trails", "map") { Size = 1 },
        new DockSplit(new DockGroup("notes") { Size = 3 }, new DockGroup("weather")) { Vertical = true, Size = 3 }));

    private static readonly (string Id, string Title)[] HikePanels =
        [("trails", "Trails"), ("map", "Map"), ("notes", "Notes"), ("weather", "Weather")];

    private readonly BunitJSModuleInterop _module;
    private readonly List<DockLayout> _changes = [];

    public TbDockManagerCompactTests()
    {
        this.AddTrailblazor();
        _module = JSInterop.SetupModule("./_content/Trailblazor/dock-manager.js");
        _module.SetupModule("attachDock", _ => true).Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public async Task Render_ShouldShowItsDockedPanelsAsTheTabsOfOneGroup_WhenToldItIsNarrow()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        Assert.False(dock.IsCompact);

        await dock.NarrowAsync();

        Assert.True(dock.IsCompact);
        var group = Assert.Single(cut.FindAll("[data-part=group]"));
        Assert.Equal("--tb-dock-x: 0; --tb-dock-y: 0; --tb-dock-w: 100; --tb-dock-h: 100;", group.GetAttribute("style"));
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], dock.Panels);
        Assert.Equal(["Trails"], dock.Showing);
        // The panels that showed in the other groups stay in the page, out of reach.
        Assert.Equal("trails", Assert.Single(cut.FindAll("[role=tabpanel]:not([inert])")).GetAttribute("data-panel"));
        Assert.Empty(cut.FindAll("[data-part=separator], [data-part=grip]"));
        Assert.Empty(_changes);
    }

    [Fact]
    public async Task Render_ShouldKeepItsGroups_WhenTheyAreOnlyAboveAndBelowEachOther()
    {
        var cut = RenderDock(new DockLayout(new DockSplit(new DockGroup("trails", "map"), new DockGroup("notes"))
        {
            Vertical = true,
        }));
        var dock = cut.Tb().Dock();

        await dock.NarrowAsync();

        Assert.False(dock.IsCompact);
        Assert.Equal(2, cut.FindAll("[data-part=group]").Count);
        Assert.Single(cut.FindAll("[data-part=separator]"));
    }

    [Fact]
    public async Task Render_ShouldKeepItsGroups_WhenToldItIsNarrowWithCompactBelowXs()
    {
        var cut = RenderDock(Hike, Breakpoint.Xs);
        var dock = cut.Tb().Dock();

        await dock.NarrowAsync();

        Assert.False(dock.IsCompact);
        Assert.Equal(3, cut.FindAll("[data-part=group]").Count);
    }

    [Theory]
    [InlineData(Breakpoint.Sm, "640")]
    [InlineData(Breakpoint.Lg, "1024")]
    [InlineData(Breakpoint.Xs, null)]
    public void Render_ShouldStateTheWidthItIsCompactBelow_WhenGivenCompactBelow(Breakpoint below, string? width)
    {
        var cut = RenderDock(Hike, below);

        Assert.Equal(width, cut.Find("[data-part=dock]").GetAttribute("data-compact-below"));
    }

    [Fact]
    public async Task Select_ShouldShowThePanelAndKeepTheLayout_WhenItsOwnGroupShowedItAlready()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();

        await dock.SelectAsync("Notes");

        Assert.Equal(["Notes"], dock.Showing);
        Assert.Empty(_changes);

        // Map wasn't its group's showing tab, so the layout takes it, as in a wide dock.
        await dock.SelectAsync("Map");

        Assert.Equal(["Map"], dock.Showing);
        Assert.Equal(Hike.Open("map"), Assert.Single(_changes));
    }

    [Fact]
    public async Task Narrow_ShouldShowEveryGroupAgain_WhenToldItIsAsWideAgain()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();
        await dock.SelectAsync("Map");

        await dock.NarrowAsync(false);

        Assert.False(dock.IsCompact);
        Assert.Equal(3, cut.FindAll("[data-part=group]").Count);
        Assert.Equal(["Map", "Notes", "Weather"], dock.Showing);
    }

    [Fact]
    public async Task Close_ShouldShowTheTabThatTakesItsPlace_WhenTheShowingPanelOfACompactDockCloses()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();
        await dock.SelectAsync("Map");

        // Map was the last tab of its own group, and Notes follows it in the one row.
        await dock.CloseAsync("Map");

        Assert.Equal(["Trails", "Notes", "Weather"], dock.Panels);
        Assert.Equal(["Notes"], dock.Showing);

        await dock.SelectAsync("Weather");
        await dock.CloseAsync("Weather");

        Assert.Equal(["Notes"], dock.Showing);
    }

    [Fact]
    public async Task Menu_ShouldOfferNoNewGroupAndNoMaximizing_WhenTheDockIsCompact()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        Assert.NotEmpty(cut.FindAll("[data-move-beside], [data-move-edge], [data-move=maximize]"));

        await dock.NarrowAsync();

        Assert.Empty(cut.FindAll("[data-move-beside], [data-move-edge], [data-move=maximize], [data-move-group]"));
        Assert.NotEmpty(cut.FindAll("[data-move=float]"));
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.MoveToEdgeAsync("Trails", DockPosition.Bottom));
        Assert.Contains("compact", refused.Message);
        refused = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dock.MoveToNewGroupAsync("Trails", DockPosition.End));
        Assert.Contains("compact", refused.Message);
    }

    [Fact]
    public async Task MoveTab_ShouldMoveATabPastTheNextGroupsFirstTab_WhenTheDockIsCompact()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();

        await dock.MoveTabAsync("Map", 1);

        Assert.Equal(["Trails", "Notes", "Map", "Weather"], dock.Panels);
        Assert.Equal(["Map"], dock.Showing);
    }

    [Fact]
    public async Task MoveTab_ShouldShowTheMovedPanel_WhenATabThatWasntShowingJoinsAnotherGroupOfACompactDock()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();

        // Trails shows. Map's own menu moves it past Notes, the first tab of the next group, which nobody can see is
        // another group.
        await dock.ChooseFromTabMenuAsync("Map", "Move tab forward");

        Assert.Equal(["Trails", "Notes", "Map", "Weather"], dock.Panels);
        Assert.Equal(["Map"], dock.Showing);
    }

    [Fact]
    public async Task MoveTab_ShouldShowTheMovedPanel_WhenItsOwnGroupShowedItBehindAnotherGroupsPanel()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();
        await dock.SelectAsync("Notes");

        // Trails is still its own group's showing tab, and stays in that group.
        await dock.ChooseFromTabMenuAsync("Trails", "Move tab forward");

        Assert.Equal(["Map", "Trails", "Notes", "Weather"], dock.Panels);
        Assert.Equal(["Trails"], dock.Showing);
    }

    [Fact]
    public async Task Layout_ShouldShowAPanelTheAppMovedToAnotherGroup_WhenTheDockIsCompact()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();
        await dock.SelectAsync("Notes");

        // Trails was its own group's showing tab, and now shows in front of Notes in the group it joined.
        cut.Render(parameters => parameters.Add(each => each.Layout,
            Hike.Open("notes").Move("trails", "notes", DockPosition.After)));

        Assert.Equal(["Trails"], dock.Showing);
    }

    [Fact]
    public async Task Close_ShouldShowTheNextTabOfTheRow_WhenTheClosedPanelsOwnGroupHasTabsLeft()
    {
        var cut = RenderDock(new DockLayout(new DockSplit(
            new DockGroup("trails", "map", "weather") { Active = "weather" }, new DockGroup("notes"))));
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();
        Assert.Equal(["Weather"], dock.Showing);

        // Notes follows Weather in the row, though Trails and Map are the tabs left in Weather's own group.
        await dock.CloseAsync("Weather");

        Assert.Equal(["Trails", "Map", "Notes"], dock.Panels);
        Assert.Equal(["Notes"], dock.Showing);
    }

    [Fact]
    public async Task Layout_ShouldBringThePanelItNewlyShowsToTheFront_WhenTheAppSetsOneOnACompactDock()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();
        await dock.SelectAsync("Notes");

        cut.Render(parameters => parameters.Add(each => each.Layout, Hike.Open("map")));

        Assert.Equal(["Map"], dock.Showing);
    }

    [Fact]
    public async Task ShowAsync_ShouldShowADockedPanel_WhenItsGroupShowedItAlreadyOrNot()
    {
        var cut = RenderDock(Hike);
        var dock = cut.Tb().Dock();
        await dock.NarrowAsync();

        await cut.Instance.ShowAsync("weather");

        Assert.Equal(["Weather"], dock.Showing);
        Assert.Empty(_changes);

        await cut.Instance.ShowAsync("map");

        Assert.Equal(["Map"], dock.Showing);
        Assert.Equal(Hike.Open("map"), Assert.Single(_changes));

        // A panel the dock doesn't hold changes nothing.
        await cut.Instance.ShowAsync("photos");

        Assert.Equal(["Map"], dock.Showing);
        Assert.Single(_changes);
    }

    [Fact]
    public async Task ShowAsync_ShouldShowEveryGroupAgain_WhenThePanelIsTheShowingTabOfACoveredGroup()
    {
        var filled = Hike.Maximize("trails");
        var cut = RenderDock(filled);

        await cut.Instance.ShowAsync("notes");

        Assert.Equal(filled.Open("notes"), Assert.Single(_changes));
        Assert.Equal(["Trails", "Notes", "Weather"], cut.Tb().Dock().Showing);
    }

    [Fact]
    public async Task ShowAsync_ShouldBringATabToTheFrontOfItsGroup_WhenTheDockIsWide()
    {
        var cut = RenderDock(Hike);

        await cut.Instance.ShowAsync("map");

        Assert.Equal(["Map", "Notes", "Weather"], cut.Tb().Dock().Showing);
        Assert.Equal(Hike.Open("map"), Assert.Single(_changes));
    }

    [Fact]
    public async Task Narrow_ShouldKeepThePanelWhoseTabHoldsTheFocusInFrontAndFocusItsTab_WhenTheScriptNamesIt()
    {
        var cut = RenderDock(Hike);
        var report = ((DotNetObjectReference<DockReport>)_module.Invocations["attachDock"].Single().Arguments[1]!)
            .Value;

        await cut.InvokeAsync(() => report.OnNarrow(true, "weather", true));

        Assert.Equal(["Weather"], cut.Tb().Dock().Showing);
        var weather = cut.FindAll("[role=tab]").Single(tab => tab.GetAttribute("data-panel") == "weather");
        Assert.Equal(weather.GetAttribute("blazor:elementreference"),
            ((ElementReference)JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!).Id);
    }

    private IRenderedComponent<TbDockManager> RenderDock(DockLayout layout, Breakpoint below = Breakpoint.Sm) =>
        Render<TbDockManager>(parameters => parameters.Add(dock => dock.Layout, layout)
            .Add(dock => dock.CompactBelow, below).Add(dock => dock.LayoutChanged, _changes.Add)
            .Add(dock => dock.ChildContent, builder =>
            {
                foreach (var (id, title) in HikePanels)
                {
                    builder.OpenComponent<TbDockPanel>(0);
                    builder.SetKey(id);
                    builder.AddAttribute(1, nameof(TbDockPanel.Id), id);
                    builder.AddAttribute(2, nameof(TbDockPanel.Title), title);
                    builder.AddAttribute(3, nameof(TbDockPanel.ChildContent), (RenderFragment)(content =>
                        content.AddContent(0, title)));
                    builder.CloseComponent();
                }
            }));
}
