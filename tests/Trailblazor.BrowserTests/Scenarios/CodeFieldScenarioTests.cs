using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class CodeFieldScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Code_ShouldBeTurnedDownThenAccepted_WhenAWrongCodeAndThenTheRightOneAreEntered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field/verification-code");
        // A code field is driven by the text field's helper, found by its label.
        var code = page.Tb().TextField("Verification code");
        var result = page.Locator("output");

        await code.EnterAsync("135790");
        await Expect(result).ToHaveTextAsync("135790 isn't the code.");

        await code.EnterAsync("246810");
        await Expect(result).ToHaveTextAsync("Code accepted.");
    }
}
