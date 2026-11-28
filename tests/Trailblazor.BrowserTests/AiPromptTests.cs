using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class AiPromptTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Box(IPage page) =>
        page.GetByRole(AriaRole.Textbox, new() { Name = "Ask about the ridge trail" });

    private static ILocator Output(IPage page) => page.Locator("[data-part=outputs] > [data-part=output]").First;

    private async Task<IPage> OpenPromptAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/ai-prompt", options);
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Ask about the ridge trail" }))
            .ToHaveAttributeAsync("data-ready", "");
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Prompt_ShouldGenerateOnEnterAndStartALineOnShiftEnter_WhenTyping(string browser)
    {
        var page = await OpenPromptAsync(browser);
        var box = Box(page);

        await box.FillAsync("Which");
        await box.PressAsync("Shift+Enter");
        await box.PressSequentiallyAsync("trail");
        await Expect(box).ToHaveValueAsync("Which\ntrail");
        await box.PressAsync("Enter");

        await Expect(Output(page)).ToHaveAttributeAsync("data-status", "done");
        await Expect(Output(page).Locator("[data-part=prompt]")).ToHaveTextAsync("Which\ntrail");
        await Expect(box).ToHaveValueAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Response_ShouldKeepItsBorder_WhenColorsAreForced(string browser)
    {
        var page = await OpenPromptAsync(browser, new() { ForcedColors = ForcedColors.Active });

        await Box(page).FillAsync("How long is it");
        await Box(page).PressAsync("Enter");

        await Expect(Output(page)).ToHaveAttributeAsync("data-status", "done");
        await Expect(Output(page)).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(Output(page)).ToHaveCSSAsync("border-top-width", "1px");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Escape_ShouldStopTheResponse_WhenItStreams(string browser)
    {
        var page = await OpenPromptAsync(browser);
        var box = Box(page);

        await box.FillAsync("one two three four five six seven eight nine ten");
        await box.PressAsync("Enter");
        await Expect(Output(page).Locator("[data-part=text]")).ToContainTextAsync("You");
        await box.PressAsync("Escape");

        await Expect(Output(page)).ToHaveAttributeAsync("data-status", "stopped");
        await Expect(Output(page).Locator("[data-part=text]")).Not.ToContainTextAsync("ten");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Prompt_ShouldNotGenerate_WhenEnterEndsAComposition(string browser)
    {
        var page = await OpenPromptAsync(browser);
        var box = Box(page);
        await box.FillAsync("Kyoto");

        await box.EvaluateAsync("""
            box => box.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', isComposing: true, bubbles: true,
                cancelable: true }))
            """);
        // A round trip to the app, after any generate the Enter would have started.
        await page.GetByRole(AriaRole.Button, new() { Name = "How long is the loop?" }).ClickAsync();
        await Expect(box).ToHaveValueAsync("How long is the loop?");

        await Expect(Output(page)).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Suggestion_ShouldFillTheBoxAndFocusIt_WhenClicked(string browser)
    {
        var page = await OpenPromptAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "How long is the loop?" }).ClickAsync();

        await Expect(Box(page)).ToHaveValueAsync("How long is the loop?");
        await Expect(Box(page)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Generate_ShouldKeepFocus_WhenItTurnsIntoStopAndBack(string browser)
    {
        var page = await OpenPromptAsync(browser);
        await Box(page).FillAsync("Is it shaded?");
        var button = page.Locator("[data-part=composer] [data-part=generate]");

        await button.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(button).ToHaveTextAsync("Stop");
        await Expect(button).ToBeFocusedAsync();
        await Expect(Output(page)).ToHaveAttributeAsync("data-status", "done");

        await Expect(button).ToHaveTextAsync("Generate");
        await Expect(button).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldPromptRetryStopAndInsert_WhenUsed(string browser)
    {
        var page = await OpenPromptAsync(browser);
        var prompt = page.Tb().AiPrompt("Ask about the ridge trail");

        await prompt.ChooseSuggestionAsync("How long is the loop?");
        await prompt.PromptAsync("How long is the loop?");
        await prompt.RetryAsync();
        await prompt.InsertAsync();
        await Box(page).FillAsync("one two three four five six seven eight nine ten");
        await Box(page).PressAsync("Enter");
        await Expect(prompt.AiPrompt).ToHaveAttributeAsync("data-generating", "");
        await prompt.StopAsync();

        Assert.Equal(["stopped", "done"], (await prompt.StatusesAsync()).Select(status =>
            status.ToString().ToLowerInvariant()));
        Assert.Equal("You asked: How long is the loop?", (await prompt.ResponsesAsync())[1]);
        await Expect(page.GetByTestId("inserted")).ToHaveTextAsync("You asked: How long is the loop?");
    }
}
