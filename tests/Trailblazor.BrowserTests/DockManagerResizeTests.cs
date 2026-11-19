using System.Globalization;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// The fixture's dock is 766 by 382 pixels inside its border, and no separator leaves a group under 64.
public class DockManagerResizeTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldResizeWithinTheSmallestGroupSize_WhenASeparatorsKeysAreUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        var trails = Separator(page, "Resize Trails");
        await trails.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "26");
        await page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "36");
        await page.Keyboard.PressAsync("ArrowLeft");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "35");
        Assert.Equal(0.35 * 766, await WidthAsync(page, 0), 1.0);

        // 64 pixels is a little over 8% of the width.
        await page.Keyboard.PressAsync("Home");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "8");
        Assert.Equal(64, await WidthAsync(page, 0), 1.0);
        await page.Keyboard.PressAsync("End");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "92");
        Assert.Equal(64, await WidthAsync(page, 1), 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldHandTheAppTheNewSizes_WhenASeparatorsKeysAreUsed(string browser)
    {
        var page = await OpenDockAsync(browser);
        await Separator(page, "Resize Trails").FocusAsync();

        // Every press counts, even when they come faster than the app renders the sizes.
        for (var press = 0; press < 10; press++)
        {
            await page.Keyboard.PressAsync("ArrowRight");
        }

        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-report", "10");
        await Expect(Separator(page, "Resize Trails")).ToHaveAttributeAsync("aria-valuenow", "35");
        // The two parts shared a size of 4.
        await Expect(page.GetByTestId("layout")).ToContainTextAsync("\"size\":1.4");
        // The app's boxes show now, so the script's own are gone.
        Assert.Equal("", await page.Locator(".tb-dock__group").First.EvaluateAsync<string>(
            "group => group.style.getPropertyValue('--tb-dock-live-w')"));
        Assert.Equal(0.35 * 766, await WidthAsync(page, 0), 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldResizeWithUpAndDown_WhenTheSeparatorLiesBetweenStackedGroups(string browser)
    {
        var page = await OpenDockAsync(browser);
        var notes = Separator(page, "Resize Notes");
        await notes.FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(notes).ToHaveAttributeAsync("aria-valuenow", "76");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(notes).ToHaveAttributeAsync("aria-valuenow", "74");

        // Left and Right belong to side-by-side separators, and Alt, Ctrl and Cmd leave the keys to the browser.
        foreach (var key in new[] { "ArrowRight", "ArrowLeft", "Alt+ArrowDown", "Control+ArrowDown", "Meta+ArrowDown" })
        {
            await page.Keyboard.PressAsync(key);
        }

        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(notes).ToHaveAttributeAsync("aria-valuenow", "73");
        // The first group is beside the stack, not in it, and keeps its height.
        Assert.Equal(382, await page.Locator(".tb-dock__panel").First.EvaluateAsync<double>(
            "panel => panel.getBoundingClientRect().bottom - panel.parentElement.getBoundingClientRect().top - 1"), 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldGrowTheFirstGroupWithTheLeftArrow_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenDockAsync(browser, "?direction=rtl");
        var trails = Separator(page, "Resize Trails");
        await trails.FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "26");
        // The first group sits on the right.
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var group = (await page.Locator(".tb-dock__group").First.BoundingBoxAsync())!;
        Assert.Equal(dock.X + dock.Width - 1, group.X + group.Width, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMarkTheShowingTabAndAFocusedSeparatorInTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenDockAsync(browser, options: new() { ForcedColors = ForcedColors.Active });
        var highlight = await SystemColorAsync(page, "Highlight");
        // Forced colors drop the line's shadow, so the showing tab has a border, and the other tabs have none.
        var tops = await page.Locator(".tb-dock__tab").EvaluateAllAsync<string[]>("""
            tabs => tabs.map(tab => {
                const style = getComputedStyle(tab);
                return style.borderTopWidth === '0px' ? 'none' : style.borderTopColor;
            })
            """);
        Assert.Equal([highlight, "none", highlight, "none", highlight], tops);

        var separator = Separator(page, "Resize Trails");
        var line = () => separator.EvaluateAsync<string>("line => getComputedStyle(line, '::before').backgroundColor");
        Assert.Equal(await SystemColorAsync(page, "CanvasText"), await line());
        await separator.FocusAsync();
        // A key press puts the browser in keyboard mode, so :focus-visible matches after focus from a script.
        await page.Keyboard.PressAsync("ArrowRight");
        // The line's color eases in, so it is read once the transition ends.
        await WaitForFadesAsync(page);

        Assert.Equal(highlight, await line());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldStateHowFarASeparatorCanGo_WhenTheSmallestGroupSizeLimitsIt(string browser)
    {
        var page = await OpenDockAsync(browser);
        var trails = Separator(page, "Resize Trails");
        var least = await LimitAsync(trails, "aria-valuemin");
        var most = await LimitAsync(trails, "aria-valuemax");
        Assert.InRange(least, 1, 49);
        Assert.InRange(most, 51, 99);

        // Home and End take the separator to the limits it states, where a group is down to its 64 pixels.
        await trails.FocusAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("Home"));
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", least.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(64, await WidthAsync(page, 0), 1.0);
        await ChangeAsync(page, () => page.Keyboard.PressAsync("End"));
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", most.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(64, await WidthAsync(page, 1), 1.0);
        Assert.Equal(least, await LimitAsync(trails, "aria-valuemin"));
        Assert.Equal(most, await LimitAsync(trails, "aria-valuemax"));

        // The helper refuses a percent past a limit before it presses a key.
        var dock = page.Tb().Dock();
        var refused = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => dock.ResizeAsync("Resize Trails", least - 1));
        Assert.Contains($"goes from {least}% to {most}%", refused.Message);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dock.ResizeAsync("Resize Trails", most + 1));
        await dock.ResizeAsync("Resize Trails", least);
        Assert.Equal(least, await dock.SizeAsync("Resize Trails"));

        // In a narrower dock the same 64 pixels are a larger share, so the limits move in.
        await dock.ResizeAsync("Resize Trails", 50);
        await NarrowAsync(page, "30rem");
        await Expect(trails).Not.ToHaveAttributeAsync("aria-valuemin", least.ToString(CultureInfo.InvariantCulture));
        var narrower = await LimitAsync(trails, "aria-valuemin");
        var widest = await LimitAsync(trails, "aria-valuemax");
        Assert.True(narrower > least);

        // A dock that shrinks further can leave the separator's value out of reach. The value still lies within the
        // limits: it counts as the least until the separator moves.
        await trails.FocusAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("Home"));
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", narrower.ToString(CultureInfo.InvariantCulture));
        await NarrowAsync(page, "20rem");
        await Expect(trails).Not.ToHaveAttributeAsync("aria-valuemax", widest.ToString(CultureInfo.InvariantCulture));
        Assert.Equal(narrower, await LimitAsync(trails, "aria-valuemin"));
        Assert.InRange(await LimitAsync(trails, "aria-valuemax"), 51, widest - 1);

        // In a dock too narrow for two groups of 64 pixels nothing can move, and the separator states the value it
        // has as both limits.
        await NarrowAsync(page, "6rem");
        var now = narrower.ToString(CultureInfo.InvariantCulture);
        await Expect(trails).ToHaveAttributeAsync("aria-valuemax", now);
        await Expect(trails).ToHaveAttributeAsync("aria-valuemin", now);
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", now);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldCountASeparatorsValueAsItsMost_WhenTheDockShrinksWhileItIsAtItsLargest(string browser)
    {
        var page = await OpenDockAsync(browser);
        var trails = Separator(page, "Resize Trails");
        var least = (await trails.GetAttributeAsync("aria-valuemin"))!;
        await trails.FocusAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("End"));
        var most = await LimitAsync(trails, "aria-valuemax");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", most.ToString(CultureInfo.InvariantCulture));

        // The narrower dock leaves the value past the separator's reach, so the value counts as the most.
        await NarrowAsync(page, "20rem");

        await Expect(trails).Not.ToHaveAttributeAsync("aria-valuemin", least);
        Assert.Equal(most, await LimitAsync(trails, "aria-valuemax"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldStateItsFirstSeparatorsLimits_WhenAPanelGetsTheDocksSecondGroup(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=lone");

        // Gear leaves its window for a group of its own, and the separator that comes with it states its limits.
        await page.Tb().Dock().MoveToEdgeAsync("Gear", DockPosition.End);

        var notes = Separator(page, "Resize Notes");
        Assert.InRange(await LimitAsync(notes, "aria-valuemin"), 1, 49);
        Assert.InRange(await LimitAsync(notes, "aria-valuemax"), 51, 99);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldRestateASeparatorsLimits_WhenAnotherSeparatorChangesWhatItsSidesHold(string browser)
    {
        var page = await OpenDockAsync(browser);
        await page.Tb().Dock().MoveToNewGroupAsync("Map", DockPosition.End);
        var map = Separator(page, "Resize Map");
        var before = await LimitAsync(map, "aria-valuemin");

        // Trails goes down to its smallest, which changes what the next separator's first side holds.
        await Separator(page, "Resize Trails").FocusAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("Home"));

        await Expect(map).Not.ToHaveAttributeAsync("aria-valuemin", before.ToString(CultureInfo.InvariantCulture));
    }

    // Gives the box around the dock another width, as a page's layout would.
    private static Task NarrowAsync(IPage page, string width) => page.Locator(".tb-dock-frame").EvaluateAsync(
        "(frame, width) => frame.parentElement.style.inlineSize = width", width);

    private static async Task<int> LimitAsync(ILocator separator, string attribute) =>
        int.Parse((await separator.GetAttributeAsync(attribute))!, CultureInfo.InvariantCulture);

    private static Task<double> WidthAsync(IPage page, int group) => page.Locator(".tb-dock__group").Nth(group)
        .EvaluateAsync<double>("group => group.getBoundingClientRect().width");
}

// Dragging uses pointer capture, which Firefox can leak into parallel pages, so it runs with the other drag tests.
[Collection(nameof(PointerDrags))]
public class DockManagerDragTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldShowTheNewSizesAtOnceAndReportThemOnRelease_WhenASeparatorIsDragged(string browser)
    {
        var page = await OpenDockAsync(browser);
        var trails = Separator(page, "Resize Trails");
        var layout = await page.GetByTestId("layout").TextContentAsync();
        var box = (await trails.BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x + 76.6f, y, new() { Steps = 5 });

        // Everything in both parts follows before the release: the first group, and the stacked pair beside it.
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        foreach (var group in new[] { 1, 2 })
        {
            Assert.Equal(dock.X + 1 + 0.35 * 766,
                (await page.Locator(".tb-dock__group").Nth(group).BoundingBoxAsync())!.X, 1.0);
        }

        Assert.Equal(0.35 * 766, (await page.Locator(".tb-dock__group").First.BoundingBoxAsync())!.Width, 1.0);
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "35");
        await Expect(page.GetByTestId("layout")).ToHaveTextAsync(layout!);
        // A panel's iframe would take the pointer from the drag.
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-resizing", "");
        await Expect(Panel(page, "Weather")).ToHaveCSSAsync("pointer-events", "none");

        await page.Mouse.UpAsync();

        // The two parts shared a size of 4, and the pointer stopped within a pixel of 35%.
        await Expect(page.GetByTestId("layout")).ToContainTextAsync(
            new System.Text.RegularExpressions.Regex("\"size\":1\\.(39|40)"));
        await Expect(page.Locator(".tb-dock")).Not.ToHaveAttributeAsync("data-resizing", "");
        await Expect(Panel(page, "Weather")).ToHaveCSSAsync("pointer-events", "auto");
        Assert.Equal(0.35 * 766, (await page.Locator(".tb-dock__group").First.BoundingBoxAsync())!.Width, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldMoveAnotherSeparatorFromWhereADragHasPutThings_WhenTheAppHasNotRenderedYet(
        string browser)
    {
        var page = await OpenDockAsync(browser);
        var box = (await Separator(page, "Resize Trails").BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x - 76.6f, y, new() { Steps = 5 });

        // The drag is still held, so the app has no sizes yet, and the stacked pair is wider than it last rendered.
        // The key and the reading share one script, so no render from the app comes between them.
        var edges = await page.Locator(".tb-dock").EvaluateAsync<double[]>("""
            dock => {
                const stacked = dock.querySelector('.tb-dock__separator--vertical');
                stacked.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
                const box = dock.querySelectorAll('.tb-dock__group')[2].getBoundingClientRect();
                const notes = dock.querySelector('.tb-dock__panel[data-panel="notes"]').getBoundingClientRect();
                const own = dock.getBoundingClientRect();
                return [box.x - own.x, box.y - own.y, notes.bottom - own.y];
            }
            """);

        // The weather below the separator and the notes above it both follow, at the width the drag gave them.
        Assert.Equal(1 + 0.15 * 766, edges[0], 1.0);
        Assert.Equal(1 + 0.76 * 382, edges[1], 1.0);
        Assert.Equal(1 + 0.76 * 382, edges[2], 1.0);
        await page.Mouse.UpAsync();
        // The stacked pair shared a size of 4, and so did the two side-by-side parts.
        await Expect(page.GetByTestId("layout")).ToContainTextAsync(new System.Text.RegularExpressions.Regex(
            "\"size\":3\\.04.*\"size\":0\\.(59|60)|\"size\":0\\.(59|60).*\"size\":3\\.04"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldFollowThePointerFromTheRight_WhenASeparatorIsDraggedInRightToLeftText(string browser)
    {
        var page = await OpenDockAsync(browser, "?direction=rtl");
        var trails = Separator(page, "Resize Trails");
        var box = (await trails.BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        // The first group is on the right, so a drag to the left makes it wider.
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x - 76.6f, y, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "35");
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-report", "1");
        Assert.Equal(0.35 * 766, (await page.Locator(".tb-dock__group").First.BoundingBoxAsync())!.Width, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dock_ShouldStopAtTheSmallestGroupSize_WhenASeparatorIsDraggedPastIt(string browser)
    {
        var page = await OpenDockAsync(browser);
        var notes = Separator(page, "Resize Notes");
        var box = (await notes.BoundingBoxAsync())!;
        var dock = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);

        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, dock.Y + dock.Height + 40, new() { Steps = 5 });
        await page.Mouse.UpAsync();

        // The weather below keeps 64 of the 382 pixels.
        await Expect(notes).ToHaveAttributeAsync("aria-valuenow", "83");
        await Expect(page.Locator(".tb-dock")).ToHaveAttributeAsync("data-report", "1");
        var weather = (await page.Locator(".tb-dock__group").Nth(2).BoundingBoxAsync())!;
        Assert.Equal(64, dock.Y + dock.Height - 1 - weather.Y, 1.0);
    }
}
