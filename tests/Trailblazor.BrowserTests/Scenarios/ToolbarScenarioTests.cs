using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ToolbarScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailNotes_ShouldRunTheClickedAction_WhenItsButtonsAreClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/toolbar/overflow");
        var notes = page.Tb().Toolbar("Trail notes");

        // ClickAsync clicks a toolbar button by its name.
        await notes.ClickAsync("Add a note");
        await Expect(page.GetByText("Last action: Add a note")).ToBeVisibleAsync();

        // On a narrow page the last buttons move into the More menu, and ClickAsync chooses them from there.
        await page.SetViewportSizeAsync(360, 800);
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Delete note", Exact = true })).ToBeHiddenAsync();
        await notes.ClickAsync("Delete note");
        await Expect(page.GetByText("Last action: Delete note")).ToBeVisibleAsync();
    }
}
