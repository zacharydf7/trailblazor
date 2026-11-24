using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests.Scenarios;

// The page of this component shows the body of this test, so it has to read well without the class around it.
public class LabelScenarioTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task BagSize_ShouldExplainItselfAndNameItsField_WhenItsInfoIsOpenedAndItsFieldFilled(string browser)
    {
        var page = await OpenAsync(browser, "/examples/label/info-button");

        // The info button is named "Information about" and the label, and OpenAsync returns the panel it opens.
        var info = await page.Tb().Popover("Information about Bag size").OpenAsync();
        await Expect(info).ToContainTextAsync("A day pack is about 20.");

        // The label names its field, so the field's helper finds it by the label's text.
        await page.Tb().TextField("Bag size").EnterAsync("35");
        var field = page.GetByRole(AriaRole.Textbox, new() { Name = "Bag size", Exact = true });
        await Expect(field).ToHaveValueAsync("35");
    }
}
