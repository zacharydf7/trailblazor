namespace Trailblazor.BrowserTests.Primitives;

public class DismissableLayerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EscapeKey_ShouldDismissLayer_WhenLayerIsOpen(string browser)
    {
        var page = await OpenOuterLayerAsync(browser);

        await page.Keyboard.PressAsync("Escape");

        await ExpectLogAsync(page, "outer:EscapeKey");
        await Expect(page.GetByTestId("outer-layer")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PointerDownOutside_ShouldDismissLayerOnce_WhenPressMovesFocusOutsideToo(string browser)
    {
        var page = await OpenOuterLayerAsync(browser);
        await page.GetByTestId("outer-input").FocusAsync();

        await page.GetByTestId("outside").ClickAsync();

        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("0");
        await ExpectLogAsync(page, "outer:PointerDownOutside");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task FocusOutside_ShouldDismissLayer_WhenFocusMovesOutside(string browser)
    {
        var page = await OpenOuterLayerAsync(browser);
        await page.GetByTestId("outer-input").FocusAsync();

        await page.GetByTestId("outside").FocusAsync();

        await ExpectLogAsync(page, "outer:FocusOutside");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PointerDown_ShouldNotDismissLayer_WhenPressingInsideLayer(string browser)
    {
        var page = await OpenOuterLayerAsync(browser);

        await page.GetByTestId("outer-input").ClickAsync();
        await page.Keyboard.PressAsync("Escape");

        await ExpectLogAsync(page, "outer:EscapeKey");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PointerDown_ShouldNotDismissLayer_WhenPressingTrigger(string browser)
    {
        var page = await OpenOuterLayerAsync(browser);

        await page.GetByTestId("outer-trigger").ClickAsync();

        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("0");
        await ExpectLogAsync(page);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EscapeKey_ShouldDismissOnlyTopmostLayer_WhenLayersAreNested(string browser)
    {
        var page = await OpenInnerLayerAsync(browser);

        await page.Keyboard.PressAsync("Escape");

        await ExpectLogAsync(page, "inner:EscapeKey");
        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("1");
        await Expect(page.GetByTestId("outer-layer")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PointerDownOutside_ShouldDismissOnlyInnerLayer_WhenPressingInsideOuterLayer(string browser)
    {
        var page = await OpenInnerLayerAsync(browser);

        await page.GetByTestId("outer-input").ClickAsync();

        await ExpectLogAsync(page, "inner:PointerDownOutside");
        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EscapeKey_ShouldDismissOuterLayer_WhenInnerLayerWasDismissedFirst(string browser)
    {
        var page = await OpenInnerLayerAsync(browser);
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("1");

        await page.Keyboard.PressAsync("Escape");

        await ExpectLogAsync(page, "inner:EscapeKey", "outer:EscapeKey");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task EscapeKey_ShouldLeaveNativeDialogOpen_WhenLayerInsideDialogIsDismissed(string browser)
    {
        var page = await OpenAsync(browser, "/dismissable-layer-in-dialog");
        var dialog = page.GetByTestId("dialog");
        await page.GetByTestId("open-dialog").ClickAsync();
        await page.GetByTestId("trigger").ClickAsync();
        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("1");

        await page.Keyboard.PressAsync("Escape");

        await ExpectLogAsync(page, "layer:EscapeKey");
        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("0");
        await Expect(dialog).ToHaveAttributeAsync("open", "");

        // With no layer left, the same key closes the dialog, so the check above is not passing by accident.
        await page.Keyboard.PressAsync("Escape");
        await Expect(dialog).Not.ToHaveAttributeAsync("open", "");
    }

    private async Task<IPage> OpenOuterLayerAsync(string browser)
    {
        var page = await OpenAsync(browser, "/dismissable-layer");
        await page.GetByTestId("outer-trigger").ClickAsync();
        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("1");
        return page;
    }

    private async Task<IPage> OpenInnerLayerAsync(string browser)
    {
        var page = await OpenOuterLayerAsync(browser);
        await page.GetByTestId("inner-trigger").ClickAsync();
        await Expect(page.GetByTestId("layer-count")).ToHaveTextAsync("2");
        return page;
    }

    private static Task ExpectLogAsync(IPage page, params string[] entries) =>
        Expect(page.GetByTestId("log").Locator("li")).ToHaveTextAsync(entries);
}
