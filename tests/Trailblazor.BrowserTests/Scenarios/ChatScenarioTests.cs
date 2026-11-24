using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ChatScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TripChat_ShouldShowTheMessageAndTheReply_WhenTheUserSends(string browser)
    {
        var page = await OpenAsync(browser, "/examples/chat/basic");
        var chat = page.Tb().Chat("Trip planning");

        // SendAsync writes in the box, presses Enter and waits until the app's OnSend has run.
        await chat.SendAsync("Shall we meet at eight?");
        Assert.Equal("You: Shall we meet at eight?", (await chat.MessagesAsync())[^1]);

        // The example's reply streams in after a pause; Expect waits until its last word is there.
        await Expect(page.GetByRole(AriaRole.Article).Last).ToContainTextAsync("meet you at the trailhead at eight.");
        Assert.Equal("Robin: Sounds good. I'll bring the map and meet you at the trailhead at eight.",
            (await chat.MessagesAsync())[^1]);
        Assert.Null(await chat.TypingAsync());
    }
}
