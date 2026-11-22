using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class DialogNavigationTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldBeCanceled_WhenALinkOrTheBackButtonTakesTheAppToAnotherPage(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-navigation");
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Leave?" });
        await Button(page, "Ask").ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();

        // A link that only changes the query string stays on the page, so the dialog stays open.
        await dialog.GetByRole(AriaRole.Link, new() { Name = "Maps tab" }).ClickAsync();
        await Expect(page).ToHaveURLAsync(new Regex("tab=maps$"));
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("asking");

        // A link to another page cancels it, and the page under it can be used again.
        await dialog.GetByRole(AriaRole.Link, new() { Name = "Page two" }).ClickAsync();
        await Expect(page.GetByTestId("where")).ToHaveTextAsync("two");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("canceled");

        // So does the browser's Back button, which goes back to the first page.
        await Button(page, "Ask").ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("asking");
        await page.GoBackAsync();
        await Expect(page.GetByTestId("where")).ToHaveTextAsync("one");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("canceled");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldStayOpen_WhenItsOptionsKeepItAcrossPages(string browser)
    {
        var page = await OpenAsync(browser, "/dialog-navigation");
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Leave?" });
        await Button(page, "Ask and keep").ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();

        await dialog.GetByRole(AriaRole.Link, new() { Name = "Page two" }).ClickAsync();
        await Expect(page.GetByTestId("where")).ToHaveTextAsync("two");

        // It is still open over the new page, and still answers.
        await Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Stay" }).ClickAsync();
        await Expect(page.GetByTestId("answer")).ToHaveTextAsync("confirmed");
    }
}
