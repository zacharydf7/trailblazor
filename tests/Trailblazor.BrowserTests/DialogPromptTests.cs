using System.Text.RegularExpressions;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class DialogPromptTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Prompt_ShouldStartInTheFieldAndSubmitOnEnter_WhenOpened(string browser)
    {
        var page = await OpenAsync(browser, "/examples/dialog/prompt");
        await page.Tb().Button("Rename trail").ClickAsync();
        var name = page.GetByRole(AriaRole.Textbox, new() { Name = "Name" });
        await Expect(name).ToBeFocusedAsync();

        await name.FillAsync("Ox");
        await page.Keyboard.PressAsync("Enter");
        await Expect(name).ToHaveAccessibleDescriptionAsync(new Regex("Use at least 3 letters."));
        await name.FillAsync("Cedar Flats loop");
        await page.Keyboard.PressAsync("Enter");

        await page.Tb().Dialog("Rename trail").WaitForClosedAsync();
        await Expect(page.GetByText("Cedar Flats loop")).ToBeVisibleAsync();
    }
}
