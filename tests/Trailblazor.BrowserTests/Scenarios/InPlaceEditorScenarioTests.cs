using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class InPlaceEditorScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task TrailName_ShouldChangeOnlyWhenSaved_WhenEditedInPlace(string browser)
    {
        var page = await OpenAsync(browser, "/examples/in-place-editor/basic");
        var name = page.Tb().InPlaceEditor("Trail name");
        var saved = page.Locator("output");
        Assert.Equal("Pine Hollow loop", await name.TextAsync());

        // EditAsync opens the editor, which holds the app's own field.
        await name.EditAsync();
        await page.Tb().TextField("Trail name").EnterAsync("Eagle Ridge traverse");
        await name.SaveAsync();
        Assert.Equal("Eagle Ridge traverse", await name.TextAsync());
        await Expect(saved).ToHaveTextAsync("Eagle Ridge traverse, 4 miles");

        // Cancel keeps the value the app has.
        await name.EditAsync();
        await page.Tb().TextField("Trail name").EnterAsync("Somewhere else");
        await name.CancelAsync();
        Assert.Equal("Eagle Ridge traverse", await name.TextAsync());
        await Expect(saved).ToHaveTextAsync("Eagle Ridge traverse, 4 miles");
    }
}
