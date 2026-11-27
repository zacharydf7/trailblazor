using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DockManagerAutoHideTests(BrowserFixture fixture) : DockManagerPage(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strips_ShouldLieAlongTheirEdgesAroundTheDocksArea_WhenPanelsAreAutoHidden(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var frame = (await page.Locator(".tb-dock-frame").BoundingBoxAsync())!;
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var start = (await page.GetByRole(AriaRole.Group, new() { Name = "Auto-hidden panels (At the start)" })
            .BoundingBoxAsync())!;
        var bottom = (await page.GetByRole(AriaRole.Group, new() { Name = "Auto-hidden panels (At the bottom)" })
            .BoundingBoxAsync())!;

        // The frame is as large as the dock was given, and the strips take their room from the area.
        Assert.Equal((768, 384), (frame.Width, frame.Height));
        Assert.Equal((frame.X, frame.Y), (start.X, start.Y));
        Assert.Equal(start.X + start.Width, area.X, 0.5);
        Assert.Equal(area.X + area.Width, frame.X + frame.Width, 0.5);
        Assert.Equal((frame.X, frame.Width), (bottom.X, bottom.Width));
        Assert.Equal(area.Y + area.Height, bottom.Y, 0.5);
        Assert.Equal(bottom.Y + bottom.Height, frame.Y + frame.Height, 0.5);
        Assert.Equal(start.Height, area.Height, 0.5);
        // A title along a side reads from top to bottom, and each button is large enough to press.
        var gear = (await StripButton(page, "Gear").BoundingBoxAsync())!;
        var weather = (await StripButton(page, "Weather").BoundingBoxAsync())!;
        Assert.True(gear.Height > gear.Width && gear.Width >= 24, $"{gear.Width} by {gear.Height}");
        Assert.True(weather.Width > weather.Height && weather.Height >= 24, $"{weather.Width} by {weather.Height}");
        Assert.Equal(["Gear", "Weather"], await page.Tb().Dock().AutoHiddenAsync());
        Assert.Equal(["Trails", "Map", "Notes"], await page.Tb().Dock().PanelsAsync());

        // The start strip comes before the dock's area for the Tab key, and the bottom one after it.
        await StripButton(page, "Gear").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(Tab(page, "Trails")).ToBeFocusedAsync();
        await StripButton(page, "Weather").FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        Assert.True(await page.EvaluateAsync<bool>(
            "() => document.querySelector('.tb-dock').contains(document.activeElement)"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldSlideItsPanelOutOverTheDockAndBackOnEscape_WhenItsButtonIsPressed(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        // The notes float over the edge the panel slides out from.
        await dock.FloatAsync("Notes");
        await dock.MoveWindowAsync("Notes", -25, 0);
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        await StripButton(page, "Gear").ClickAsync();

        // The panel covers a quarter of the area's width from its start edge, from top to bottom, with its tab in a
        // row of its own, and it lies over the docked groups and the window.
        var row = (await page.Locator(".tb-dock__group--revealed").BoundingBoxAsync())!;
        var panel = (await Panel(page, "Gear").BoundingBoxAsync())!;
        var inner = area.Width - 2;
        Assert.Equal((area.X + 1, area.Y + 1), (row.X, row.Y));
        Assert.Equal(inner / 4, row.Width, 1.0);
        Assert.Equal((row.X, row.Width), (panel.X, panel.Width));
        Assert.Equal(area.Y + area.Height - 1, panel.Y + panel.Height, 1.0);
        Assert.Equal("gear", await page.EvaluateAsync<string?>(
            "([x, y]) => document.elementFromPoint(x, y).closest('.tb-dock__panel')?.dataset.panel",
            new[] { (double)(panel.X + panel.Width / 2), panel.Y + panel.Height / 2 }));
        // A panel that is out is drawn as a window, but isn't one of them.
        Assert.Equal(["Notes"], await dock.FloatingAsync());
        await Expect(Tab(page, "Gear")).ToBeFocusedAsync();
        await Expect(StripButton(page, "Gear")).ToHaveAttributeAsync("aria-expanded", "true");
        // A press inside it keeps it out.
        await page.GetByRole(AriaRole.Button, new() { Name = "Add gear" }).ClickAsync();
        await Expect(Panel(page, "Gear")).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Add gear" }).PressAsync("Escape");

        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(0);
        await Expect(StripButton(page, "Gear")).ToBeFocusedAsync();
        await Expect(StripButton(page, "Gear")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Add gear" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldSlideItsPanelBack_WhenAPressOrTheFocusGoesElsewhere(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        var revealed = page.Locator(".tb-dock__group--revealed");

        // A press on a docked tab that the panel doesn't cover.
        await dock.RevealAsync("Gear");
        await Tab(page, "Notes").ClickAsync();
        await Expect(revealed).ToHaveCountAsync(0);

        // The focus going out of the dock.
        await dock.RevealAsync("Gear");
        await page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).FocusAsync();
        await Expect(revealed).ToHaveCountAsync(0);

        // A press on plain text outside the dock, which takes no focus.
        await dock.RevealAsync("Gear");
        await page.GetByTestId("layout").ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Expect(revealed).ToHaveCountAsync(0);

        // Another strip's button slides its own panel out in its place: one panel is out at a time.
        await dock.RevealAsync("Gear");
        await StripButton(page, "Weather").ClickAsync();
        await Expect(Tab(page, "Weather")).ToBeFocusedAsync();
        await Expect(revealed).ToHaveCountAsync(1);
        await Expect(revealed).ToHaveAttributeAsync("data-edge", "bottom");
        await Expect(StripButton(page, "Gear")).ToHaveAttributeAsync("aria-expanded", "false");

        // The button of the panel that is out slides it back.
        await StripButton(page, "Weather").ClickAsync();
        await Expect(revealed).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoHide_ShouldKeepWhatAPanelsIframeLoaded_WhenThePanelGoesToAStripAndSlidesOut(string browser)
    {
        var page = await OpenDockAsync(browser);
        var dock = page.Tb().Dock();
        var city = page.FrameLocator("iframe[title=Forecast]").GetByLabel("City");
        await city.FillAsync("Oslo");

        await dock.AutoHideAsync("Weather", DockPosition.Top);

        // The focus is on the panel's button on its strip, and screen readers hear where the panel went.
        await Expect(StripButton(page, "Weather")).ToBeFocusedAsync();
        await page.Tb().Announcements.ExpectAnnouncedAsync("Weather is auto-hidden.");
        await Expect(page.GetByTestId("layout")).ToContainTextAsync(
            "\"autoHidden\":[{\"panel\":\"weather\",\"edge\":\"top\",\"size\":25}]");
        Assert.Equal(["Weather"], await dock.AutoHiddenAsync());
        // On its strip the panel is in no window, and asking for its window doesn't slide it out.
        Assert.Equal("The panel \"Weather\" isn't in a floating window.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dock.WindowAsync("Weather"))).Message);
        await Expect(StripButton(page, "Weather")).ToHaveAttributeAsync("aria-expanded", "false");

        await dock.RevealAsync("Weather");

        // The top strip's panel covers the area's top quarter, across its width.
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var row = (await page.Locator(".tb-dock__group--revealed").BoundingBoxAsync())!;
        Assert.Equal((area.X + 1, area.Y + 1), (row.X, row.Y));
        Assert.Equal(area.Width - 2, row.Width, 1.0);
        await Expect(city).ToHaveValueAsync("Oslo");

        // A helper that needs the panel's tab slides the panel out first.
        await dock.ConcealAsync();
        await dock.DockAsync("Weather");

        Assert.Empty(await dock.AutoHiddenAsync());
        await Expect(page.Locator("[data-part=strip]")).ToHaveCountAsync(0);
        await Expect(city).ToHaveValueAsync("Oslo");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldLieWhereTextStartsAndSlideItsPanelOutFromThere_WhenTextRunsRightToLeft(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips&direction=rtl");
        var frame = (await page.Locator(".tb-dock-frame").BoundingBoxAsync())!;
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var start = (await StripButton(page, "Gear").BoundingBoxAsync())!;

        Assert.Equal(frame.X, area.X, 0.5);
        Assert.True(start.X >= area.X + area.Width - 0.5, $"{start.X} against {area.X + area.Width}");

        await page.Tb().Dock().RevealAsync("Gear");

        var row = (await page.Locator(".tb-dock__group--revealed").BoundingBoxAsync())!;
        Assert.Equal(area.X + area.Width - 1, row.X + row.Width, 1.0);
        Assert.Equal((area.Width - 2) / 4, row.Width, 1.0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldLeaveTheWindowInFrontAlone_WhenItKeepsAPanelHiddenOrSlidOut(string browser)
    {
        // The raises are counted on the circuit's socket, so the page runs on the server.
        var page = await OpenDockAsync(browser, "?mode=server");
        var dock = page.Tb().Dock();
        await dock.FloatAsync("Weather");
        await dock.FloatAsync("Notes");
        // The notes' window is in front; out of the way, it leaves the weather's Refresh button free to press.
        await dock.MoveWindowAsync("Notes", 20, 20);
        // Counts the calls the dock's script makes to bring a window forward, by their name in what the page sends.
        await page.EvaluateAsync("""
            () => {
                window.raised = 0;
                const send = WebSocket.prototype.send;
                WebSocket.prototype.send = function (data) {
                    const text = typeof data === 'string' ? data : new TextDecoder().decode(data);
                    window.raised += text.includes('OnRaise') ? 1 : 0;
                    return send.call(this, data);
                };
            }
            """);
        Task<int> RaisedAsync() => page.EvaluateAsync<int>("() => window.raised");
        var refresh = page.GetByRole(AriaRole.Button, new() { Name = "Refresh" });

        // A press in the weather's window, which is behind, brings it forward: the press and the focus it moves
        // each call for it.
        await refresh.ClickAsync();
        await dock.SelectAsync("Trails");
        Assert.True(await RaisedAsync() > 0);

        // The notes showed before they went to their strip, so the strip keeps their panel hidden in the dock.
        await dock.AutoHideAsync("Notes", DockPosition.End);
        var settled = await RaisedAsync();
        await refresh.ClickAsync();
        await dock.SelectAsync("Map");
        Assert.Equal(settled, await RaisedAsync());

        // Out over the dock, the notes still aren't a window for the weather to come in front of.
        await dock.RevealAsync("Notes");
        await refresh.ClickAsync();
        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(0);
        await dock.SelectAsync("Trails");

        Assert.Equal(settled, await RaisedAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Strip_ShouldShowWhichPanelIsOutInTheHighlightColors_WhenColorsAreForced(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips", new() { ForcedColors = ForcedColors.Active });
        var gear = StripButton(page, "Gear");
        var plain = await gear.EvaluateAsync<string>("button => getComputedStyle(button).backgroundColor");

        await page.Tb().Dock().RevealAsync("Gear");

        var highlight = await page.EvaluateAsync<string>("""
            () => {
                const probe = document.body.appendChild(document.createElement('span'));
                probe.style.color = 'Highlight';
                const color = getComputedStyle(probe).color;
                probe.remove();
                return color;
            }
            """);
        await Expect(gear).ToHaveCSSAsync("background-color", highlight);
        Assert.NotEqual(plain, highlight);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideSeparator_ShouldResizeTheSlidOutPanel_WhenItsKeysOrItsGripsMenuAreUsed(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        await dock.RevealAsync("Weather");
        var edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Weather", Exact = true });
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Size of Weather", Exact = true });
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        // The edge lies along the top of the panel's row, nine pixels deep around its line, with its grip on it.
        var line = (await edge.BoundingBoxAsync())!;
        var top = (await page.Locator(".tb-dock__group--revealed").BoundingBoxAsync())!;
        var grip = (await button.BoundingBoxAsync())!;
        Assert.Equal(top.Y, line.Y + line.Height / 2, 1.0);
        Assert.Equal(9, line.Height, 0.01);
        Assert.Equal(top.Width, line.Width, 1.0);
        Assert.Equal(top.Y, grip.Y + grip.Height / 2, 1.0);
        // It comes after its panel for the Tab key and for screen readers, and its grip's button is no Tab stop.
        Assert.EndsWith("weather slide-separator slide-grip", await PartsAsync(page));
        await Expect(edge).ToHaveAttributeAsync("tabindex", "0");
        await Expect(button).ToHaveAttributeAsync("tabindex", "-1");

        // The weather slides up from the bottom, so the helper's keys grow it upward: three tens and two ones.
        await dock.ResizeAsync("Resize Weather", 57);

        Assert.Equal(57, await dock.SizeAsync("Resize Weather"));
        var row = (await page.Locator(".tb-dock__group--revealed").BoundingBoxAsync())!;
        Assert.Equal(area.Y + 1 + (area.Height - 2) * 0.43, row.Y, 1.0);
        await Expect(edge).ToBeFocusedAsync();
        await Expect(edge).ToHaveAttributeAsync("aria-orientation", "horizontal");

        // Home and End take the panel to its smallest and its largest. The arrows along the edge do nothing, and Alt,
        // Ctrl or Cmd with an arrow stays with the browser.
        // The smallest here is the 64 pixels a group may be, which is more than a tenth of the area's height.
        var least = (int)Math.Round(6400 / (area.Height - 2), MidpointRounding.AwayFromZero);
        await Expect(edge).ToHaveAttributeAsync("aria-valuemin",
            least.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await Expect(edge).ToHaveAttributeAsync("aria-valuemax", "90");
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => dock.ResizeAsync("Resize Weather", least - 1));
        await ChangeAsync(page, () => edge.PressAsync("End"));
        Assert.Equal(90, await dock.SizeAsync("Resize Weather"));
        await ChangeAsync(page, () => edge.PressAsync("Home"));
        Assert.Equal(least, await dock.SizeAsync("Resize Weather"));
        Assert.True(least > 10, $"{least}");
        foreach (var key in new[] { "ArrowRight", "Alt+ArrowUp", "Control+ArrowUp", "Meta+ArrowUp" })
        {
            await edge.PressAsync(key);
        }

        await ChangeAsync(page, () => edge.PressAsync("ArrowUp"));
        Assert.Equal(least + 1, await dock.SizeAsync("Resize Weather"));

        // The grip on the edge opens a menu of single steps, for those who can't drag.
        await button.ClickAsync();
        await ChangeAsync(page,
            () => page.GetByRole(AriaRole.Menuitem, new() { Name = "Make Weather larger" }).ClickAsync());
        Assert.Equal(least + 11, await dock.SizeAsync("Resize Weather"));

        // Escape on the edge slides the panel back, as anywhere in it.
        await edge.FocusAsync();
        await edge.PressAsync("Escape");
        await Expect(page.Locator(".tb-dock__group--revealed")).ToHaveCountAsync(0);
        await Expect(StripButton(page, "Weather")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideSeparator_ShouldRestateItsLeastSize_WhenAnotherPanelSlidesOutOrTheDockShrinks(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips");
        var dock = page.Tb().Dock();
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;
        var across = (int)Math.Round(6400 / (area.Height - 2), MidpointRounding.AwayFromZero);

        // Gear slides out from the side and the weather from the bottom, where the same 64 pixels are a larger share
        // of the dock. The edge is the same element each time, and states the least size of the panel that is out.
        foreach (var (panel, least) in new[] { ("Gear", 10), ("Weather", across), ("Gear", 10), ("Weather", across) })
        {
            await dock.RevealAsync(panel);
            var edge = page.GetByRole(AriaRole.Separator, new() { Name = $"Resize {panel}", Exact = true });
            await Expect(edge).ToHaveAttributeAsync("aria-valuemin",
                least.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        // In a dock narrower than a group may be, Gear's quarter of it is out of reach, and counts as the least.
        await dock.RevealAsync("Gear");
        var gear = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Gear", Exact = true });
        await page.Locator(".tb-dock-frame").EvaluateAsync("frame => frame.parentElement.style.inlineSize = '6rem'");
        await Expect(gear).ToHaveAttributeAsync("aria-valuemin", "25");
        await Expect(gear).ToHaveAttributeAsync("aria-valuemax", "90");

        // Once the edge has moved, the quarter no longer counts: the least it states is where Home now takes it.
        await gear.FocusAsync();
        await ChangeAsync(page, () => page.Keyboard.PressAsync("End"));
        await ChangeAsync(page, () => page.Keyboard.PressAsync("Home"));
        var smallest = (await gear.GetAttributeAsync("aria-valuenow"))!;
        Assert.NotEqual("25", smallest);
        await Expect(gear).ToHaveAttributeAsync("aria-valuemin", smallest);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideSeparator_ShouldGrowThePanelAwayFromItsStrip_WhenTextRunsRightToLeft(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips&direction=rtl");
        var dock = page.Tb().Dock();
        await dock.RevealAsync("Gear");
        var edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Gear", Exact = true });
        var area = (await page.Locator(".tb-dock").BoundingBoxAsync())!;

        // The strip is at the right here, so the edge is the panel's left side and Left grows the panel.
        var row = (await page.Locator(".tb-dock__group--revealed").BoundingBoxAsync())!;
        var line = (await edge.BoundingBoxAsync())!;
        Assert.Equal(row.X, line.X + line.Width / 2, 1.0);
        await edge.FocusAsync();
        await ChangeAsync(page, () => edge.PressAsync("Shift+ArrowLeft"));

        Assert.Equal(35, await dock.SizeAsync("Resize Gear"));
        row = (await page.Locator(".tb-dock__group--revealed").BoundingBoxAsync())!;
        Assert.Equal(area.X + area.Width - 1, row.X + row.Width, 1.0);
        Assert.Equal((area.Width - 2) * 0.35, row.Width, 1.0);

        // The helper presses the same keys.
        await dock.ResizeAsync("Resize Gear", 30);
        Assert.Equal(30, await dock.SizeAsync("Resize Gear"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SlideSeparator_ShouldBeDrawnInTheHighlight_WhenItHasTheFocusAndColorsAreForced(string browser)
    {
        var page = await OpenDockAsync(browser, "?layout=strips", new() { ForcedColors = ForcedColors.Active });
        // The panel slides out by a key, which keeps the browser in keyboard mode: after a click, Firefox wouldn't
        // match :focus-visible for focus from a script.
        await StripButton(page, "Gear").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        var edge = page.GetByRole(AriaRole.Separator, new() { Name = "Resize Gear", Exact = true });
        await Expect(edge).ToBeVisibleAsync();
        var line = () => edge.EvaluateAsync<string>("line => getComputedStyle(line, '::before').backgroundColor");
        Assert.Equal(await SystemColorAsync(page, "CanvasText"), await line());

        await edge.FocusAsync();
        // The line's color eases in, so it is read once the transition ends.
        await WaitForFadesAsync(page);

        Assert.Equal(await SystemColorAsync(page, "Highlight"), await line());
    }

    // Only Chromium emulates a phone, whose pointer can't hover.
    [Fact]
    public async Task Dock_ShouldAlwaysShowItsGrips_WhenTheMainPointerIsAFinger()
    {
        var page = await OpenDockAsync("chromium", "?layout=strips", new() { IsMobile = true, HasTouch = true });
        Assert.True(await page.EvaluateAsync<bool>("() => matchMedia('(pointer: coarse)').matches"));
        await page.Tb().Dock().RevealAsync("Gear");

        // The grip on a separator and the one on a slid-out panel's edge both show with nothing hovering.
        foreach (var name in new[] { "Size of Trails", "Size of Gear" })
        {
            await Expect(page.Locator(".tb-dock").GetByRole(AriaRole.Button, new() { Name = name, Exact = true }))
                .ToHaveCSSAsync("opacity", "1");
        }
    }
}
