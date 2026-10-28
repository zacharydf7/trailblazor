namespace Trailblazor.BrowserTests;

public class ButtonTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldActivate_WhenEnterOrSpaceIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var counter = page.GetByTestId("counter");
        await counter.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(counter).ToHaveTextAsync("Clicked 1 times");
        await page.Keyboard.PressAsync("Space");

        await Expect(counter).ToHaveTextAsync("Clicked 2 times");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldSubmitTheForm_WhenClickedOrEnterIsPressedInAField(string browser)
    {
        var page = await OpenAsync(browser, "/submit-button");

        await page.GetByTestId("submit").ClickAsync();
        await page.GetByTestId("field").PressAsync("Enter");

        await Expect(page.GetByTestId("submits")).ToHaveTextAsync("2");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldNotSubmitTheForm_WhenLoading(string browser)
    {
        var page = await OpenAsync(browser, "/submit-button?loading=true");

        // Playwright treats aria-disabled as disabled and would wait forever without Force.
        await page.GetByTestId("submit").ClickAsync(new() { Force = true });
        await page.GetByTestId("field").PressAsync("Enter");
        await page.GetByTestId("ping").ClickAsync();

        // The ping round-trips after both submit attempts, so a submit would have landed by now.
        await Expect(page.GetByTestId("pings")).ToHaveTextAsync("1");
        await Expect(page.GetByTestId("submits")).ToHaveTextAsync("0");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldShowAFocusRing_WhenFocusedByKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var counter = page.GetByTestId("counter");
        await counter.FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");

        await page.Keyboard.PressAsync("Tab");

        await Expect(counter).ToBeFocusedAsync();
        await Expect(counter).ToHaveCSSAsync("outline-style", "solid");
        await Expect(counter).ToHaveCSSAsync("outline-width", "2px");
    }
}
