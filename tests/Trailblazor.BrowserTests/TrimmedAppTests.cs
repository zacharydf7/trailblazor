using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

// The trimmed app is published with full trimming, so these show what an app trimmed that far still runs.
public class TrimmedAppTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Dialog_ShouldOpenWithItsParametersAndReturnItsResult_WhenTheAppIsFullyTrimmed(string browser)
    {
        var page = await OpenTrimmedAppAsync(browser);

        await page.GetByRole(AriaRole.Button, new() { Name = "Rename the trail" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Rename the trail" });
        var name = dialog.GetByRole(AriaRole.Textbox, new() { Name = "Name" });
        await Expect(name).ToHaveValueAsync("Ridge loop");
        await name.FillAsync("Creek path");
        await name.PressAsync("Tab");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(dialog).ToBeHiddenAsync();
        await Expect(page.GetByTestId("name")).ToHaveTextAsync("Creek path");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldConvertWhatIsTyped_WhenTheAppIsFullyTrimmed(string browser)
    {
        var page = await OpenTrimmedAppAsync(browser);

        await page.Tb().NumberField("Length in miles").EnterAsync("6.25");

        await Expect(page.GetByTestId("length")).ToHaveTextAsync("6.25");
    }
}
