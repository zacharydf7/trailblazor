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
        var page = await OpenDockAsync(browser);
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

    private static ILocator StripButton(IPage page, string title) => page.Locator("[data-part=strip-button]")
        .And(page.GetByRole(AriaRole.Button, new() { Name = title, Exact = true }));
}
