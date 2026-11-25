using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SpeechToTextTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // A test browser has no microphone, so a fake recognizer stands in, and window.say hands it a phrase. Like a real
    // one, it ends after that phrase unless continuous.
    private const string FakeRecognition = """
        window.SpeechRecognition = class {
            start() {
                window.say = text => {
                    this.onresult({ resultIndex: 0,
                        results: [Object.assign([{ transcript: text }], { isFinal: true })] });
                    if (!this.continuous) {
                        this.onend?.();
                    }
                };
            }
            stop() { this.onend?.(); }
            abort() { this.onend?.(); }
        };
        """;

    private async Task<IPage> OpenWithAsync(string browser, string script)
    {
        var page = await OpenAsync(browser, "/examples/speech-to-text");
        await page.AddInitScriptAsync(script);
        await page.ReloadAsync();
        return page;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SpeechToText_ShouldFillTheFieldFromWhatIsSaid_WhenListening(string browser)
    {
        var page = await OpenWithAsync(browser, FakeRecognition);
        var dictate = page.Tb().SpeechToText("Dictate notes");

        await dictate.Button.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(dictate.Button).ToHaveAttributeAsync("aria-pressed", "true");
        await page.EvaluateAsync("() => window.say('Muddy by the bridge')");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Trail notes" }))
            .ToHaveValueAsync("Muddy by the bridge");
        await Expect(dictate.Button).ToHaveAttributeAsync("aria-pressed", "false");
        await page.Keyboard.PressAsync("Enter");
        await Expect(dictate.Button).ToHaveAttributeAsync("aria-pressed", "true");
        await page.Keyboard.PressAsync("Enter");

        await Expect(dictate.Button).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SpeechToText_ShouldKeepListening_WhenStartedAgainBeforeTheLastStopEnds(string browser)
    {
        // Like a real one, this recognizer ends a moment after it's asked to stop.
        var page = await OpenWithAsync(browser, FakeRecognition
            .Replace("stop() { this.onend?.(); }", "stop() { setTimeout(() => this.onend?.(), 300); }")
            .Replace("abort() { this.onend?.(); }", "abort() { setTimeout(() => this.onend?.(), 300); }"));
        var dictate = page.Tb().SpeechToText("Dictate notes");

        await dictate.Button.ClickAsync();
        await dictate.Button.ClickAsync();
        await dictate.Button.ClickAsync();
        await page.WaitForTimeoutAsync(600);

        await Expect(dictate.Button).ToHaveAttributeAsync("aria-pressed", "true");
        await page.EvaluateAsync("() => window.say('Muddy by the bridge')");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Trail notes" }))
            .ToHaveValueAsync("Muddy by the bridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SayAsync_ShouldHandTheAppAPhraseAndWaitForTheStop_WhenThePageUsesFakeSpeech(string browser)
    {
        var page = await OpenAsync(browser, "/examples/speech-to-text");
        await page.UseFakeSpeechAsync();
        await page.ReloadAsync();
        var dictate = page.Tb().SpeechToText("Dictate notes");
        Assert.Equal("The dictation button isn't listening; call StartAsync first.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => dictate.SayAsync("Muddy"))).Message);

        await dictate.StartAsync();
        await dictate.SayAsync("Muddy by the bridge");

        Assert.False(await dictate.IsListeningAsync());
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Trail notes" }))
            .ToHaveValueAsync("Muddy by the bridge");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SayAsync_ShouldKeepListening_WhenTheButtonIsContinuous(string browser)
    {
        var page = await OpenAsync(browser, "/speech-to-text");
        await page.UseFakeSpeechAsync();
        await page.ReloadAsync();
        var dictate = page.Tb().SpeechToText("Dictate log");

        var log = page.GetByRole(AriaRole.Textbox, new() { Name = "Trail log" });

        await dictate.StartAsync();
        await dictate.SayAsync("Mile one.");
        Assert.Equal("Mile one.", await log.InputValueAsync());
        await dictate.SayAsync("Mile two.");

        Assert.True(await dictate.IsListeningAsync());
        Assert.Equal("Mile one. Mile two.", await log.InputValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SayAsync_ShouldReachOnlyTheButtonStartedLast_WhenAnotherWasListening(string browser)
    {
        var page = await OpenAsync(browser, "/speech-to-text");
        await page.UseFakeSpeechAsync();
        await page.ReloadAsync();
        var (log, notes) = (page.Tb().SpeechToText("Dictate log"), page.Tb().SpeechToText("Dictate notes"));
        await log.StartAsync();

        await notes.StartAsync();

        // As in a browser, one recognition listens at a time, so the log's stopped.
        Assert.Equal("The dictation button isn't listening; call StartAsync first.",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => log.SayAsync("Mile one."))).Message);
        await notes.SayAsync("Muddy by the bridge.");
        Assert.Equal("", await page.GetByRole(AriaRole.Textbox, new() { Name = "Trail log" }).InputValueAsync());
        Assert.Equal("Muddy by the bridge.",
            await page.GetByRole(AriaRole.Textbox, new() { Name = "Trail notes" }).InputValueAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SayAsync_ShouldSayWhatToCall_WhenThePageHasNoFakeSpeech(string browser)
    {
        var page = await OpenWithAsync(browser, FakeRecognition);
        var dictate = page.Tb().SpeechToText("Dictate notes");
        await dictate.StartAsync();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => dictate.SayAsync("Muddy"));

        Assert.Equal("The page isn't using fake speech; call page.UseFakeSpeechAsync() before the page opens the "
            + "app, or reload the page after.", thrown.Message);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SpeechToText_ShouldStayHidden_WhenTheBrowserRecognizesNoSpeech(string browser)
    {
        var page = await OpenWithAsync(browser,
            "delete window.SpeechRecognition; delete window.webkitSpeechRecognition;");

        await Expect(page.Locator(".tb-speech-to-text")).ToHaveAttributeAsync("data-ready", "");
        Assert.False(await page.Tb().SpeechToText("Dictate notes").IsAvailableAsync());
    }
}
