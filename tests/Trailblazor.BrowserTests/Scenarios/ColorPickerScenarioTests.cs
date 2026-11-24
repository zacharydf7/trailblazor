using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class ColorPickerScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task WarningColor_ShouldClearItsError_WhenAColorThatShowsIsChosen(string browser)
    {
        var page = await OpenAsync(browser, "/examples/color-picker/validation");
        var warning = page.Tb().ColorPicker("Warning color");
        await Expect(page.GetByText("White won't show on the trail map.")).ToBeVisibleAsync();

        // ChooseAsync types the color and waits until the field has taken it; the app's Validation runs again.
        await warning.ChooseAsync("#e8a33d");

        Assert.Equal("#e8a33d", await warning.ValueAsync());
        await Expect(page.GetByText("White won't show on the trail map.")).ToBeHiddenAsync();
    }
}
