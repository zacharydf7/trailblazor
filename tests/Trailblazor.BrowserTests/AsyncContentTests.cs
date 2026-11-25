using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class AsyncContentTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Retry_ShouldKeepFocusUntilTheContentLoads_WhenPressedWithTheKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/async-content-retry");
        var groups = page.Tb().AsyncContent("groups");
        var retry = page.GetByRole(AriaRole.Button, new() { Name = "Retry" });
        await groups.WaitUntilLoadedAsync();
        await retry.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(retry).ToHaveAttributeAsync("aria-busy", "true");
        await groups.WaitUntilLoadedAsync();
        await Expect(retry).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByText("Dawn hikers")).ToBeVisibleAsync();

        await Expect(groups.Region).ToBeFocusedAsync();
        Assert.Equal(AsyncContentState.Content, await groups.StateAsync());
    }
}
