namespace Trailblazor.BrowserTests;

public class StepperTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string TitleUnderNumber = @"steps => steps.map(step =>
        step.querySelector('[data-part=title]').getBoundingClientRect().top
            >= step.querySelector('[data-part=indicator]').getBoundingClientRect().bottom)";

    private static ILocator Booking(IPage page) => page.GetByRole(AriaRole.List, new() { Name = "Trip booking" });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Stepper_ShouldGoBackToAFinishedStepAndKeepFocusOnIt_WhenEnterIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/stepper");
        await page.GetByRole(AriaRole.Button, new() { Name = "Next" }).ClickAsync();
        var trip = Booking(page).GetByRole(AriaRole.Button, new() { Name = "Trip Completed" });

        await trip.FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(trip).ToHaveAttributeAsync("aria-current", "step");
        await Expect(trip).ToBeFocusedAsync();
        await Expect(page.GetByText("Pick the trail and the dates.")).ToBeVisibleAsync();
    }

    // The Back button is off on the first step, so the next tab stop after the steps is Next.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Stepper_ShouldSkipTheStepsAheadWithTab_WhenItIsLinear(string browser)
    {
        var page = await OpenAsync(browser, "/examples/stepper");

        await Booking(page).GetByRole(AriaRole.Button, new() { Name = "Trip" }).FocusAsync();
        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Next" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Stepper_ShouldPutEachTitleUnderItsNumber_WhenTheScreenIsAPhone(string browser)
    {
        var page = await OpenAsync(browser, "/examples/stepper",
            new() { ViewportSize = new() { Width = 390, Height = 800 } });

        var under = await Booking(page).Locator("[data-part=step]").EvaluateAllAsync<bool[]>(TitleUnderNumber);

        Assert.Equal([true, true, true], under);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Stepper_ShouldPutEachTitleUnderItsNumber_WhenSixStepsShareAMidWidth(string browser)
    {
        var page = await OpenAsync(browser, "/stepper-layouts");

        var under = await page.GetByRole(AriaRole.List, new() { Name = "Six steps" }).Locator("[data-part=step]")
            .EvaluateAllAsync<bool[]>(TitleUnderNumber);

        Assert.Equal(Enumerable.Repeat(true, 6), under);
    }

    // Squeezed by a long paragraph beside it, a title may wrap between its words but never inside one.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Stepper_ShouldBreakTitlesOnlyBetweenWords_WhenVerticalInARow(string browser)
    {
        var page = await OpenAsync(browser, "/stepper-layouts");

        var lines = await page.GetByRole(AriaRole.List, new() { Name = "In a row" }).GetByText("Trail alerts")
            .EvaluateAsync<double>("title => title.getBoundingClientRect().height "
                + "/ parseFloat(getComputedStyle(title).lineHeight)");

        Assert.InRange(Math.Round(lines), 1, 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Stepper_ShouldHighlightTheCurrentStepAndKeepTheLines_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/stepper", new() { ForcedColors = ForcedColors.Active });
        var highlight = await page.EvaluateAsync<string>(@"() => {
            const probe = document.body.appendChild(document.createElement('div'));
            probe.style.cssText = 'background: Highlight; forced-color-adjust: none';
            return getComputedStyle(probe).backgroundColor; }");

        var text = await page.EvaluateAsync<string>(@"() => {
            const probe = document.body.appendChild(document.createElement('div'));
            probe.style.cssText = 'background: CanvasText; forced-color-adjust: none';
            return getComputedStyle(probe).backgroundColor; }");

        await Expect(Booking(page).Locator("[aria-current=step] [data-part=indicator]"))
            .ToHaveCSSAsync("background-color", highlight);
        Assert.Equal(text, await Booking(page).Locator(".tb-stepper__header").First
            .EvaluateAsync<string>("header => getComputedStyle(header, '::after').backgroundColor"));
    }
}
