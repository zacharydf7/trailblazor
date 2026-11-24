using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class PopconfirmScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DeleteTrail_ShouldKeepOrDeleteTheTrail_WhenTheQuestionIsAnswered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/popconfirm/basic");
        // A popconfirm is found by the name of the button that opens it.
        var delete = page.Tb().Popconfirm("Delete trail");
        var status = page.Locator("output");

        // CancelAsync and ConfirmAsync open the question from the button and press its answer.
        await delete.CancelAsync();
        await Expect(status).ToHaveTextAsync("Kept it.");

        await delete.ConfirmAsync();
        await Expect(status).ToHaveTextAsync("Deleted.");
    }
}
