namespace Trailblazor.BrowserTests;

public class CarouselTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Featured(IPage page) =>
        page.GetByRole(AriaRole.Region, new() { Name = "Featured trails" });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldShowTheNextSlideAndReadItOut_WhenNextIsPressedFromTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel");
        var next = Featured(page).GetByRole(AriaRole.Button, new() { Name = "Next slide" });
        await next.FocusAsync();

        await page.Keyboard.PressAsync("Enter");

        await Expect(Featured(page).GetByRole(AriaRole.Group, new() { Name = "2 of 3" })).ToBeVisibleAsync();
        await Expect(Featured(page).GetByRole(AriaRole.Group, new() { Name = "1 of 3" })).ToBeHiddenAsync();
        await Expect(page.GetByText("Showing Cedar Flats.")).ToBeVisibleAsync();
        await Expect(next).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldTabThroughItsControlsInOrder_WhenTabbedInto(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel");
        await Featured(page).GetByRole(AriaRole.Button, new() { Name = "Previous slide" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(Featured(page).GetByRole(AriaRole.Button, new() { Name = "Next slide" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(Featured(page).GetByRole(AriaRole.Button, new() { Name = "Slide 1" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldPauseItsTurningSlides_WhenThePointerIsOverIt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel");
        var photos = page.GetByRole(AriaRole.Region, new() { Name = "Trail photos" });
        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "off");

        await photos.HoverAsync();

        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "polite");
        await page.Mouse.MoveAsync(0, 0);
        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "off");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldStartWithItsSlidesStill_WhenTheUserAsksForReducedMotion(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel", new() { ReducedMotion = ReducedMotion.Reduce });
        var photos = page.GetByRole(AriaRole.Region, new() { Name = "Trail photos" });

        await Expect(photos.GetByRole(AriaRole.Button, new() { Name = "Start slide rotation" })).ToBeVisibleAsync();
        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "polite");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldKeepFocusOnNext_WhenTheKeysReachTheLastSlideOfOneThatDoesntLoop(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel");
        var steps = page.GetByRole(AriaRole.Region, new() { Name = "Trip steps" });
        var next = steps.GetByRole(AriaRole.Button, new() { Name = "Next slide" });
        await next.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");

        await Expect(steps.GetByRole(AriaRole.Group, new() { Name = "Step 3, sign in" })).ToBeVisibleAsync();
        await Expect(next).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(next).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldStopItsSlidesAtTheStopButton_WhenTabbedInto(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel");
        var photos = page.GetByRole(AriaRole.Region, new() { Name = "Trail photos" });
        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "off");
        // A button just before the carousel, so one Tab moves focus into it.
        await photos.EvaluateAsync("section => { const before = document.createElement('button'); "
            + "before.textContent = 'Before'; section.before(before); before.focus(); }");

        await page.Keyboard.PressAsync("Tab");
        await page.Mouse.MoveAsync(0, 0);

        await Expect(photos.GetByRole(AriaRole.Button, new() { Name = "Start slide rotation" })).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "polite");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldKeepItsSlidesTurning_WhenAClickFocusesAButtonAndThePointerLeaves(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel");
        var photos = page.GetByRole(AriaRole.Region, new() { Name = "Trail photos" });
        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "off");

        await photos.GetByRole(AriaRole.Button, new() { Name = "Next slide" }).ClickAsync();
        await page.Locator("body").ClickAsync(new() { Position = new() { X = 1, Y = 1 } });

        await Expect(photos.GetByRole(AriaRole.Button, new() { Name = "Stop slide rotation" })).ToBeVisibleAsync();
        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "off");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldStopItsSlides_WhenTabMovesFocusOnFromAClickedButton(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel");
        var photos = page.GetByRole(AriaRole.Region, new() { Name = "Trail photos" });
        var next = photos.GetByRole(AriaRole.Button, new() { Name = "Next slide" });
        await Expect(photos.Locator("[data-part=slides]")).ToHaveAttributeAsync("aria-live", "off");
        await next.ClickAsync();
        // Safari doesn't focus a button when it's clicked, so focus it here as the other browsers' clicks do.
        await next.FocusAsync();

        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Mouse.MoveAsync(0, 0);

        await Expect(photos.GetByRole(AriaRole.Button, new() { Name = "Start slide rotation" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Carousel_ShouldMarkTheShownSlidesButtonInTheSystemHighlight_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/carousel", new() { ForcedColors = ForcedColors.Active });
        var picks = Featured(page).Locator("[data-part=pick]");

        var colors = await picks.EvaluateAllAsync<string[]>(
            "picks => picks.map(pick => getComputedStyle(pick, '::before').backgroundColor)");

        Assert.Equal(await SystemColorAsync(page, "Highlight"), colors[0]);
        Assert.NotEqual(colors[0], colors[1]);
    }
}
