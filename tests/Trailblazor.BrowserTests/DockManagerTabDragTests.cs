using System.Globalization;
using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Dragging holds the pointer, which Firefox can leak into parallel pages, so it runs with the other drag tests.
[Collection(nameof(PointerDrags))]
public class DockManagerTabDragTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveAPanelBesideATab_WhenItsTabIsDroppedOnAnotherGroupsRow(string browser)
    {
        var page = await OpenDockAsync(browser);
        var notes = (await Tab(page, "Notes").BoundingBoxAsync())!;

        // Past the middle of Notes, so the map's tab would go between Notes and Gear.
        await CarryAsync(page, "Map", notes.X + notes.Width * 0.75f, notes.Y + notes.Height / 2);

        await ExpectMarkAsync(page, notes.X + notes.Width - 1, notes.Y, 2, notes.Height);
        await Expect(Tab(page, "Map")).ToHaveAttributeAsync("data-dragging", "");
        // Nothing changes until the drop.
        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());

        await DropAsync(page);

        await Expect(Tab(page, "Map")).ToBeFocusedAsync();
        Assert.Equal(["Trails", "Notes", "Map", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());
        Assert.Equal(["Trails", "Map", "Weather"], await page.Tb().Dock().ShowingAsync());
        await Expect(Mark(page)).ToBeHiddenAsync();
        await Expect(Tab(page, "Map")).Not.ToHaveAttributeAsync("data-dragging", "");
        await page.Tb().Announcements.ExpectAnnouncedAsync("Map moved to panel group 2.");
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("[\"notes\",\"map\",\"gear\"]");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveATabAlongItsRowAndLeaveItAlone_WhenItIsDroppedPastANeighborOrOnItsOwnPlace(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Locator(".tb-dock");
        var map = (await Tab(page, "Map").BoundingBoxAsync())!;

        await CarryAsync(page, "Trails", map.X + map.Width * 0.75f, map.Y + map.Height / 2);
        await DropAsync(page);
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
        Assert.Equal(["Map", "Trails", "Notes", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());

        // A few pixels along its own tab is still the map's own place: no mark, no move, and the release doesn't
        // select the tab as a click would.
        var commits = await dock.GetAttributeAsync("data-commits");
        map = (await Tab(page, "Map").BoundingBoxAsync())!;
        await CarryAsync(page, "Map", map.X + map.Width / 2 + 8, map.Y + map.Height / 2);
        await Expect(Tab(page, "Map")).ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeHiddenAsync();
        await page.Mouse.UpAsync();

        await Expect(Tab(page, "Map")).Not.ToHaveAttributeAsync("data-dragging", "");
        // The middle of its own group is where the map already is, too.
        var own = (await Panel(page, "Trails").BoundingBoxAsync())!;
        await CarryAsync(page, "Map", own.X + own.Width / 2, own.Y + own.Height / 2);
        await Expect(Tab(page, "Map")).ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeHiddenAsync();
        await page.Mouse.UpAsync();

        Assert.Equal(["Map", "Trails", "Notes", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());
        Assert.Equal(["Trails", "Notes", "Weather"], await page.Tb().Dock().ShowingAsync());
        Assert.Equal(commits, await dock.GetAttributeAsync("data-commits"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldPutATabFirstInARow_WhenItIsDroppedBeforeTheMiddleOfTheFirstTab(string browser)
    {
        var page = await OpenDockAsync(browser);
        var notes = (await Tab(page, "Notes").BoundingBoxAsync())!;

        await CarryAsync(page, "Map", notes.X + notes.Width * 0.25f, notes.Y + notes.Height / 2);
        await ExpectMarkAsync(page, notes.X - 1, notes.Y, 2, notes.Height);
        await DropAsync(page);

        await Expect(page.GetByTestId("layout")).ToContainTextAsync("[\"map\",\"notes\",\"gear\"]");
        Assert.Equal(["Trails", "Map", "Weather"], await page.Tb().Dock().ShowingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldSplitATabOffItsOwnGroup_WhenItIsDroppedOnASideOfItsOwnPanel(string browser)
    {
        var page = await OpenDockAsync(browser);
        var own = (await Panel(page, "Trails").BoundingBoxAsync())!;
        var row = (await page.Locator(".tb-dock__group").First.BoundingBoxAsync())!;

        // Trails is its group's showing tab, so the new group goes above the map, the tab it leaves behind.
        await CarryAsync(page, "Trails", own.X + own.Width / 2, own.Y + own.Height * 0.1f);
        var half = (own.Y + own.Height - row.Y) / 2;
        await ExpectMarkAsync(page, own.X, row.Y, own.Width, half);
        await DropAsync(page);

        await Expect(page.Locator(".tb-dock__group")).ToHaveCountAsync(4);
        Assert.Equal(["Trails", "Map", "Notes", "Weather"], await page.Tb().Dock().ShowingAsync());
        var trails = (await Panel(page, "Trails").BoundingBoxAsync())!;
        var map = (await Panel(page, "Map").BoundingBoxAsync())!;
        Assert.Equal(own.X, map.X, 1.0);
        Assert.True(trails.Y < map.Y, $"{trails.Y} above {map.Y}");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldSelectATab_WhenAFingerRestsOnItForAMomentBeforeLifting(string browser)
    {
        var page = await OpenDockAsync(browser);
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");

        // A slow tap: the finger holds still past the moment that picks the tab up, drifts by a pixel or two as a
        // resting finger does, then lifts. It rests near the tab's top, within the band along the dock's edge, where
        // a drop would move the panel.
        var marks = await Tab(page, "Map").EvaluateAsync<bool[]>("""
            async tab => {
                const box = tab.getBoundingClientRect();
                const fire = (type, drift) => tab.dispatchEvent(new PointerEvent(type, { bubbles: true, pointerId: 7,
                    pointerType: 'touch', isPrimary: true, button: type === 'pointermove' ? -1 : 0,
                    clientX: box.left + box.width / 2 + drift, clientY: box.top + 3 }));
                fire('pointerdown', 0);
                await new Promise(resolve => setTimeout(resolve, 400));
                const carried = tab.hasAttribute('data-dragging');
                fire('pointermove', 2);
                const landing = document.querySelector('.tb-dock__drop').hasAttribute('data-show');
                fire('pointerup', 2);
                tab.click();
                return [carried, landing];
            }
            """);

        // The hold picked the tab up, and the drift showed nowhere to land.
        Assert.Equal([true, false], marks);
        await Expect(Tab(page, "Map")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Tab(page, "Map")).Not.ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeHiddenAsync();
        // The tap selected the tab and nothing more: the one commit is the selection, and no panel moved.
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-commits", "1");
        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldDrawTheMarkInTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenDockAsync(browser, options: new() { ForcedColors = ForcedColors.Active });
        var highlight = await SystemColorAsync(page, "Highlight");
        var notes = (await Tab(page, "Notes").BoundingBoxAsync())!;
        var weather = (await Panel(page, "Weather").BoundingBoxAsync())!;

        await CarryAsync(page, "Map", notes.X + notes.Width * 0.75f, notes.Y + notes.Height / 2);
        await Expect(Mark(page)).ToHaveAttributeAsync("data-show", "line");
        await Expect(Mark(page)).ToHaveCSSAsync("background-color", highlight);

        // Over a group the mark is an outline with nothing filled in.
        await page.Mouse.MoveAsync(weather.X + weather.Width / 2, weather.Y + weather.Height / 2, new() { Steps = 4 });
        await Expect(Mark(page)).ToHaveAttributeAsync("data-show", "box");
        await Expect(Mark(page)).ToHaveCSSAsync("border-top-color", highlight);
        await Expect(Mark(page)).ToHaveCSSAsync("background-color", new Regex(@", 0\)$"));
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldShowNoMark_WhenTheTabOfItsOnlyPanelIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Only notes" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tablist)).ToHaveCountAsync(1);
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        // With nothing else docked, the panel has nowhere else to go, the dock's edge included.
        await CarryAsync(page, "Notes", dock.X + dock.Width - 5, dock.Y + dock.Height / 2);

        await Expect(Tab(page, "Notes")).ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeHiddenAsync();
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldGiveAPanelANewGroup_WhenItsTabIsDroppedOnASideOfAGroupOrOfTheDock(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var weather = (await Panel(page, "Weather").BoundingBoxAsync())!;
        var row = (await page.Locator(".tb-dock__group").Nth(2).BoundingBoxAsync())!;

        // The lower quarter of the weather's panel: the gear would take the lower half of that group's box.
        await CarryAsync(page, "Gear", weather.X + weather.Width / 2, weather.Y + weather.Height * 0.8f);
        var half = (weather.Y + weather.Height - row.Y) / 2;
        await ExpectMarkAsync(page, weather.X, row.Y + half, weather.Width, half);
        await DropAsync(page);

        await Expect(page.GetByRole(AriaRole.Tablist)).ToHaveCountAsync(4);
        Assert.Equal(["Trails", "Map", "Notes", "Weather", "Gear"], await page.Tb().Dock().PanelsAsync());
        var gear = (await Panel(page, "Gear").BoundingBoxAsync())!;
        Assert.True(gear.Y > (await Panel(page, "Weather").BoundingBoxAsync())!.Y);

        // Within a few pixels of the dock's end edge: a quarter of the dock along that edge.
        await CarryAsync(page, "Trails", dock.X + dock.Width - 5, dock.Y + dock.Height / 2);
        await ExpectMarkAsync(page, dock.X + dock.Width * 0.75, dock.Y, dock.Width / 4, dock.Height);
        await DropAsync(page);

        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
        Assert.Equal(["Map", "Notes", "Weather", "Gear", "Trails"], await page.Tb().Dock().PanelsAsync());
        var trails = (await Panel(page, "Trails").BoundingBoxAsync())!;
        Assert.Equal(dock.X + 1 + 0.75 * 766, trails.X, 1.0);
        Assert.Equal(382 - (await page.Locator(".tb-dock__group").Last.BoundingBoxAsync())!.Height, trails.Height, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLandAlongTheDocksEdge_WhenATabIsDroppedOnARowWithinAFewPixelsOfThatEdge(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var notes = (await Tab(page, "Notes").BoundingBoxAsync())!;

        // On the Notes tab, but within a few pixels of the dock's top edge, and the edge wins.
        await CarryAsync(page, "Gear", notes.X + notes.Width / 2, dock.Y + 4);
        await ExpectMarkAsync(page, dock.X, dock.Y, dock.Width, dock.Height / 4);
        await DropAsync(page);

        Assert.Equal(["Gear", "Trails", "Map", "Notes", "Weather"], await page.Tb().Dock().PanelsAsync());
        Assert.Equal(766, (await Panel(page, "Gear").BoundingBoxAsync())!.Width, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldTakeAPanelAsAGroupsLastTab_WhenItsTabIsDroppedOnTheMiddleOfThatGroupOverAnIframe(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var weather = (await Panel(page, "Weather").BoundingBoxAsync())!;
        var frame = (await page.Locator("iframe[title=Forecast]").BoundingBoxAsync())!;
        var row = (await page.Locator(".tb-dock__group").Nth(2).BoundingBoxAsync())!;
        // Just past the panel's upper quarter, which is still on the iframe.
        var (x, y) = (frame.X + frame.Width * 0.8f, weather.Y + weather.Height * 0.27f);
        Assert.InRange(y, frame.Y, frame.Y + frame.Height);

        await CarryAsync(page, "Trails", x, y);

        // The panels let the pointer through while a tab is carried, or the iframe would take it.
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-moving", "");
        await Expect(Panel(page, "Weather")).ToHaveCSSAsync("pointer-events", "none");
        await ExpectMarkAsync(page, weather.X, row.Y, weather.Width, weather.Y + weather.Height - row.Y);
        await DropAsync(page);

        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
        Assert.Equal(["Map", "Notes", "Gear", "Weather", "Trails"], await page.Tb().Dock().PanelsAsync());
        Assert.Equal(["Map", "Notes", "Trails"], await page.Tb().Dock().ShowingAsync());
        await Expect(page.Locator(".tb-dock")).Not.ToHaveAttributeAsync("data-moving", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveThePanelWhereItWas_WhenTheDragIsCanceledOrEndsOutsideTheDock(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Locator(".tb-dock");
        var box = (await dock.BoundingBoxAsync())!;
        var notes = (await Tab(page, "Notes").BoundingBoxAsync())!;
        var commits = int.Parse((await dock.GetAttributeAsync("data-commits"))!);

        await CarryAsync(page, "Map", notes.X + notes.Width * 0.75f, notes.Y + notes.Height / 2);
        await Expect(Mark(page)).ToBeVisibleAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(Mark(page)).ToBeHiddenAsync();
        await Expect(Tab(page, "Map")).Not.ToHaveAttributeAsync("data-dragging", "");
        await page.Mouse.UpAsync();

        // Below the dock there is nowhere to land.
        await CarryAsync(page, "Map", box.X + box.Width / 2, box.Y + box.Height + 30);
        await Expect(Mark(page)).ToBeHiddenAsync();
        await page.Mouse.UpAsync();

        await page.Tb().Dock().SelectAsync("Gear");
        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());
        // Only the selection that followed counted: neither drag moved nor selected anything.
        Assert.Equal(["Trails", "Gear", "Weather"], await page.Tb().Dock().ShowingAsync());
        Assert.Equal(commits + 1, int.Parse((await dock.GetAttributeAsync("data-commits"))!));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldPutTheNewGroupWhereThePointerIs_WhenATabIsDroppedInRightToLeftText(string browser)
    {
        var page = await OpenDockAsync(browser, "?direction=rtl");
        var weather = (await Panel(page, "Weather").BoundingBoxAsync())!;
        var row = (await page.Locator(".tb-dock__group").Nth(2).BoundingBoxAsync())!;

        // The left quarter of the weather's panel, which is where text ends.
        await CarryAsync(page, "Gear", weather.X + weather.Width * 0.1f, weather.Y + weather.Height / 2);
        await ExpectMarkAsync(page, weather.X, row.Y, weather.Width / 2, weather.Y + weather.Height - row.Y);
        await DropAsync(page);

        await Expect(page.GetByRole(AriaRole.Tablist)).ToHaveCountAsync(4);
        Assert.Equal(["Trails", "Map", "Notes", "Weather", "Gear"], await page.Tb().Dock().PanelsAsync());
        Assert.Equal(weather.X, (await Panel(page, "Gear").BoundingBoxAsync())!.X, 1.0);
        Assert.Equal(weather.X + weather.Width / 2, (await Panel(page, "Weather").BoundingBoxAsync())!.X, 2.0);

        // Along a row, the left of a tab is past its middle, so the map goes after Notes, with the line at its left.
        var notes = (await Tab(page, "Notes").BoundingBoxAsync())!;
        await CarryAsync(page, "Map", notes.X + notes.Width * 0.25f, notes.Y + notes.Height / 2);
        await ExpectMarkAsync(page, notes.X - 1, notes.Y, 2, notes.Height);
        await DropAsync(page);

        Assert.Equal(["Trails", "Notes", "Map", "Weather", "Gear"], await page.Tb().Dock().PanelsAsync());
    }

    // Only Chromium can drive touch from Playwright.
    [Fact]
    public async Task Dock_ShouldMoveAPanelAfterAHoldAndLeaveASwipeAlone_WhenAFingerDragsItsTab()
    {
        var page = await OpenDockAsync("chromium", options: new() { HasTouch = true });
        var dock = page.Locator(".tb-dock");
        await Expect(dock).ToHaveAttributeAsync("data-ready", "");
        var map = (await Tab(page, "Map").BoundingBoxAsync())!;
        var notes = (await Tab(page, "Notes").BoundingBoxAsync())!;
        var (x, y) = (map.X + map.Width / 2, map.Y + map.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);

        // A finger that moves at once is a swipe, which scrolls a row of tabs and carries nothing.
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", x + 30, y);
        await Expect(Tab(page, "Map")).Not.ToHaveAttributeAsync("data-dragging", "");
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        // A finger that holds still for a moment picks the tab up. The page reports the pickup itself: if the finger
        // then rests for half a second, the tab is put down again, and on a slow machine polling for the attribute
        // can take that long.
        var commits = int.Parse((await dock.GetAttributeAsync("data-commits"))!, CultureInfo.InvariantCulture);
        await Tab(page, "Map").EvaluateAsync("""
            tab => {
                window.pickedUp = new Promise(resolve => new MutationObserver(() => {
                    if (tab.hasAttribute('data-dragging')) {
                        resolve();
                    }
                }).observe(tab, { attributes: true, attributeFilter: ['data-dragging'] }));
            }
            """);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await page.EvaluateAsync("() => window.pickedUp");
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", (x + notes.X) / 2, y);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", notes.X + notes.Width * 0.75, y);
        await ExpectMarkAsync(page, notes.X + notes.Width - 1, notes.Y, 2, notes.Height);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        await Expect(dock).ToHaveAttributeAsync("data-commits", (commits + 1).ToString(CultureInfo.InvariantCulture));
        Assert.Equal(["Trails", "Notes", "Map", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());
    }

    private static ILocator Mark(IPage page) => page.Locator(".tb-dock__drop");

    // The box the mark covers, within a pixel and a half.
    private static async Task ExpectMarkAsync(IPage page, double x, double y, double width, double height)
    {
        await Expect(Mark(page)).ToBeVisibleAsync();
        var mark = (await Mark(page).BoundingBoxAsync())!;
        Assert.Equal(x, mark.X, 1.5);
        Assert.Equal(y, mark.Y, 1.5);
        Assert.Equal(width, mark.Width, 1.5);
        Assert.Equal(height, mark.Height, 1.5);
    }

    // Lets go of the carried tab and waits until the app has the new layout.
    private static async Task DropAsync(IPage page)
    {
        var dock = page.Locator(".tb-dock");
        var commits = int.Parse((await dock.GetAttributeAsync("data-commits"))!, CultureInfo.InvariantCulture);
        await page.Mouse.UpAsync();
        await Expect(dock).ToHaveAttributeAsync("data-commits",
            (commits + 1).ToString(CultureInfo.InvariantCulture));
    }

    // Presses on a tab and carries it to a point, without letting go.
    private static async Task CarryAsync(IPage page, string tab, float x, float y)
    {
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");
        var box = (await Tab(page, tab).BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, y, new() { Steps = 6 });
    }
}
