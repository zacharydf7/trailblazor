using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ButtonScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SendReport_ShouldSendOnlyANamedTrail_WhenClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button/submit");
        var send = page.Tb().Button("Send report");
        var trail = page.Tb().TextField("Trail name");
        var sent = page.GetByLabel("Reports sent");

        // Without a trail name the form isn't sent, and the field says why. The click on a form's submit button
        // returns once the form has handled the submit.
        await send.ClickAsync();
        Assert.Equal("Trail name is required.", await trail.ErrorAsync());
        await Expect(sent).ToHaveTextAsync("Reports sent: 0");

        await trail.EnterAsync("Pine Hollow loop");
        await send.ClickAsync();

        Assert.Null(await trail.ErrorAsync());
        await Expect(sent).ToHaveTextAsync("Reports sent: 1");
    }
}
