namespace Trailblazor.BrowserTests;

public class NumberFieldTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldStepWithTheArrowKeys_WhenFocused(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var quantity = page.GetByLabel("Quantity");

        await quantity.PressAsync("ArrowUp");
        await quantity.PressAsync("ArrowUp");
        await quantity.PressAsync("Tab");

        await Expect(page.GetByTestId("quantity-echo")).ToHaveTextAsync("3");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldSkipItsStepButtons_WhenTabbing(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        await page.GetByLabel("Quantity").FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByLabel("Weight")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldStepAndStopAtItsLimit_WhenTheStepButtonIsClicked(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var increase = page.GetByTestId("quantity").GetByRole(AriaRole.Button, new() { Name = "Increase" });

        for (var i = 0; i < 9; i++)
        {
            await increase.ClickAsync();
        }

        await Expect(page.GetByTestId("quantity-echo")).ToHaveTextAsync("10");
        await Expect(increase).ToBeDisabledAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldSayItNeedsANumber_WhenAnEmptyFieldGetsTextTheBrowserCannotRead(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var copies = page.GetByLabel("Copies");

        await copies.PressSequentiallyAsync("1e");
        // Copies is the last field, and tabbing out of the page fires no blur in Firefox.
        await copies.PressAsync("Shift+Tab");

        await Expect(copies).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(copies).ToHaveAccessibleDescriptionAsync("Copies must be a number.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldClearTheError_WhenUnreadableTextIsDeleted(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var copies = page.GetByLabel("Copies");
        await copies.PressSequentiallyAsync("1e");
        await copies.PressAsync("Shift+Tab");
        await Expect(copies).ToHaveAttributeAsync("aria-invalid", "true");

        await copies.ClearAsync();
        await copies.PressAsync("Shift+Tab");

        await Expect(copies).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldExplainTheLimits_WhenATypedValueIsOutsideThem(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var quantity = page.GetByLabel("Quantity");

        await quantity.FillAsync("12");
        await quantity.PressAsync("Tab");

        await Expect(quantity).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(quantity).ToHaveAccessibleDescriptionAsync("From 0 to 10. Quantity must be from 0 to 10.");
        await Expect(page.GetByTestId("quantity-echo")).ToHaveTextAsync("1");
    }
}
