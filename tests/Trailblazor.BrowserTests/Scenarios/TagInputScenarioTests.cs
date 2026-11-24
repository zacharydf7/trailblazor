using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class TagInputScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TripReport_ShouldBeSentOnlyWithAnAnimal_WhenAnimalsAreAddedAndRemoved(string browser)
    {
        var page = await OpenAsync(browser, "/examples/tag-input/required");
        var seen = page.Tb().TagInput("Wildlife seen");
        var send = page.Tb().Button("Send report");
        var sent = page.Locator("output");

        // A report with no animals isn't sent, and the field says why.
        await send.ClickAsync();
        Assert.Equal("Add at least one animal.", await seen.ErrorAsync());
        await Expect(sent).ToHaveTextAsync("");

        // AddAsync types the text, presses Enter and waits for the app's answer.
        await seen.AddAsync("Marmot");
        await seen.AddAsync("Deer");
        await seen.AddAsync("Heron");
        await seen.RemoveAsync("Deer");
        await Expect(seen.Tags).ToHaveTextAsync(["Marmot", "Heron"]);

        await send.ClickAsync();
        Assert.Null(await seen.ErrorAsync());
        await Expect(sent).ToHaveTextAsync("Sent: Marmot, Heron.");
    }
}
