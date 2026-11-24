using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ToastScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Archive_ShouldRestoreTheReport_WhenTheToastsUndoIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toast/action");

        await page.Tb().Button("Archive report").ClickAsync();
        await Expect(page.GetByText("Archived", new() { Exact = true })).ToBeVisibleAsync();

        // ChooseAsync waits for the toast with this text and presses its action.
        await page.Tb().Toasts.ChooseAsync("Report archived", "Undo");

        await Expect(page.GetByText("Restored", new() { Exact = true })).ToBeVisibleAsync();
    }
}
