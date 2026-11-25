using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class SpeechToTextScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailNotes_ShouldTakeWhatIsSaid_WhenDictated(string browser)
    {
        var page = await OpenAsync(browser, "/examples/speech-to-text/basic");
        // A test browser has no microphone, so UseFakeSpeechAsync stands in for its speech recognition from the next
        // load on.
        await page.UseFakeSpeechAsync();
        await page.ReloadAsync();
        var dictate = page.Tb().SpeechToText("Dictate notes");

        // StartAsync presses the button, and SayAsync hands the app a phrase as the browser does on hearing one.
        await dictate.StartAsync();
        Assert.True(await dictate.IsListeningAsync());
        await dictate.SayAsync("Bridge out at mile three.");

        Assert.Equal("Bridge out at mile three.", await page.Tb().TextField("Trail notes").TextAsync());
    }
}
