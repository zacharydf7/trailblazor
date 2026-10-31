namespace Trailblazor.BrowserTests;

public class TextFieldTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldPostItsValue_WhenSubmittedInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form");

        await page.GetByLabel("Name").FillAsync("Ada");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("Ada, False, Free");
        await Expect(page.GetByLabel("Name")).ToHaveValueAsync("Ada");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowItsValidationMessage_WhenAStaticFormIsSubmittedEmpty(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-form");

        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByText("Enter a name.")).ToBeVisibleAsync();
        await Expect(page.GetByLabel("Name")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.GetByTestId("posted")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepWhatWasTypedAndExplain_WhenAStaticFormPostsTextThatDoesNotConvert(
        string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-attempted");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(age).ToHaveValueAsync("thirty");
        await Expect(age).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.Locator("[data-part=error]")).ToContainTextAsync("thirty");
        await Expect(page.GetByTestId("posted")).ToBeEmptyAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowTheSavedValue_WhenAPlainFormPostsTextThatDoesNotConvert(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-attempted-plain");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("0");
        await Expect(age).ToHaveValueAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowTheNewModelsValue_WhenTheFormStartsOverAfterTextThatDoesNotConvert(
        string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-attempted-reset");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("state")).ToHaveTextAsync("started over");
        await Expect(age).ToHaveValueAsync("18");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldUpdateWhileTyping_WhenUpdateOnInputIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");

        await page.GetByLabel("Search").PressSequentiallyAsync("tea");

        await Expect(page.GetByTestId("search-echo")).ToHaveTextAsync("tea");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldEmptyAndKeepFocus_WhenTheClearButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var search = page.GetByLabel("Search");
        await search.PressSequentiallyAsync("tea");
        await Expect(page.GetByTestId("search-echo")).ToHaveTextAsync("tea");

        await page.GetByTestId("search").GetByRole(AriaRole.Button, new() { Name = "Clear" }).ClickAsync();

        await Expect(page.GetByTestId("search-echo")).ToHaveTextAsync("");
        await Expect(search).ToHaveValueAsync("");
        await Expect(search).ToBeFocusedAsync();
        await Expect(page.GetByTestId("search").GetByRole(AriaRole.Button, new() { Name = "Clear" }))
            .ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldUpdateAtOnce_WhenEnterOrLeavingTheFieldEndsTheDelay(string browser)
    {
        var page = await OpenAsync(browser, "/delayed-search");
        var search = page.GetByLabel("Search");

        await search.PressSequentiallyAsync("tea");
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByTestId("search-echo")).ToHaveTextAsync("tea");

        await search.PressSequentiallyAsync("pot");
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByTestId("search-echo")).ToHaveTextAsync("teapot");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldGrowWithItsTextUpToMaxLinesThenScroll_WhenMaxLinesIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var notes = page.GetByLabel("Notes");
        // Playwright passes a float to the page as an empty object, so the height goes over as a double.
        double start = (await notes.BoundingBoxAsync())!.Height;

        await notes.FillAsync("1\n2\n3\n4\n5");
        await page.WaitForFunctionAsync(
            "start => document.querySelector('textarea[data-max-lines]').getBoundingClientRect().height > start",
            start);
        await notes.FillAsync(string.Join('\n', Enumerable.Range(1, 20)));
        await page.WaitForFunctionAsync(
            "() => (box => box.scrollHeight > box.clientHeight)(document.querySelector('textarea[data-max-lines]'))");

        var full = (await notes.BoundingBoxAsync())!.Height;
        Assert.InRange(full, start * 2, start * 8 / 3);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepEveryKeystroke_WhenEachUpdateIsSlowToComeBack(string browser)
    {
        // A lost keystroke depends on timing, so a few page loads make a regression fail reliably.
        const string text = "abcdefghijklmnopqrst";
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var page = await OpenAsync(browser, "/slow-typing");
            var search = page.GetByLabel("Search");

            await search.PressSequentiallyAsync(text);
            await Expect(page.GetByTestId("completed")).ToHaveTextAsync(text.Length.ToString());

            Assert.Equal(text, await search.InputValueAsync());
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldKeepTheTextAndExplain_WhenTheTextDoesNotParse(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var age = page.GetByLabel("Age");

        await age.FillAsync("thirty");
        await age.PressAsync("Tab");

        await Expect(age).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(age).ToHaveValueAsync("thirty");
        await Expect(age).ToHaveAccessibleDescriptionAsync("Age isn't valid.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShowTheParentsErrorUntilItClears_WhenTheParentSetsOne(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var handle = page.GetByLabel("Handle");

        await handle.PressSequentiallyAsync("robin");
        await page.GetByRole(AriaRole.Button, new() { Name = "Check the handle" }).ClickAsync();
        await Expect(handle).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(handle).ToBeFocusedAsync();
        await Expect(handle).ToHaveAccessibleDescriptionAsync(
            "Try robin, who already has an account. That handle is taken.");
        await Expect(page.Locator("[data-part=announcer] [role=status]")).ToContainTextAsync("That handle is taken.");

        await handle.PressSequentiallyAsync("2");

        await Expect(handle).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldSizeItsBoxAndText_WhenGivenASize(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        string[] fields = ["Trail code", "Name", "Destination"];

        var heights = await Task.WhenAll(fields.Select(field => BoxStyleAsync(page, field, "height")));
        var fonts = await Task.WhenAll(fields.Select(field =>
            page.GetByLabel(field).EvaluateAsync<string>("input => getComputedStyle(input).fontSize")));

        Assert.Equal(["32px", "40px", "48px"], heights);
        Assert.Equal(["14px", "16px", "18px"], fonts);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldShrinkASmallBoxWithTheDensity_WhenTheThemeIsCompact(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        await page.EvaluateAsync("() => document.documentElement.dataset.tbDensity = 'compact'");

        Assert.Equal("26px", await BoxStyleAsync(page, "Trail code", "height"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TextField_ShouldDrawATintedBoxWithOnlyItsBottomLine_WhenFilled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");

        Assert.Equal("rgba(0, 0, 0, 0)", await BoxStyleAsync(page, "Call sign", "borderTopColor"));
        Assert.Equal(await BoxStyleAsync(page, "Name", "borderBottomColor"),
            await BoxStyleAsync(page, "Call sign", "borderBottomColor"));
        Assert.Equal(await BoxStyleAsync(page, "Account", "backgroundColor"),
            await BoxStyleAsync(page, "Call sign", "backgroundColor"));
        Assert.NotEqual(await BoxStyleAsync(page, "Name", "backgroundColor"),
            await BoxStyleAsync(page, "Call sign", "backgroundColor"));
    }

    private static Task<string> BoxStyleAsync(IPage page, string label, string property) =>
        page.GetByLabel(label).EvaluateAsync<string>(
            "(input, property) => getComputedStyle(input.closest('.tb-field__control'))[property]", property);
}
