using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class DialogScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task DeleteReport_ShouldKeepOrDeleteTheReport_WhenTheConfirmationIsAnswered(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog/confirming");
        var delete = page.Tb().Button("Delete report");
        var confirmation = page.Tb().Dialog("Delete report?");

        // The click returns once the dialog is open, and ChooseAsync once the dialog has gone. Nothing marks when the
        // app has finished with the answer, so Expect waits for what it shows.
        await delete.ClickAsync();
        await confirmation.ChooseAsync("Cancel");
        await Expect(page.GetByText("Kept")).ToBeVisibleAsync();

        await delete.ClickAsync();
        await confirmation.ChooseAsync("Delete");
        await Expect(page.GetByText("Deleted")).ToBeVisibleAsync();
        Assert.False(await confirmation.IsOpenAsync());
    }
}
