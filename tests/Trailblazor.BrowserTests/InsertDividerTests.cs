namespace Trailblazor.BrowserTests;

public class InsertDividerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Insert(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InsertDivider_ShouldShowItsButtonOnlyOnHoverOrFocus_WhenThePointerCanHover(string browser)
    {
        var page = await OpenAsync(browser, "/examples/insert-divider");
        var button = Insert(page, "Add a step after Pack the tent");
        Assert.SkipWhen(await page.EvaluateAsync<bool>("() => matchMedia('(any-pointer: coarse)').matches"),
            "This machine has a touch screen, so the button is always shown.");
        await Expect(button).ToHaveCSSAsync("opacity", "0");

        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("opacity", "1");
        await page.Mouse.MoveAsync(1, 1);
        await Expect(button).ToHaveCSSAsync("opacity", "0");

        await button.FocusAsync();
        await Expect(button).ToHaveCSSAsync("opacity", "1");
    }

    // Only Chromium emulates a phone, whose pointer can't hover.
    [Fact]
    public async Task InsertDivider_ShouldKeepItsButtonInView_WhenThePointerCannotHover()
    {
        var page = await OpenAsync("chromium", "/examples/insert-divider", new() { IsMobile = true, HasTouch = true });

        await Expect(Insert(page, "Add a step after Pack the tent")).ToHaveCSSAsync("opacity", "1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InsertDivider_ShouldAddTheItemInItsPlace_WhenItsButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/insert-divider");
        var steps = page.GetByRole(AriaRole.List, new() { Name = "Trip checklist" }).GetByRole(AriaRole.Listitem);

        await Insert(page, "Add a step after Pack the tent").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Insert(page, "Add a step at the start").ClickAsync();

        await Expect(steps).ToHaveTextAsync(
            ["New step 2", "Pack the tent", "New step 1", "Fill the water bottles", "Check the trail map"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InsertDivider_ShouldCenterItsLineOnTheButton_WhenShown(string browser)
    {
        var page = await OpenAsync(browser, "/examples/insert-divider");
        var button = Insert(page, "Add a step after Pack the tent");

        await button.HoverAsync();

        // The button's center and the line's top, both measured from the top of the divider.
        var offsets = await button.EvaluateAsync<double[]>("""
            button => {
                const divider = button.closest('.tb-insert-divider');
                const box = button.getBoundingClientRect();
                return [box.top + box.height / 2 - divider.getBoundingClientRect().top,
                    parseFloat(getComputedStyle(divider, '::before').top)];
            }
            """);
        Assert.Equal(offsets[0], offsets[1], tolerance: 2);
        var divider = (await page.Locator(".tb-insert-divider").First.BoundingBoxAsync())!;
        var start = (await Insert(page, "Add a step at the start").BoundingBoxAsync())!;
        Assert.Equal(divider.Height, start.Height, tolerance: 0.5);
    }
}
