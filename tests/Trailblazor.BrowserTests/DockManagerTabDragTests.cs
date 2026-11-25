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
    public async Task Dock_ShouldLeaveThePanelsWhereTheyAre_WhenATabIsDraggedInALockedDock(string browser)
    {
        var page = await OpenDockAsync(browser, "?locked=true");
        var commits = await page.Locator(".tb-dock").GetAttributeAsync("data-commits");
        var notes = (await Tab(page, "Notes").BoundingBoxAsync())!;

        await CarryAsync(page, "Map", notes.X + notes.Width * 0.75f, notes.Y + notes.Height / 2);
        await Expect(Mark(page)).ToBeHiddenAsync();
        await Expect(Tab(page, "Map")).Not.ToHaveAttributeAsync("data-dragging", "");
        await page.Mouse.UpAsync();

        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());
        Assert.Equal(commits, await page.Locator(".tb-dock").GetAttributeAsync("data-commits"));
        Assert.True(await page.Tb().Dock().IsLockedAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => page.Tb().Dock().FloatAsync("Map"));
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
    public async Task Dock_ShouldOfferAWindowAndNoDockedPlace_WhenTheTabOfItsOnlyPanelIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Only notes" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Tablist)).ToHaveCountAsync(1);
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        // On the dock's border, outside its one group, there is nowhere to land: Notes is docked, so the empty area
        // takes no drop, and the edge would only give it the group it has. A fifth of a pixel in stays on the
        // border however a browser rounds it.
        await CarryAsync(page, "Notes", dock.X + 0.2f, dock.Y + dock.Height / 2);
        await Expect(Tab(page, "Notes")).ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeHiddenAsync();

        // With nothing else docked, the panel has no other docked place to go, the dock's edge included. The mark is
        // the window a drop would float it in, against the edge the pointer is at.
        await page.Mouse.MoveAsync(dock.X + dock.Width - 5, dock.Y + dock.Height / 2, new() { Steps = 6 });

        await ExpectMarkAsync(page, dock.X + 1 + 383, dock.Y + dock.Height / 2 - 16, 383, 191);
        await DropAsync(page);
        Assert.Equal(["Notes"], await page.Tb().Dock().FloatingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldFloatAPanelWhereItsTabIsDropped_WhenTheTabIsDroppedOnTheMiddleOfItsOwnGroup(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var root = page.Locator(".tb-dock");
        var dock = (await root.BoundingBoxAsync())!;
        var own = (await Panel(page, "Notes").BoundingBoxAsync())!;
        var (x, y) = (own.X + own.Width * 0.3f, own.Y + own.Height / 2);

        await CarryAsync(page, "Gear", x, y);

        // The mark is the window: half as wide and as tall as the dock, with the pointer 48 pixels in from where its
        // row starts and 16 down.
        await ExpectMarkAsync(page, x - 48, y - 16, 383, 191);
        await DropAsync(page);

        // The dock is 766 by 382 pixels inside its border.
        var window = await page.Tb().Dock().WindowAsync("Gear");
        Assert.Equal((x - 48 - dock.X - 1) / 7.66, window.X, 0.2);
        Assert.Equal((y - 16 - dock.Y - 1) / 3.82, window.Y, 0.2);
        Assert.Equal((50, 50), (window.Width, window.Height));
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        await Expect(Mark(page)).ToBeHiddenAsync();
        await page.Tb().Announcements.ExpectAnnouncedAsync("Gear floats in a window.");
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"floating\":[{\"panels\":[\"gear\"]");

        // Alone in its window, Gear is where a drop on that window would put it.
        var body = (await Panel(page, "Gear").BoundingBoxAsync())!;
        var commits = await root.GetAttributeAsync("data-commits");
        await CarryAsync(page, "Gear", body.X + body.Width / 2, body.Y + body.Height / 2);
        await Expect(Tab(page, "Gear")).ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeHiddenAsync();
        await page.Mouse.UpAsync();
        await Expect(Tab(page, "Gear")).Not.ToHaveAttributeAsync("data-dragging", "");
        Assert.Equal(commits, await root.GetAttributeAsync("data-commits"));

        // With the notes beside it in the window, the same drop gives Gear a window of its own.
        await page.Tb().Dock().MoveToGroupAsync("Notes", 4);
        var windows = page.Locator(".tb-dock__group--floating");
        await Expect(windows).ToHaveCountAsync(1);
        var row = (await windows.BoundingBoxAsync())!;
        await CarryAsync(page, "Gear", row.X + row.Width / 2, row.Y + row.Height + 60);
        await Expect(Mark(page)).ToBeVisibleAsync();
        await DropAsync(page);

        await Expect(windows).ToHaveCountAsync(2);
        Assert.Equal(["Notes", "Gear"], await page.Tb().Dock().FloatingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldKeepTheWindowInsideTheDock_WhenATabAloneInItsGroupIsDroppedOnASideOfItsPanel(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var weather = (await Panel(page, "Weather").BoundingBoxAsync())!;

        // Within a few pixels of the dock's edge, a drop still gives the weather a group along that edge.
        await CarryAsync(page, "Weather", weather.X + weather.Width - 3, weather.Y + weather.Height / 2);
        await ExpectMarkAsync(page, dock.X + dock.Width * 0.75, dock.Y, dock.Width / 4, dock.Height);

        // Alone in its group, the weather's tab has no tabs to split off from, so a side of its panel floats it too.
        // The panel is in the dock's bottom-right corner, where a window half the dock's size doesn't fit under the
        // pointer.
        await page.Mouse.MoveAsync(weather.X + weather.Width * 0.9f, weather.Y + weather.Height / 2,
            new() { Steps = 4 });

        await ExpectMarkAsync(page, dock.X + 1 + 383, dock.Y + 1 + 191, 383, 191);
        await DropAsync(page);
        Assert.Equal((50, 50, 50, 50), await page.Tb().Dock().WindowAsync("Weather"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldFloatAPanelUnderThePointer_WhenItsTabIsPulledOutInRightToLeftText(string browser)
    {
        var page = await OpenDockAsync(browser, "?direction=rtl");
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var own = (await Panel(page, "Notes").BoundingBoxAsync())!;
        var (x, y) = (own.X + own.Width * 0.7f, own.Y + own.Height / 2);

        await CarryAsync(page, "Gear", x, y);

        // A row starts at the right here, so the window reaches left from 48 pixels right of the pointer.
        await ExpectMarkAsync(page, x + 48 - 383, y - 16, 383, 191);
        await DropAsync(page);

        // Its box counts from the right, where text starts.
        Assert.Equal((dock.X + 1 + 766 - x - 48) / 7.66, (await page.Tb().Dock().WindowAsync("Gear")).X, 0.2);
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
        // The swipe made its group's panel active, which the page binds; waiting keeps that render out of the hold.
        await Expect(dock).ToHaveAttributeAsync("data-active-panel", "trails");

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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldShowEveryGroupAgain_WhenATabOfTheGroupThatFillsTheDockIsDroppedOnItsSide(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.Tb().Dock().MaximizeAsync("Gear");
        var row = (await page.Locator(".tb-dock__group[data-maximized]").BoundingBoxAsync())!;
        var panel = (await Panel(page, "Gear").BoundingBoxAsync())!;

        // The side of the group where text ends, clear of the dock's own edge.
        await CarryAsync(page, "Gear", panel.X + panel.Width - 20, panel.Y + panel.Height / 2);

        await ExpectMarkAsync(page, row.X + row.Width / 2, row.Y, row.Width / 2, panel.Y + panel.Height - row.Y);

        await DropAsync(page);

        // Gear has a group of its own beside the notes, so nothing fills the dock anymore.
        await Expect(page.Locator(".tb-dock__group[data-maximized]")).ToHaveCountAsync(0);
        Assert.Equal(["Trails", "Map", "Notes", "Gear", "Weather"], await page.Tb().Dock().PanelsAsync());
        Assert.Equal(["Trails", "Notes", "Gear", "Weather"], await page.Tb().Dock().ShowingAsync());
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldJoinAWindowOrLeaveIt_WhenATabIsDroppedOnAWindowOrOnADockedGroup(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Gear");
        var row = (await page.Locator(".tb-dock__group--floating").BoundingBoxAsync())!;
        var body = (await Panel(page, "Gear").BoundingBoxAsync())!;

        // A window has no sides to split off: anywhere on it, the mark covers all of it and the panel joins its tabs.
        await CarryAsync(page, "Trails", body.X + 10, body.Y + body.Height / 2);
        await ExpectMarkAsync(page, row.X, row.Y, row.Width, body.Y + body.Height - row.Y);
        await DropAsync(page);

        Assert.Equal(["Gear", "Trails"], await dock.FloatingAsync());
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();

        // A floating panel's tab dropped on the middle of a docked group docks there, as its last tab.
        var weather = (await Panel(page, "Weather").BoundingBoxAsync())!;
        await CarryAsync(page, "Gear", weather.X + weather.Width / 2, weather.Y + weather.Height / 2);
        await DropAsync(page);

        Assert.Equal(["Trails"], await dock.FloatingAsync());
        Assert.Equal(["Map", "Notes", "Weather", "Gear", "Trails"], await dock.PanelsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldJoinAWindow_WhenATabIsDroppedOnOneThatReachesTheDocksEdge(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=corner");
        var whole = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var row = (await page.Locator(".tb-dock__group--floating").BoundingBoxAsync())!;
        var body = (await Panel(page, "Gear").BoundingBoxAsync())!;

        // Within the few pixels of the dock's edge that would give the panel a group along it, but on the window.
        await CarryAsync(page, "Weather", whole.X + 4, body.Y + body.Height / 2);

        await ExpectMarkAsync(page, row.X, row.Y, row.Width, body.Y + body.Height - row.Y);
        await DropAsync(page);
        Assert.Equal(["Gear", "Weather"], await page.Tb().Dock().FloatingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveAWindowWhereItIs_WhenASeparatorUnderItIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.Tb().Dock().FloatAsync("Gear");
        var window = page.Locator(".tb-dock__group--floating");
        var before = (await window.BoundingBoxAsync())!;
        var whole = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        // The dock's first separator, after the trails, which the page's splitter shares its name with.
        var line = (await page.Locator(".tb-dock > .tb-dock__separator").First.BoundingBoxAsync())!;

        // It runs down the dock and under the window's edge, and is pressed above the window.
        await page.Mouse.MoveAsync(line.X + line.Width / 2, whole.Y + 20);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(line.X + line.Width / 2 + 60, whole.Y + 20, new() { Steps = 4 });

        // The docked groups follow the pointer at once, and the window keeps its box meanwhile.
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-resizing", "");
        var during = (await window.BoundingBoxAsync())!;
        Assert.Equal(before.X, during.X, 0.5);
        Assert.Equal(before.Width, during.Width, 0.5);
        await DropAsync(page);
        Assert.Equal(before.X, (await window.BoundingBoxAsync())!.X, 0.5);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldOfferAWindowAtTheDocksEdge_WhenTheOnlyDockedPanelIsDraggedBesideAWindow(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=lone");
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var window = (await Panel(page, "Gear").BoundingBoxAsync())!;

        // An edge would give the notes the group they have, so it is no place to dock. The mark is the window a drop
        // would float the notes in, in the dock's bottom-left corner.
        await CarryAsync(page, "Notes", dock.X + 4, dock.Y + dock.Height - 40);

        await Expect(Tab(page, "Notes")).ToHaveAttributeAsync("data-dragging", "");
        await ExpectMarkAsync(page, dock.X + 1, dock.Y + 1 + 191, 383, 191);

        // The window is a place to land, though.
        await page.Mouse.MoveAsync(window.X + window.Width / 2, window.Y + window.Height / 2, new() { Steps = 4 });
        await Expect(Mark(page)).ToBeVisibleAsync();
        await DropAsync(page);
        Assert.Equal(["Gear", "Notes"], await page.Tb().Dock().FloatingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldDockAPanelAlongTheDocksEdge_WhenTheTabOfTheOnlyWindowIsDroppedThere(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=adrift");
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        // Nothing is docked, so the edge gives Gear a group that takes the whole dock, as the window's menu would.
        await CarryAsync(page, "Gear", dock.X + dock.Width - 5, dock.Y + dock.Height - 40);

        await ExpectMarkAsync(page, dock.X, dock.Y, dock.Width, dock.Height);
        await DropAsync(page);
        Assert.Empty(await page.Tb().Dock().FloatingAsync());
        Assert.Equal(["Gear"], await page.Tb().Dock().ShowingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldGiveAPanelTheWholeDock_WhenTheTabOfTheOnlyWindowIsDroppedOnTheDocksEmptyArea(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=adrift");
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var window = (await Panel(page, "Gear").BoundingBoxAsync())!;

        // Over its own window the tab has nowhere to go, as when something is docked.
        await CarryAsync(page, "Gear", window.X + window.Width / 2, window.Y + window.Height / 2);
        await Expect(Tab(page, "Gear")).ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeHiddenAsync();

        // Nothing is docked, so the empty area beside the window takes the panel, far from the dock's edges too.
        await page.Mouse.MoveAsync(dock.X + dock.Width * 0.25f, dock.Y + dock.Height * 0.75f, new() { Steps = 6 });

        await ExpectMarkAsync(page, dock.X, dock.Y, dock.Width, dock.Height);
        await DropAsync(page);
        Assert.Empty(await page.Tb().Dock().FloatingAsync());
        Assert.Equal(["Gear"], await page.Tb().Dock().ShowingAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldPutAPanelAwayOnAStrip_WhenItsTabIsDroppedOnTheStrip(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        var start = Strip(page, "start");
        var box = (await start.BoundingBoxAsync())!;

        // Over a strip, the strip itself is marked, since the dock's mark can't leave the dock's area.
        await CarryAsync(page, "Notes", box.X + box.Width / 2, box.Y + box.Height * 0.75f);
        await Expect(start).ToHaveAttributeAsync("data-drop", "");
        await Expect(Strip(page, "bottom")).Not.ToHaveAttributeAsync("data-drop", "");
        await Expect(Mark(page)).ToBeHiddenAsync();
        // The strips and their buttons show the cursor of the drag, as the dock's area does.
        await Expect(start).ToHaveCSSAsync("cursor", "grabbing");
        await Expect(StripButton(page, "Gear")).ToHaveCSSAsync("cursor", "grabbing");
        await DropAsync(page);

        // The panel goes after the strip's last button, and its own button takes the focus.
        Assert.Equal(["Gear", "Notes", "Weather"], await dock.AutoHiddenAsync());
        Assert.Equal(["Trails", "Map"], await dock.PanelsAsync());
        await Expect(StripButton(page, "Notes")).ToBeFocusedAsync();
        await Expect(start).Not.ToHaveAttributeAsync("data-drop", "");
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("{\"panel\":\"notes\",\"edge\":\"start\"");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldDockItsPanel_WhenItsButtonIsDraggedIntoTheDock(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        var notes = (await Panel(page, "Notes").BoundingBoxAsync())!;

        // Onto the middle of a group, the panel joins it as its last tab.
        await CarryAsync(page, StripButton(page, "Gear"), notes.X + notes.Width / 2, notes.Y + notes.Height / 2);
        await Expect(StripButton(page, "Gear")).ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeVisibleAsync();
        await DropAsync(page);

        Assert.Equal(["Trails", "Map", "Notes", "Gear"], await dock.PanelsAsync());
        Assert.Equal(["Weather"], await dock.AutoHiddenAsync());
        // The drag wasn't taken for a click on the button: nothing slid out, and the docked panel's tab has the focus.
        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(0);
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        await Expect(Strip(page, "start")).ToHaveCountAsync(0);

        // Onto an edge of the dock, the panel gets a new group along it.
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        await CarryAsync(page, StripButton(page, "Weather"), area.X + area.Width / 2, area.Y + 4);
        await DropAsync(page);

        Assert.Equal(["Weather", "Trails", "Map", "Notes", "Gear"], await dock.PanelsAsync());
        Assert.Empty(await dock.AutoHiddenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldSendItsPanelToAnotherStripAndSlideTheOpenPanelBack_WhenItsButtonIsDragged(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        await dock.RevealAsync("Gear");
        var own = (await Strip(page, "start").BoundingBoxAsync())!;
        var other = (await Strip(page, "bottom").BoundingBoxAsync())!;
        var commits = await page.Locator(".tb-dock").GetAttributeAsync("data-commits");

        // A button that is dragged slides back the panel that is out, so that it doesn't cover where the button can
        // go. The button's own strip takes no drop.
        await CarryAsync(page, StripButton(page, "Gear"), own.X + own.Width / 2, own.Y + own.Height * 0.75f);
        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(0);
        await Expect(Strip(page, "start")).Not.ToHaveAttributeAsync("data-drop", "");
        await page.Mouse.MoveAsync(other.X + other.Width * 0.75f, other.Y + other.Height / 2, new() { Steps = 4 });
        await Expect(Strip(page, "bottom")).ToHaveAttributeAsync("data-drop", "");
        // Escape puts the button down where it was.
        await page.Keyboard.PressAsync("Escape");
        await Expect(Strip(page, "bottom")).Not.ToHaveAttributeAsync("data-drop", "");
        await Expect(StripButton(page, "Gear")).Not.ToHaveAttributeAsync("data-dragging", "");
        await page.Mouse.UpAsync();
        Assert.Equal(["Gear", "Weather"], await dock.AutoHiddenAsync());
        // The panel that slid back for the drag wasn't counted as a finished action.
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-commits", commits!);

        // The next drag slides back the panel that is out then.
        await dock.RevealAsync("Weather");
        await CarryAsync(page, StripButton(page, "Gear"), other.X + other.Width * 0.75f, other.Y + other.Height / 2);
        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(0);
        await DropAsync(page);

        Assert.Equal(["Weather", "Gear"], await dock.AutoHiddenAsync());
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("{\"panel\":\"gear\",\"edge\":\"bottom\"");
        // A click on a button still slides its panel out.
        await dock.RevealAsync("Gear");
        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldGiveItsPanelTheWholeDock_WhenItsButtonIsDroppedOnADockWithNothingDocked(
        string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        foreach (var panel in new[] { "Trails", "Map", "Notes" })
        {
            await dock.AutoHideAsync(panel, DockPosition.Start);
        }

        // Nothing is docked, so all of the dock's empty area takes the panel, not only the band along its edges.
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        await CarryAsync(page, StripButton(page, "Map"), area.X + area.Width / 2, area.Y + area.Height / 2);
        await ExpectMarkAsync(page, area.X, area.Y, area.Width, area.Height);
        await DropAsync(page);

        Assert.Equal(["Map"], await dock.ShowingAsync());
        Assert.Equal(["Gear", "Trails", "Notes", "Weather"], await dock.AutoHiddenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldPutItsPanelInAWindow_WhenItsButtonIsDroppedOnOne(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Notes");
        var window = (await Panel(page, "Notes").BoundingBoxAsync())!;

        await CarryAsync(page, StripButton(page, "Weather"), window.X + window.Width / 2,
            window.Y + window.Height / 2);
        await DropAsync(page);

        Assert.Equal(["Notes", "Weather"], await dock.FloatingAsync());
        Assert.Equal(["Gear"], await dock.AutoHiddenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldSlideItsPanelBack_WhenAFingerRestsOnItsButtonForAMomentBeforeLifting(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        await page.Tb().Dock().RevealAsync("Gear");

        // A slow tap: the finger holds still past the moment that picks the button up, drifts by a pixel or two as a
        // resting finger does, then lifts.
        var carried = false;
        await ChangeAsync(page, async () => carried = await StripButton(page, "Gear").EvaluateAsync<bool>("""
            async button => {
                const box = button.getBoundingClientRect();
                const fire = (type, drift) => button.dispatchEvent(new PointerEvent(type, { bubbles: true,
                    pointerId: 7, pointerType: 'touch', isPrimary: true, button: type === 'pointermove' ? -1 : 0,
                    clientX: box.left + box.width / 2 + drift, clientY: box.top + box.height / 2 }));
                fire('pointerdown', 0);
                await new Promise(resolve => setTimeout(resolve, 400));
                const carried = button.hasAttribute('data-dragging');
                fire('pointermove', 2);
                fire('pointerup', 2);
                button.click();
                return carried;
            }
            """));

        // The hold picked the button up, and the tap still did what a tap does: the panel slid back, and stays back.
        Assert.True(carried);
        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(0);
        await Expect(StripButton(page, "Gear")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(StripButton(page, "Gear")).Not.ToHaveAttributeAsync("data-dragging", "");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldTakeNoDropOnThePanelThatIsStillOut_WhenAButtonIsCarriedOverItAtOnce(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        await dock.RevealAsync("Gear");

        // The press, the moves and the release come in one task, so the panel can't have slid back in between.
        var seen = await page.EvaluateAsync<string>("""
            () => {
                const button = document.querySelector('[data-part="strip-button"][data-panel="weather"]');
                const from = button.getBoundingClientRect();
                const out = document.querySelector('.tb-dock__panel--revealed').getBoundingClientRect();
                const fire = (target, type, x, y) => target.dispatchEvent(new PointerEvent(type, { bubbles: true,
                    cancelable: true, pointerId: 7, isPrimary: true, button: 0, pointerType: 'mouse', clientX: x,
                    clientY: y }));
                fire(button, 'pointerdown', from.x + 5, from.y + 5);
                fire(document, 'pointermove', from.x + 15, from.y + 5);
                fire(document, 'pointermove', out.x + out.width / 2, out.y + out.height / 2);
                const seen = `${button.hasAttribute('data-dragging')} ${document.querySelector('.tb-dock__drop')
                    .dataset.show ?? 'no mark'}`;
                fire(document, 'pointerup', out.x + out.width / 2, out.y + out.height / 2);
                return seen;
            }
            """);

        Assert.Equal("true no mark", seen);
        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(0);
        Assert.Equal(["Gear", "Weather"], await dock.AutoHiddenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldDockASlidOutPanel_WhenItsTabIsDroppedOnADockedGroup(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        await dock.RevealAsync("Gear");
        var own = (await Panel(page, "Gear").BoundingBoxAsync())!;
        var notes = (await Panel(page, "Notes").BoundingBoxAsync())!;

        // Over the slid-out panel itself there is nowhere to land.
        await CarryAsync(page, "Gear", own.X + own.Width / 2, own.Y + own.Height / 2);
        await Expect(Tab(page, "Gear")).ToHaveAttributeAsync("data-dragging", "");
        await Expect(Mark(page)).ToBeHiddenAsync();
        await page.Mouse.MoveAsync(notes.X + notes.Width / 2, notes.Y + notes.Height / 2, new() { Steps = 4 });
        await Expect(Mark(page)).ToBeVisibleAsync();
        await DropAsync(page);

        Assert.Equal(["Trails", "Map", "Notes", "Gear"], await dock.PanelsAsync());
        Assert.Equal(["Weather"], await dock.AutoHiddenAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldBeOutlinedInTheHighlight_WhenATabIsCarriedOverItAndColorsAreForced(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips", new() { ForcedColors = ForcedColors.Active });
        var strip = Strip(page, "bottom");
        var box = (await strip.BoundingBoxAsync())!;

        await CarryAsync(page, "Notes", box.X + box.Width * 0.75f, box.Y + box.Height / 2);

        await Expect(strip).ToHaveCSSAsync("outline-style", "solid");
        await Expect(strip).ToHaveCSSAsync("outline-color", await SystemColorAsync(page, "Highlight"));
        // Escape puts the tab down, and the strip's outline goes.
        await page.Keyboard.PressAsync("Escape");
        await Expect(strip).Not.ToHaveAttributeAsync("data-drop", "");
        await page.Mouse.UpAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldLeaveAStripButtonInsideAPanelAlone_WhenItIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var root = page.Locator(".tb-dock");
        // A dock inside a panel has strips of its own, whose buttons this dock doesn't carry.
        await Panel(page, "Notes").EvaluateAsync("""
            panel => {
                const strip = document.createElement('div');
                strip.dataset.part = 'strip';
                strip.innerHTML = '<button type="button" data-part="strip-button" data-panel="gear">Inner</button>';
                panel.prepend(strip);
            }
            """);
        var inner = page.GetByRole(AriaRole.Button, new() { Name = "Inner", Exact = true });
        var box = (await inner.BoundingBoxAsync())!;

        await CarryAsync(page, inner, box.X + box.Width / 2 + 120, box.Y + box.Height / 2 + 80);

        await Expect(inner).Not.ToHaveAttributeAsync("data-dragging", "");
        await Expect(root).Not.ToHaveAttributeAsync("data-moving", "");
        await Expect(Mark(page)).ToBeHiddenAsync();
        await page.Mouse.UpAsync();
    }

    // Only Chromium can drive touch from Playwright.
    [Fact]
    public async Task Strip_ShouldDockItsPanel_WhenAFingerHoldsItsButtonAndDragsItIntoTheDock()
    {
        var page = await OpenDockAsync("chromium", "?layout=strips", new() { HasTouch = true });
        var dock = page.Locator(".tb-dock");
        var button = StripButton(page, "Gear");
        var from = (await button.BoundingBoxAsync())!;
        var notes = (await Panel(page, "Notes").BoundingBoxAsync())!;
        var (x, y) = (from.X + from.Width / 2, from.Y + from.Height / 2);
        var (toX, toY) = (notes.X + notes.Width / 2, notes.Y + notes.Height / 2);
        var touch = await page.Context.NewCDPSessionAsync(page);
        var commits = int.Parse((await dock.GetAttributeAsync("data-commits"))!, CultureInfo.InvariantCulture);

        // A finger that holds still for a moment picks the button up, as it does a tab. The page reports the pickup.
        await button.EvaluateAsync("""
            button => {
                window.pickedUp = new Promise(resolve => new MutationObserver(() => {
                    if (button.hasAttribute('data-dragging')) {
                        resolve();
                    }
                }).observe(button, { attributes: true, attributeFilter: ['data-dragging'] }));
            }
            """);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchStart", x, y);
        await page.EvaluateAsync("() => window.pickedUp");
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", (x + toX) / 2, (y + toY) / 2);
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchMove", toX, toY);
        await Expect(Mark(page)).ToBeVisibleAsync();
        await DialogMoveAndResizeTests.TouchAsync(touch, "touchEnd", null, null);

        await Expect(dock).ToHaveAttributeAsync("data-commits", (commits + 1).ToString(CultureInfo.InvariantCulture));
        Assert.Equal(["Trails", "Map", "Notes", "Gear"], await page.Tb().Dock().PanelsAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMakeNoNewGroup_WhenATabIsDroppedAtAnEdgeOrASideOfACompactDock(string browser)
    {
        var page = await OpenDockAsync(browser, "?compact=true");
        await NarrowAsync(page, "24rem");
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-compact", "");
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        // Along the bottom edge, where a wide dock would give the panel a group of its own. A compact dock takes the
        // drop as one on the middle of the tab's own group, which floats the panel.
        await CarryAsync(page, "Map", area.X + area.Width / 2, area.Y + area.Height - 4);
        await DropAsync(page);

        Assert.Equal(["Map"], await page.Tb().Dock().FloatingAsync());

        // The same goes for the side of the group, where a wide dock would split it.
        await CarryAsync(page, "Notes", area.X + area.Width - 24, area.Y + area.Height / 2);
        await DropAsync(page);

        Assert.Equal(["Map", "Notes"], await page.Tb().Dock().FloatingAsync());
        Assert.True(await page.Tb().Dock().IsCompactAsync());
        Assert.Equal(["Trails", "Gear", "Weather", "Map", "Notes"], await page.Tb().Dock().PanelsAsync());
    }

    private static ILocator Strip(IPage page, string edge) => page.Locator($"[data-part=strip][data-edge={edge}]");

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
    private static Task CarryAsync(IPage page, string tab, float x, float y) => CarryAsync(page, Tab(page, tab), x, y);

    // Presses on a tab or a strip's button and carries it to a point, without letting go.
    private static async Task CarryAsync(IPage page, ILocator item, float x, float y)
    {
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-ready", "");
        var box = (await item.BoundingBoxAsync())!;
        await page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, y, new() { Steps = 6 });
    }
}
