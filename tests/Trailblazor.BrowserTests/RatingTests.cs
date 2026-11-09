namespace Trailblazor.BrowserTests;

public class RatingTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Ridge(IPage page) =>
        page.GetByRole(AriaRole.Radiogroup, new() { Name = "Rate the ridge loop" });

    private static Task<string> ColorOfStarAsync(IPage page, int star) => Ridge(page).Locator("[data-part=star]")
        .Nth(star - 1).EvaluateAsync<string>("star => getComputedStyle(star).color");

    private static Task<string[]> FillsAsync(IPage page) => Ridge(page).Locator("svg")
        .EvaluateAllAsync<string[]>("icons => icons.map(icon => getComputedStyle(icon).fill)");

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rating_ShouldChooseTheNextStar_WhenTheRightArrowIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating");
        // WebKit doesn't focus a radio it clicks, so focus is put on it first.
        var two = Ridge(page).GetByRole(AriaRole.Radio, new() { Name = "2 stars" });
        await two.CheckAsync();
        await two.FocusAsync();

        await page.Keyboard.PressAsync("ArrowRight");

        var three = Ridge(page).GetByRole(AriaRole.Radio, new() { Name = "3 stars" });
        await Expect(three).ToBeCheckedAsync();
        await Expect(three).ToBeFocusedAsync();
        await Expect(page.GetByText("You gave it 3 of 5.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rating_ShouldTakeTheRatingBack_WhenTheChosenStarIsClickedAgain(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating");
        var four = Ridge(page).GetByRole(AriaRole.Radio, new() { Name = "4 stars" });
        await four.CheckAsync();
        await Expect(page.GetByText("You gave it 4 of 5.")).ToBeVisibleAsync();

        await four.ClickAsync();

        await Expect(page.GetByText("Not rated yet.")).ToBeVisibleAsync();
        await Expect(four).Not.ToBeCheckedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rating_ShouldTakeTheRatingBack_WhenSpaceIsPressedOnTheChosenStar(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating");
        var two = Ridge(page).GetByRole(AriaRole.Radio, new() { Name = "2 stars" });
        await two.CheckAsync();
        await Expect(page.GetByText("You gave it 2 of 5.")).ToBeVisibleAsync();
        await two.FocusAsync();

        // Held as long as a person holds it, so the page can re-render between keydown and keyup.
        await page.Keyboard.PressAsync("Space", new() { Delay = 150 });

        await Expect(page.GetByText("Not rated yet.")).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rating_ShouldFillTheChosenStarsAndOutlineTheRest_WhenRendered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating");
        var difficulty = page.GetByRole(AriaRole.Radiogroup, new() { Name = "Difficulty" }).Locator("svg");

        var fills = await difficulty
            .EvaluateAllAsync<string[]>("icons => icons.map(icon => getComputedStyle(icon).fill)");

        Assert.NotEqual("none", fills[0]);
        Assert.Equal(["none"], fills[3..]);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rating_ShouldPreviewTheRatingAStarWouldGive_WhenPointedAt(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating");
        var off = await ColorOfStarAsync(page, 4);

        await Ridge(page).Locator("[data-part=star]").Nth(3).HoverAsync();

        var on = await ColorOfStarAsync(page, 4);
        Assert.NotEqual(off, on);
        Assert.Equal(on, await ColorOfStarAsync(page, 1));
        Assert.Equal(off, await ColorOfStarAsync(page, 5));
        Assert.Equal([false, false, false, false, true], (await FillsAsync(page)).Select(fill => fill == "none"));
    }

    // Checking a radio from script raises no change event, so the page doesn't re-render, just like a choice made
    // before it is interactive.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rating_ShouldShowTheChosenStarsFromItsRadios_WhenThePageHasNotRenderedTheChoice(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating");

        await Ridge(page).GetByRole(AriaRole.Radio, new() { Name = "4 stars" })
            .EvaluateAsync("radio => radio.checked = true");

        Assert.Equal([false, false, false, false, true], (await FillsAsync(page)).Select(fill => fill == "none"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rating_ShouldRingTheFocusedStar_WhenItHasKeyboardFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating");

        // The rating is the page's first tab stop, and with no star chosen, Tab lands on the first.
        await page.Keyboard.PressAsync("Tab");

        await Expect(Ridge(page).GetByRole(AriaRole.Radio, new() { Name = "1 star" })).ToBeFocusedAsync();
        await Expect(Ridge(page).Locator("[data-part=star]").First).ToHaveCSSAsync("outline-style", "solid");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Rating_ShouldHighlightTheChosenStars_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/rating", new() { ForcedColors = ForcedColors.Active });
        var highlight = await SystemColorAsync(page, "Highlight");
        var difficulty = page.GetByRole(AriaRole.Radiogroup, new() { Name = "Difficulty" }).Locator("[data-part=star]");

        await Expect(difficulty.Nth(0)).ToHaveCSSAsync("color", highlight);
        await Expect(difficulty.Nth(3)).Not.ToHaveCSSAsync("color", highlight);
    }
}
