using System.Text.RegularExpressions;

namespace Trailblazor.BrowserTests;

public class ButtonTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldKeepItsTextClearOfItsBorders_WhenALongLabelWraps(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var button = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Add the Eagle Ridge") });

        var box = (await button.BoundingBoxAsync())!;
        var text = (await button.Locator("[data-part=label]").BoundingBoxAsync())!;

        Assert.True(text.Height > 30, "The label should wrap onto more than one line.");
        Assert.True(text.Y >= box.Y + 2 && text.Y + text.Height <= box.Y + box.Height - 2,
            "The text should stay clear of the button's top and bottom borders.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldActivate_WhenEnterOrSpaceIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var counter = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Clicks") });
        await counter.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(counter).ToHaveTextAsync("Clicks: 1");
        await page.Keyboard.PressAsync("Space");

        await Expect(counter).ToHaveTextAsync("Clicks: 2");
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
    public async Task IconButton_ShouldShowItsNameWithoutDescribingItself_WhenFocusedWithShowTooltip(string browser)
    {
        var page = await OpenAsync(browser, "/examples/icon-button");
        var bell = page.GetByRole(AriaRole.Button, new() { Name = "Notifications" });

        await bell.FocusAsync();

        await Expect(page.GetByRole(AriaRole.Tooltip, new() { Name = "Notifications" })).ToBeVisibleAsync();
        await Expect(bell).ToHaveAccessibleDescriptionAsync("");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldShowAFocusRing_WhenFocusedByKeyboard(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var counter = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Clicks") });
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
        await page.GetByRole(AriaRole.Button, new() { Name = "Save the trip" }).FocusAsync();

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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Button_ShouldDeepenItsSoftFill_WhenHoveredAndThenPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/button");
        var soft = page.GetByRole(AriaRole.Button, new() { Name = "Soft Primary", Exact = true });
        var resting = await soft.EvaluateAsync<string>("button => getComputedStyle(button).backgroundColor");

        await soft.HoverAsync();
        await Expect(soft).Not.ToHaveCSSAsync("background-color", resting);
        var hovered = await soft.EvaluateAsync<string>("button => getComputedStyle(button).backgroundColor");
        await page.Mouse.DownAsync();

        await Expect(soft).Not.ToHaveCSSAsync("background-color", hovered);
        await page.Mouse.UpAsync();
    }
}
