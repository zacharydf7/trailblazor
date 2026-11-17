using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SpeechToTextTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // A test browser has no microphone, so a fake recognizer stands in, and window.say hands it a phrase.
    private const string FakeRecognition = """
        window.SpeechRecognition = class {
            start() {
                window.say = text => this.onresult({ resultIndex: 0,
                    results: [Object.assign([{ transcript: text }], { isFinal: true })] });
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
    public async Task SpeechToText_ShouldStayHidden_WhenTheBrowserRecognizesNoSpeech(string browser)
    {
        var page = await OpenWithAsync(browser,
            "delete window.SpeechRecognition; delete window.webkitSpeechRecognition;");

        await Expect(page.Locator(".tb-speech-to-text")).ToHaveAttributeAsync("data-ready", "");
        Assert.False(await page.Tb().SpeechToText("Dictate notes").IsAvailableAsync());
    }
}
