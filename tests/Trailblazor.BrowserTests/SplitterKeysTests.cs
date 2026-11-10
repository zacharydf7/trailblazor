using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// Keyboard tests and shared helpers for SplitterTests; the slow-link class reruns only these.
public abstract class SplitterKeysTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    protected static ILocator Separator(IPage page, string name) =>
        page.GetByRole(AriaRole.Separator, new() { Name = name, Exact = true });

    protected async Task<IPage> OpenSplitterAsync(string browser, string path = "/examples/splitter",
        BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, path, options);
        foreach (var splitter in await page.Locator(".tb-splitter").AllAsync())
        {
            await Expect(splitter).ToHaveAttributeAsync("data-ready", "");
        }

        return page;
    }

    // Collapses a pane from a button inside it, then checks focus lands on the separator and Enter there brings it back.
    private async Task CollapseFromInsideAsync(string browser, string button, string separator)
    {
        var page = await OpenSplitterAsync(browser, "/splitter-focus");
        var collapse = page.GetByRole(AriaRole.Button, new() { Name = button });
        await collapse.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(collapse).ToBeHiddenAsync();
        await Expect(Separator(page, separator)).ToBeFocusedAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(collapse).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldLeaveTheKeysToTheBrowser_WhenAltCtrlOrCmdIsHeld(string browser)
    {
        var page = await OpenSplitterAsync(browser);
        var trails = Separator(page, "Resize Trails");
        await trails.FocusAsync();

        foreach (var key in new[] { "Alt+ArrowRight", "Control+ArrowRight", "Meta+ArrowRight", "Control+End" })
        {
            await page.Keyboard.PressAsync(key);
        }

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "26");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldResizeWithinTheLimits_WhenTheKeysAreUsed(string browser)
    {
        var page = await OpenSplitterAsync(browser);
        var trails = Separator(page, "Resize Trails");
        await trails.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "26");
        await page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "36");
        await page.Keyboard.PressAsync("End");
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "40");
        await page.Keyboard.PressAsync("Home");

        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "15");
        await Expect(page.GetByText("Trails 15%")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldKeepACollapsedPaneShut_WhenAnotherSeparatorMovesWhileSizesAreOnTheirWay(
        string browser)
    {
        var page = await OpenSplitterAsync(browser, "/splitter-three");
        await Separator(page, "Resize Left").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("Enter");
        var collapsed = page.Locator("[data-part=pane][data-collapsed]");
        await Expect(collapsed).ToHaveCountAsync(1);

        await Separator(page, "Resize Middle").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");

        await Expect(page.Locator(".tb-splitter[data-reporting]")).ToHaveCountAsync(0);
        Assert.Equal("0", await collapsed.EvaluateAsync<string>("pane => pane.style.flexGrow"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldCollapseAndRestoreThePane_WhenEnterIsPressed(string browser)
    {
        var page = await OpenSplitterAsync(browser);
        var trails = Separator(page, "Resize Trails");
        await trails.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByText("Ridge loop, creek path")).ToBeHiddenAsync();
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "0");
        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByText("Ridge loop, creek path")).ToBeVisibleAsync();
        await Expect(trails).ToHaveAttributeAsync("aria-valuenow", "25");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldCollapseTheNextPane_WhenThePaneBeforeTheSeparatorCannot(string browser)
    {
        var page = await OpenSplitterAsync(browser);
        await Separator(page, "Resize Map").FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(page.GetByText("Distance, climb and water stops.")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldResizeTheNestedPanes_WhenTheirSeparatorUsesUpAndDown(string browser)
    {
        var page = await OpenSplitterAsync(browser);
        var map = Separator(page, "Resize Map view");
        await Expect(map).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await map.FocusAsync();

        await page.Keyboard.PressAsync("ArrowDown");

        await Expect(map).ToHaveAttributeAsync("aria-valuenow", "66");
        await Expect(Separator(page, "Resize Trails")).ToHaveAttributeAsync("aria-valuenow", "25");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldGrowThePaneWithTheLeftArrow_WhenTheTextIsRightToLeft(string browser)
    {
        var page = await OpenSplitterAsync(browser);
        await page.Locator(".tb-splitter").First.EvaluateAsync("splitter => splitter.dir = 'rtl'");
        await Separator(page, "Resize Trails").FocusAsync();

        await page.Keyboard.PressAsync("ArrowLeft");

        await Expect(Separator(page, "Resize Trails")).ToHaveAttributeAsync("aria-valuenow", "26");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldKeepTheLayout_WhenThePageIsReloadedWithAPersistenceKey(string browser)
    {
        var page = await OpenSplitterAsync(browser, "/splitter");
        var left = Separator(page, "Resize Left");
        await left.FocusAsync();
        await page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(left).ToHaveAttributeAsync("aria-valuenow", "40");
        // The layout is saved once the server has the new sizes, a round trip after the separator moves.
        await page.WaitForFunctionAsync("() => localStorage.getItem('tb-splitter:splitter-fixture')?.includes('[40,')");

        await page.ReloadAsync();
        await Expect(page.Locator(".tb-splitter")).ToHaveAttributeAsync("data-ready", "");

        await Expect(Separator(page, "Resize Left")).ToHaveAttributeAsync("aria-valuenow", "40");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldLeaveFocusAlone_WhenAnotherPaneCollapses(string browser)
    {
        var page = await OpenSplitterAsync(browser, "/splitter-focus");
        var button = page.GetByRole(AriaRole.Button, new() { Name = "Collapse its neighbor" });
        await button.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Collapse left" })).ToBeHiddenAsync();

        await Expect(button).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldDrawTheFocusedSeparatorInTheHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenSplitterAsync(browser, options: new() { ForcedColors = ForcedColors.Active });
        var separator = Separator(page, "Resize Trails");
        var line = () => separator.EvaluateAsync<string>("line => getComputedStyle(line, '::before').backgroundColor");
        Assert.Equal(await SystemColorAsync(page, "CanvasText"), await line());

        await separator.FocusAsync();
        // A key press puts the browser in keyboard mode, so :focus-visible matches after focus from a script.
        await page.Keyboard.PressAsync("ArrowRight");
        // The line's color eases in, so it is read once the transition ends.
        await WaitForFadesAsync(page);

        Assert.Equal(await SystemColorAsync(page, "Highlight"), await line());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ResizeAsync_ShouldMoveTheSeparatorWithItsKeys_WhenCalled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/splitter");
        var trails = page.Tb().Splitter("Resize Trails");

        await trails.ResizeAsync(37);
        await trails.ResizeAsync(19);

        Assert.Equal(19, await trails.SizeAsync());
        await Expect(page.GetByText("Trails 19%")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldMoveFocusToTheSeparatorAfterThePane_WhenCollapsedFromInsideIt(string browser) =>
        await CollapseFromInsideAsync(browser, "Collapse left", "Resize Left");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldMoveFocusToTheSeparatorAfterIt_WhenAMiddlePaneCollapsesFromInside(string browser) =>
        await CollapseFromInsideAsync(browser, "Collapse between", "Resize Between");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldMoveFocusToTheSeparatorBeforeTheLastPane_WhenCollapsedFromInsideIt(string browser) =>
        await CollapseFromInsideAsync(browser, "Collapse right", "Resize Middle");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Splitter_ShouldRestoreTheLastPane_WhenThePaneBeforeItCanCollapseToo(string browser) =>
        await CollapseFromInsideAsync(browser, "Collapse last", "Resize Between");
}
