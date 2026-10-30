using System.Text.RegularExpressions;

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

    [Theory, MemberData(nameof(InertButtons))]
    public async Task Button_ShouldNotSubmitTheForm_WhenLoadingOrDisabledButFocusable(string browser, string query)
    {
        var page = await OpenAsync(browser, $"/submit-button?{query}=true");

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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldStayInTheTabOrderAndShowItsTooltip_WhenDisabledButFocusable(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var publish = page.GetByRole(AriaRole.Button, new() { Name = "Publish" });
        await page.GetByRole(AriaRole.Button, new() { Name = "Pin to top" }).FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(publish).ToBeFocusedAsync();
        await Expect(publish).ToBeDisabledAsync();
        await Expect(page.GetByRole(AriaRole.Tooltip)).ToHaveTextAsync("Add a title before publishing");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldTogglePressed_WhenSpaceIsPressedOnAToggle(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var pin = page.GetByRole(AriaRole.Button, new() { Name = "Pin to top" });
        var unpressed = await pin.EvaluateAsync<string>("button => getComputedStyle(button).backgroundColor");

        await pin.FocusAsync();
        await page.Keyboard.PressAsync("Space");

        await Expect(pin).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(pin).Not.ToHaveCSSAsync("background-color", unpressed);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldNavigateAndShowItsHover_WhenItIsALink(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var link = page.GetByRole(AriaRole.Link, new() { Name = "Read the theming guide" });
        var resting = await link.EvaluateAsync<string>("link => getComputedStyle(link).backgroundColor");

        await link.HoverAsync();
        await Expect(link).Not.ToHaveCSSAsync("background-color", resting);
        await link.ClickAsync();

        await Expect(page).ToHaveURLAsync(new Regex("/guides/theming$"));
    }

    public static TheoryData<string, string> InertButtons => new(
        from browser in AllBrowsers.Names
        from query in new[] { "loading", "focusable-disabled" }
        select (browser, query));
}
