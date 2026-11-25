using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ValidationSummaryScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SignUp_ShouldListWhatToFixAndLinkToIt_WhenSubmittedEmpty(string browser)
    {
        var page = await OpenAsync(browser, "/examples/validation-summary/basic");

        await page.Tb().Button("Sign up").ClickAsync();
        var summary = page.Tb().ValidationSummary();
        await Expect(summary.Element).ToBeVisibleAsync();
        Assert.Equal(["Name is required.", "Email is required.", "Emergency contact is required."],
            await summary.MessagesAsync());

        await summary.FollowAsync("Email is required.");

        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Email" })).ToBeFocusedAsync();
    }
}
