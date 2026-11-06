using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class NumberFieldTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldFindTheField_WhenItIsRequiredAndItsLabelShowsAnAsterisk(string browser)
    {
        var page = await OpenStaticAsync(browser, "/examples/form");

        await page.Tb().NumberField("Party size").EnterAsync("4");

        await Expect(page.GetByRole(AriaRole.Spinbutton, new() { Name = "Party size" })).ToHaveValueAsync("4");
        await page.Tb().NumberField("Party size *").EnterAsync("5");
        await Expect(page.GetByRole(AriaRole.Spinbutton, new() { Name = "Party size" })).ToHaveValueAsync("5");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldCommitTheText_WhenAPlainOrFormattedFieldIsEntered(string browser)
    {
        // Not waiting for the page to be interactive, which the helper waits for itself.
        var page = await OpenStaticAsync(browser, "/examples/number-field");

        await page.Tb().NumberField("Quantity").EnterAsync("7");
        await page.Tb().NumberField("Budget").EnterAsync("2,500");

        await Expect(page.GetByTestId("quantity-echo")).ToHaveTextAsync("7");
        await Expect(page.GetByTestId("budget-echo")).ToHaveTextAsync("2500");
        await Expect(page.GetByLabel("Budget")).ToHaveValueAsync("$2,500.00");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldPostTheText_WhenTheFieldIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-number");

        await page.Tb().NumberField("Price").EnterAsync("12.5");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("12.5");
    }

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
    public async Task NumberField_ShouldSayItNeedsANumber_WhenAnEmptyFieldGetsTextTheBrowserCannotRead(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var copies = page.GetByLabel("Copies");

        await copies.PressSequentiallyAsync("1e");
        // Copies is the last field, and tabbing out of the page fires no blur in Firefox.
        await copies.PressAsync("Shift+Tab");

        await Expect(copies).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(copies).ToHaveAccessibleDescriptionAsync("Copies must be a number.");
        Assert.True(await copies.EvaluateAsync<bool>("input => input.validity.badInput"),
            "The typed text was cleared.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldKeepTheUnreadableText_WhenItIsTypedAfterAValue(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var quantity = page.GetByLabel("Quantity");
        var before = await page.GetByTestId("quantity-echo").TextContentAsync();

        await quantity.PressAsync("End");
        await quantity.PressSequentiallyAsync("e");
        await quantity.PressAsync("Tab");

        await Expect(quantity).ToHaveAttributeAsync("aria-invalid", "true");
        Assert.True(await quantity.EvaluateAsync<bool>("input => input.validity.badInput"),
            "The typed text was replaced.");
        await Expect(page.GetByTestId("quantity-echo")).ToHaveTextAsync(before!);
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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldKeepEveryArrowStep_WhenEachUpdateIsSlowToComeBack(string browser)
    {
        // A lost step depends on timing, so a few page loads make a regression fail reliably.
        for (var load = 0; load < 4; load++)
        {
            var page = await OpenAsync(browser, "/slow-fields");
            var copies = page.GetByLabel("Copies");
            await copies.FocusAsync();

            for (var press = 0; press < 6; press++)
            {
                await page.Keyboard.PressAsync("ArrowUp");
            }

            await Expect(page.GetByTestId("copies")).ToHaveTextAsync("6");
            await Expect(copies).ToHaveValueAsync("6");
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldShowTheValueInItsFormat_WhenTypedOrSteppedWithAFormat(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var budget = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Budget" });

        await budget.FillAsync("2400.5");
        await budget.PressAsync("Tab");
        await Expect(budget).ToHaveValueAsync("$2,400.50");
        // The text box would move the caret on the arrow, so the field's script holds it.
        await page.EvaluateAsync(
            "() => document.addEventListener('keydown', event => window.held = event.defaultPrevented)");
        await budget.FocusAsync();
        await budget.PressAsync("ArrowUp");
        Assert.True(await page.EvaluateAsync<bool>("() => window.held"));

        await Expect(budget).ToHaveValueAsync("$2,450.50");
        await Expect(page.GetByTestId("budget-echo")).ToHaveTextAsync("2450.5");
        await Expect(budget).ToHaveAttributeAsync("aria-valuetext", "$2,450.50");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldStepFromTheTypedText_WhenAnArrowIsPressedBeforeItIsCommitted(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var budget = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Budget" });

        await budget.FillAsync("2400");
        await budget.PressAsync("ArrowUp");

        await Expect(budget).ToHaveValueAsync("$2,450.00");
        await Expect(page.GetByTestId("budget-echo")).ToHaveTextAsync("2450");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldRoundOrClampWhatIsTyped_WhenDecimalsOrClampToRangeIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/examples/number-field");
        var discount = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Discount" });
        var group = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Group size" });

        await discount.FillAsync("12.6%");
        await discount.PressAsync("Tab");
        await group.FillAsync("30");
        // The field is the page's last, and Firefox fires no change when Tab leaves the page.
        await group.BlurAsync();

        await Expect(discount).ToHaveValueAsync("13%");
        await Expect(group).ToHaveValueAsync("12");
        await Expect(group).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldStayANumberBoxThatPosts_WhenAFormattedFieldIsInAStaticForm(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-number");
        var price = page.GetByLabel("Price");
        await Expect(price).ToHaveAttributeAsync("type", "number");

        await price.FillAsync("1234.50");
        await page.GetByRole(AriaRole.Button, new() { Name = "Send" }).ClickAsync();

        await Expect(page.GetByTestId("posted")).ToHaveTextAsync("1234.50");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldStepFromTheValueNotTheShownText_WhenNothingWasTyped(string browser)
    {
        var page = await OpenAsync(browser, "/number-format");
        var tip = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Tip" });
        var rate = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Rate" });

        await tip.PressAsync("ArrowUp");
        await Expect(page.GetByTestId("tip")).ToHaveTextAsync("10.25");
        await tip.PressAsync("ArrowUp");
        await rate.PressAsync("ArrowUp");

        await Expect(page.GetByTestId("tip")).ToHaveTextAsync("10.50");
        await Expect(page.GetByTestId("rate")).ToHaveTextAsync("0.125");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task NumberField_ShouldUpdateAsTypedAndFormatOnLeaving_WhenUpdateOnInputIsSet(string browser)
    {
        var page = await OpenAsync(browser, "/number-live");
        var budget = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Budget" });
        var copies = page.GetByLabel("Copies");

        await budget.FillAsync("");
        await budget.PressSequentiallyAsync("125");
        await Expect(page.GetByTestId("budget")).ToHaveTextAsync("125");
        await Expect(budget).ToHaveValueAsync("125");
        await copies.PressSequentiallyAsync("42");
        await Expect(page.GetByTestId("copies")).ToHaveTextAsync("42");

        await Expect(budget).ToHaveValueAsync("$125.00");
        await Expect(copies).ToHaveValueAsync("42");
    }
}
