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

        await Expect(page.GetByText("Quantity value: 7", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(page.GetByText("Budget value: 2500", new() { Exact = true })).ToBeVisibleAsync();
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

        await Expect(page.GetByText("Quantity value: 3", new() { Exact = true })).ToBeVisibleAsync();
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
        await copies.PressAsync("Tab");

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

        await quantity.PressAsync("End");
        await quantity.PressSequentiallyAsync("e");
        await quantity.PressAsync("Tab");

        await Expect(quantity).ToHaveAttributeAsync("aria-invalid", "true");
        Assert.True(await quantity.EvaluateAsync<bool>("input => input.validity.badInput"),
            "The typed text was replaced.");
        await Expect(page.GetByText("Quantity value: 1", new() { Exact = true })).ToBeVisibleAsync();
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
        await Expect(page.GetByText("Quantity value: 1", new() { Exact = true })).ToBeVisibleAsync();
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
        await Expect(page.GetByText("Budget value: 2450.5", new() { Exact = true })).ToBeVisibleAsync();
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
        await Expect(page.GetByText("Budget value: 2450", new() { Exact = true })).ToBeVisibleAsync();
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
        await group.PressAsync("Tab");

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

    private async Task<IPage> OpenStepAsync(string browser, BrowserNewContextOptions? options = null)
    {
        var page = await OpenAsync(browser, "/number-step", options);
        await Expect(page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" })).ToHaveValueAsync("5");
        return page;
    }

    private static ILocator StepButton(IPage page, string name) =>
        page.Locator("[data-part=control]").Filter(new()
        {
            Has = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" }),
        }).GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator Chosen(IPage page) =>
        page.GetByRole(AriaRole.Status, new() { Name = "Copies chosen", Exact = true });

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StepButton_ShouldMoveFocusToTheInput_WhenClicked(string browser)
    {
        var page = await OpenStepAsync(browser);
        var copies = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" });

        await StepButton(page, "Increase").ClickAsync();

        await Expect(copies).ToBeFocusedAsync();
        await Expect(Chosen(page)).ToHaveTextAsync("6");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(Chosen(page)).ToHaveTextAsync("7");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StepButton_ShouldLeaveFocusInTheInput_WhenItReachesALimitAndTurnsOff(string browser)
    {
        var page = await OpenStepAsync(browser);
        var copies = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" });
        await copies.FocusAsync();

        for (var clicks = 0; clicks < 5; clicks++)
        {
            await StepButton(page, "Increase").ClickAsync();
        }

        await Expect(StepButton(page, "Increase")).ToBeDisabledAsync();
        await Expect(copies).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Chosen(page)).ToHaveTextAsync("9");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StepButton_ShouldLeaveFocusInTheInput_WhenPressedAfterTurningOff(string browser)
    {
        var page = await OpenStepAsync(browser);
        var copies = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" });
        await copies.FillAsync("10");
        await copies.PressAsync("Enter");
        await Expect(StepButton(page, "Increase")).ToBeDisabledAsync();
        var box = (await StepButton(page, "Increase").BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        await Expect(copies).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        await Expect(Chosen(page)).ToHaveTextAsync("9");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StepButton_ShouldKeepItsStep_WhenFocusLeavesBeforeTheStepRenders(string browser)
    {
        var page = await OpenStepAsync(browser);
        var copies = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" });
        await copies.FocusAsync();

        // The click and the leaving in one script, so no render can come between them.
        await StepButton(page, "Increase").EvaluateAsync("""
            button => {
                button.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, cancelable: true }));
                button.click();
                document.activeElement.blur();
            }
            """);

        await ClickElsewhereAsync(page);

        await Expect(Chosen(page)).ToHaveTextAsync("6");
        await Expect(copies).ToHaveValueAsync("6");
    }

    private static async Task ClickElsewhereAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Elsewhere" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Status, new() { Name = "Elsewhere clicks", Exact = true })).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StepButton_ShouldKeepTheValueAsItIs_WhenFocusLeavesAPlainFieldThatRoundsItsText(string browser)
    {
        var page = await OpenStepAsync(browser);
        var rate = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Rate" });
        var chosen = page.GetByRole(AriaRole.Status, new() { Name = "Rate chosen", Exact = true });
        await page.Locator("[data-part=control]").Filter(new() { Has = rate }).Locator("[data-part=increment]").ClickAsync();
        await Expect(chosen).ToHaveTextAsync("1.125");

        await ClickElsewhereAsync(page);

        await Expect(chosen).ToHaveTextAsync("1.125");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StepButton_ShouldKeepBothSteps_WhenASecondPressComesBeforeTheFirstRenders(string browser)
    {
        var page = await OpenStepAsync(browser);
        await page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" }).FocusAsync();

        // Both clicks in one script, so no render can come between them, however fast the server is.
        await StepButton(page, "Increase").EvaluateAsync("""
            button => {
                for (let press = 0; press < 2; press++) {
                    button.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, cancelable: true }));
                    button.click();
                }
            }
            """);

        await Expect(Chosen(page)).ToHaveTextAsync("7");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StepButton_ShouldStepFromTheTypedText_WhenClickedBeforeItIsCommitted(string browser)
    {
        var page = await OpenStepAsync(browser);
        var copies = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" });
        await copies.FillAsync("2");

        await StepButton(page, "Increase").ClickAsync();

        await Expect(Chosen(page)).ToHaveTextAsync("3");
        await Expect(copies).ToHaveValueAsync("3");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task StepButton_ShouldStepFromTheValueNotTheShownText_WhenClickedWithNothingTyped(string browser)
    {
        var page = await OpenAsync(browser, "/number-format");
        var tip = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Tip" });
        var field = page.Locator("[data-part=control]").Filter(new() { Has = tip });
        await tip.FocusAsync();

        await field.Locator("[data-part=increment]").ClickAsync();
        await Expect(page.GetByTestId("tip")).ToHaveTextAsync("10.25");
        await field.Locator("[data-part=increment]").ClickAsync();

        await Expect(page.GetByTestId("tip")).ToHaveTextAsync("10.50");
    }

    [Fact]
    public async Task StepButton_ShouldLeaveTheInputUnfocused_WhenTappedAfterTurningOff()
    {
        var page = await OpenStepAsync("chromium", new() { HasTouch = true, IsMobile = true });
        var copies = page.GetByRole(AriaRole.Spinbutton, new() { Name = "Copies" });
        await copies.FillAsync("10");
        await copies.PressAsync("Enter");
        await Expect(StepButton(page, "Increase")).ToBeDisabledAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Elsewhere" }).FocusAsync();
        var box = (await StepButton(page, "Increase").BoundingBoxAsync())!;

        await page.Touchscreen.TapAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        // A tap's mouse events come a moment later, and anything they did would show by then.
        await page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve, 100))");

        await Expect(copies).Not.ToBeFocusedAsync();
    }

    [Fact]
    public async Task StepButton_ShouldLeaveFocusAlone_WhenTapped()
    {
        var page = await OpenStepAsync("chromium", new() { HasTouch = true });
        var elsewhere = page.GetByRole(AriaRole.Button, new() { Name = "Elsewhere" });
        await elsewhere.FocusAsync();
        var box = (await StepButton(page, "Increase").BoundingBoxAsync())!;

        await page.Touchscreen.TapAsync(box.X + box.Width / 2, box.Y + box.Height / 2);

        await Expect(Chosen(page)).ToHaveTextAsync("6");
        await Expect(elsewhere).ToBeFocusedAsync();
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
