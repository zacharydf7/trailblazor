using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class InsertDividerTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Insert(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static Task<string> DisabledOpacityAsync(IPage page) => page.EvaluateAsync<string>("""
        () => getComputedStyle(document.documentElement).getPropertyValue('--tb-disabled-opacity').trim()
        """);

    // The line's opacity, then the button's.
    private static Task<string[]> OpacitiesAsync(ILocator button) => button.EvaluateAsync<string[]>("""
        button => [getComputedStyle(button.closest('.tb-insert-divider'), '::before').opacity,
            getComputedStyle(button).opacity]
        """);

    private static async Task SkipOnTouchScreenAsync(IPage page) => Assert.SkipWhen(
        await page.EvaluateAsync<bool>("() => matchMedia('(any-pointer: coarse)').matches"),
        "This machine has a touch screen, so the button is always shown.");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InsertDivider_ShouldShowItsButtonOnlyOnHoverOrFocus_WhenThePointerCanHover(string browser)
    {
        var page = await OpenAsync(browser, "/examples/insert-divider");
        var button = Insert(page, "Add a step after Pack the tent");
        await SkipOnTouchScreenAsync(page);
        await Expect(button).ToHaveCSSAsync("opacity", "0");

        await button.HoverAsync();
        await Expect(button).ToHaveCSSAsync("opacity", "1");
        await page.Mouse.MoveAsync(1, 1);
        await Expect(button).ToHaveCSSAsync("opacity", "0");

        await button.FocusAsync();
        await Expect(button).ToHaveCSSAsync("opacity", "1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InsertDivider_ShouldHideOnceThePointerLeaves_WhenItsButtonWasClicked(string browser)
    {
        var page = await OpenAsync(browser, "/insert-dividers");
        var button = Insert(page, "Add here");
        await SkipOnTouchScreenAsync(page);

        await button.ClickAsync();
        await page.Mouse.MoveAsync(1, 1);

        // The click's hover starts a fade-in from 0, so a divider left lit could read as hidden until the fades settle.
        await WaitForFadesAsync(page);
        var shown = await OpacitiesAsync(button);
        Assert.Equal(["0", "0"], shown);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InsertDivider_ShouldHideADisabledButtonAtRestAndDimItOnHover_WhenDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/insert-dividers");
        var button = Insert(page, "Can't add here");
        await SkipOnTouchScreenAsync(page);
        await Expect(button).ToHaveCSSAsync("opacity", "0");
        var disabled = await DisabledOpacityAsync(page);

        await button.HoverAsync(new() { Force = true });

        await WaitForFadesAsync(page);
        var shown = await OpacitiesAsync(button);
        Assert.Equal([disabled, disabled], shown);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InsertDivider_ShouldShowItsLineAndButtonAtRest_WhenAlwaysVisible(string browser)
    {
        var page = await OpenAsync(browser, "/examples/insert-divider");
        var button = Insert(page, "Add a step at the start");

        await page.Mouse.MoveAsync(1, 1);

        await WaitForFadesAsync(page);
        var shown = await OpacitiesAsync(button);
        Assert.Equal(["1", "1"], shown);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task InsertDivider_ShouldShowADisabledButtonDimmedAtRest_WhenAlwaysVisible(string browser)
    {
        var page = await OpenAsync(browser, "/insert-dividers");
        var disabled = await DisabledOpacityAsync(page);

        await page.Mouse.MoveAsync(1, 1);

        await WaitForFadesAsync(page);
        var shown = await OpacitiesAsync(Insert(page, "Can't add here yet"));
        Assert.Equal([disabled, disabled], shown);
    }

    // Only Chromium emulates a phone, whose pointer can't hover.
    [Fact]
    public async Task InsertDivider_ShouldKeepItsButtonAndLineInView_WhenThePointerCannotHover()
    {
        var page = await OpenAsync("chromium", "/examples/insert-divider", new() { IsMobile = true, HasTouch = true });
        var insert = Insert(page, "Add a step after Pack the tent");

        await Expect(insert).ToHaveCSSAsync("opacity", "1");
        Assert.Equal("1", await insert.EvaluateAsync<string>(
            "button => getComputedStyle(button.closest('.tb-insert-divider'), '::before').opacity"));
    }

    // Only Chromium emulates a phone, whose pointer can't hover.
    [Fact]
    public async Task InsertDivider_ShouldShowADisabledButtonDimmed_WhenThePointerCannotHover()
    {
        var page = await OpenAsync("chromium", "/insert-dividers", new() { IsMobile = true, HasTouch = true });

        var insert = Insert(page, "Can't add here");
        var dimmed = await DisabledOpacityAsync(page);

        await Expect(insert).ToHaveCSSAsync("opacity", dimmed);
        Assert.Equal(dimmed, await insert.EvaluateAsync<string>(
            "button => getComputedStyle(button.closest('.tb-insert-divider'), '::before').opacity"));
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
    public async Task Helper_ShouldAddTheItemInItsPlace_WhenInserted(string browser)
    {
        var page = await OpenAsync(browser, "/examples/insert-divider");
        var steps = page.GetByRole(AriaRole.List, new() { Name = "Trip checklist" }).GetByRole(AriaRole.Listitem);

        await page.Tb().InsertDivider("Add a step after Pack the tent").InsertAsync();

        await Expect(steps).ToHaveTextAsync(
            ["Pack the tent", "New step 1", "Fill the water bottles", "Check the trail map"]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldRefuseADisabledDivider_WhenAskedToInsert(string browser)
    {
        var page = await OpenAsync(browser, "/insert-dividers");

        var disabled = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().InsertDivider("Can't add here").InsertAsync());

        Assert.Contains("disabled", disabled.Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldInsertOnceInteractive_WhenThePageIsStillPrerendered(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/examples/insert-divider");

        var inserting = page.Tb().InsertDivider("Add a step at the start").InsertAsync();
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await inserting;

        await Expect(page.GetByRole(AriaRole.List, new() { Name = "Trip checklist" }).GetByRole(AriaRole.Listitem)
            .First).ToHaveTextAsync("New step 1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldRefuseToInsert_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-insert-divider");

        var still = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.Tb().InsertDivider("Add a step").InsertAsync());

        Assert.Contains("static page", still.Message);
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
