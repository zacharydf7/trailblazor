namespace Trailblazor.Spikes.Tests.Overlays;

// Spike A, criteria 1 to 6 and 8. See spikes/overlays/README.md.
public class OverlaysTests(SpikeFixture fixture) : SpikeTest(fixture)
{
    public static TheoryData<string, string, string> TabDirections => new(
        BrowsersAndModes.Browsers.SelectMany(browser => BrowsersAndModes.Modes.SelectMany(mode =>
            new[] { (browser, mode, "Tab"), (browser, mode, "Shift+Tab") })));

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Modal_ShouldMoveFocusInside_WhenOpened(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");

        Assert.True(await IsFocusInsideAsync(page, "modal"));
    }

    [Theory, MemberData(nameof(TabDirections))]
    public async Task Modal_ShouldKeepFocusOffThePage_WhenTabbingPastEitherEnd(string browser, string mode, string key)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");
        await page.GetByTestId(key == "Tab" ? "modal-last" : "modal-first").FocusAsync();

        var focusLeftModal = false;
        for (var i = 0; i < 4; i++)
        {
            await page.Keyboard.PressAsync(key);
            focusLeftModal |= !await page.EvaluateAsync<bool>("""
                () => document.activeElement === document.body
                    || document.querySelector('[data-testid=modal]').contains(document.activeElement)
                """);
        }

        Assert.False(focusLeftModal);
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Modal_ShouldIgnorePointerInputOnThePage_WhenOpen(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");
        var box = (await page.GetByTestId("page-input").BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        Assert.False(await page.EvaluateAsync<bool>(
            "() => document.activeElement === document.querySelector('[data-testid=page-input]')"));
    }

    // Inert content can't take focus, even programmatically; Playwright's ARIA snapshot ignores inertness.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Modal_ShouldMakeThePageInert_WhenOpen(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");

        var focused = await page.EvaluateAsync<bool>("""
            () => {
                const input = document.querySelector('[data-testid=page-input]');
                input.focus();
                return document.activeElement === input;
            }
            """);

        Assert.False(focused);
    }

    [Theory, MemberData(nameof(ModesOnly))]
    public async Task Modal_ShouldRemoveThePageFromChromiumsAccessibilityTree_WhenOpen(string mode)
    {
        var page = await OpenModalAsync("chromium", mode, "/overlays");
        var session = await page.Context.NewCDPSessionAsync(page);

        var tree = (await session.SendAsync("Accessibility.getFullAXTree"))!.Value;

        var exposedNames = tree.GetProperty("nodes").EnumerateArray()
            .Where(node => !node.GetProperty("ignored").GetBoolean() && node.TryGetProperty("name", out _))
            .Select(node => node.GetProperty("name").GetProperty("value").ToString())
            .ToList();
        Assert.Contains("Modal first", exposedNames);
        Assert.DoesNotContain("Page input", exposedNames);
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Modal_ShouldLockPageScroll_WhenOpen(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");

        await page.Mouse.MoveAsync(20, 600);
        await page.Mouse.WheelAsync(0, 800);
        await WaitForFramesAsync(page);

        Assert.Equal(0, await page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Modal_ShouldReturnFocusToOpener_WhenClosedWithEscape(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("modal")).Not.ToHaveAttributeAsync("open", "");
        await Expect(page.GetByTestId("open-modal")).ToBeFocusedAsync();
    }

    // The planned design, cancelling only the cancel event: records the engine behavior that rules it out.
    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Modal_ShouldClose_WhenEscapeIsPressedTwiceWithOnlyTheCancelEventPrevented(string browser,
        string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays?noEscape=cancel");

        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("modal")).Not.ToHaveAttributeAsync("open", "");
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Modal_ShouldStayOpen_WhenEscapeIsPressedTwiceWithTheKeyBlocked(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays?noEscape=keydown");

        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Escape");
        await WaitForFramesAsync(page);

        await Expect(page.GetByTestId("modal")).ToHaveAttributeAsync("open", "");
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Modal_ShouldStayOpen_WhenEscapeIsPressedWithTheKeyBlockedAndFocusOnTheBody(string browser,
        string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays?noEscape=keydown");
        await page.EvaluateAsync("() => document.activeElement.blur()");

        await page.Keyboard.PressAsync("Escape");
        await page.Keyboard.PressAsync("Escape");
        await WaitForFramesAsync(page);

        await Expect(page.GetByTestId("modal")).ToHaveAttributeAsync("open", "");
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task NestedModal_ShouldCloseOnlyItself_WhenEscapeIsPressed(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");
        await page.GetByTestId("open-nested").PressAsync("Enter");
        await Expect(page.GetByTestId("nested")).ToHaveAttributeAsync("open", "");

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("nested")).Not.ToHaveAttributeAsync("open", "");
        await Expect(page.GetByTestId("modal")).ToHaveAttributeAsync("open", "");
        await Expect(page.GetByTestId("open-nested")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task NonModal_ShouldKeepThePageUsable_WhenTwoAreOpen(string browser, string mode)
    {
        var page = await OpenAsync(browser, mode, "/overlays");
        await page.GetByTestId("open-nonmodal-1").ClickAsync();
        await page.GetByTestId("open-nonmodal-2").ClickAsync();
        await Expect(page.GetByTestId("nonmodal-1")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("nonmodal-2")).ToBeVisibleAsync();

        await page.GetByTestId("page-input").FillAsync("typed");

        await Expect(page.GetByTestId("page-input")).ToBeFocusedAsync();
        await Expect(page.GetByTestId("page-input")).ToHaveValueAsync("typed");
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task NonModal_ShouldRenderAboveAHostileAncestorAndCover_WhenOpen(string browser, string mode)
    {
        var page = await OpenAsync(browser, mode, "/overlays");

        await page.GetByTestId("open-nonmodal-1").ClickAsync();

        await Expect(page.GetByTestId("nonmodal-1")).ToBeVisibleAsync();
        Assert.True(await IsTopmostAtCenterAsync(page, "nonmodal-1"));
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Menu_ShouldRenderAboveTheModalAndWork_WhenOpenedInsideIt(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");

        await page.GetByTestId("open-menu").ClickAsync();
        await Expect(page.GetByTestId("modal-menu")).ToBeVisibleAsync();

        Assert.True(await IsTopmostAtCenterAsync(page, "modal-menu"));
        await page.GetByTestId("menu-item").ClickAsync();
        await Expect(page.GetByTestId("log")).ToContainTextAsync("menu-item");
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Menu_ShouldCloseAndTellDotNetButLeaveTheModalOpen_WhenEscapeIsPressed(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");
        await page.GetByTestId("open-menu").ClickAsync();
        await Expect(page.GetByTestId("modal-menu")).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("Escape");

        await Expect(page.GetByTestId("modal-menu")).ToBeHiddenAsync();
        await Expect(page.GetByTestId("modal")).ToHaveAttributeAsync("open", "");
        await Expect(page.GetByTestId("log")).ToContainTextAsync("menu-closed");
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Menu_ShouldCloseButLeaveTheModalOpen_WhenPressingOutsideTheMenu(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");
        await page.GetByTestId("open-menu").ClickAsync();
        await Expect(page.GetByTestId("modal-menu")).ToBeVisibleAsync();

        await page.GetByTestId("modal-first").ClickAsync();

        await Expect(page.GetByTestId("modal-menu")).ToBeHiddenAsync();
        await Expect(page.GetByTestId("modal")).ToHaveAttributeAsync("open", "");
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Popover_ShouldInheritAncestorCustomProperties_WhenOpen(string browser, string mode)
    {
        var page = await OpenAsync(browser, mode, "/overlays");

        await page.GetByTestId("open-themed").ClickAsync();

        await Expect(page.GetByTestId("themed-popover")).ToHaveCSSAsync("color", "rgb(0, 128, 128)");
    }

    [Theory, MemberData(nameof(ModesOnly))]
    public async Task Prerender_ShouldIncludeClosedOverlays_WhenThePageIsRequested(string mode)
    {
        using var client = new HttpClient { BaseAddress = Fixture.BaseAddress };

        var html = await client.GetStringAsync(mode == "wasm" ? "/overlays?mode=wasm" : "/overlays",
            TestContext.Current.CancellationToken);

        Assert.Contains("""data-testid="modal" """, html);
        Assert.DoesNotMatch("""<dialog[^>]*\sopen[\s=>]""", html);
    }

    [Theory, ClassData(typeof(BrowsersAndModes))]
    public async Task Overlays_ShouldRaiseNoScriptErrors_WhenUsed(string browser, string mode)
    {
        var page = await OpenModalAsync(browser, mode, "/overlays");
        await page.GetByTestId("open-menu").ClickAsync();
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("modal-menu")).ToBeHiddenAsync();
        await page.GetByTestId("open-nested").PressAsync("Enter");
        await Expect(page.GetByTestId("nested")).ToHaveAttributeAsync("open", "");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("nested")).Not.ToHaveAttributeAsync("open", "");
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("modal")).Not.ToHaveAttributeAsync("open", "");
        await page.GetByTestId("open-nonmodal-1").ClickAsync();
        await page.GetByTestId("open-nonmodal-2").ClickAsync();
        await Expect(page.GetByTestId("nonmodal-2")).ToBeVisibleAsync();

        Assert.Empty(ScriptErrors);
    }

    public static TheoryData<string> ModesOnly => new(BrowsersAndModes.Modes);

    private async Task<IPage> OpenModalAsync(string browser, string mode, string path)
    {
        var page = await OpenAsync(browser, mode, path);
        await page.GetByTestId("open-modal").PressAsync("Enter");
        await Expect(page.GetByTestId("modal")).ToHaveAttributeAsync("open", "");
        return page;
    }

    private static Task<bool> IsFocusInsideAsync(IPage page, string testId) =>
        page.EvaluateAsync<bool>(
            "testId => document.querySelector(`[data-testid=${testId}]`).contains(document.activeElement)", testId);
}
