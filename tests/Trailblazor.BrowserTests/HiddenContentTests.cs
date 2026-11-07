namespace Trailblazor.BrowserTests;

public class HiddenContentTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/hidden-content");
        await Expect(page.Locator(".tb-splitter")).ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HiddenContent_ShouldHideAnOpenAccordionsPanel_WhenLoadingOrInACollapsedPane(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await Expect(Button(page, "Inside loading")).ToBeHiddenAsync();
        await Expect(Button(page, "Inside the side")).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HiddenContent_ShouldTakeNoClicks_WhenItIsLoading(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var shown = Button(page, "Shown while loading");
        var box = (await shown.BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        Assert.Null(await shown.GetAttributeAsync("data-clicked"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task HiddenContent_ShouldBeSkippedByTab_WhenLoadingOrInACollapsedPane(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await Button(page, "Before").FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Separator, new() { Name = "Resize Side" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Button(page, "After")).ToBeFocusedAsync();
    }
}
