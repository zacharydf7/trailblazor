namespace Trailblazor.BrowserTests;

public class FieldFocusTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    // Each field and the element in it that takes focus.
    private static readonly (string Field, string Input)[] Fields =
    [
        ("name", "[data-part=input]"), ("copies", "[data-part=input]"), ("subscribe", "[data-part=input]"),
        ("size", "input[value=Medium]"), ("pace", "input[value=Fast]"), ("trail", "[data-part=input]"),
        ("camp", "[data-part=input]"), ("town", "[data-part=input]"), ("dates", "[data-part=start-input]"),
        ("color", "[data-part=input]"), ("photos", "[data-part=input]"), ("volume", "[data-part=input]"),
        ("price", "[data-part=start-thumb]"),
    ];

    public static TheoryData<string, string, string> FieldsInEachBrowser => new(
        from browser in AllBrowsers.Names
        from entry in Fields
        select (browser, entry.Field, entry.Input));

    [Theory, MemberData(nameof(FieldsInEachBrowser))]
    public async Task FocusAsync_ShouldMoveFocusToTheFieldsInput_WhenCalled(string browser, string field, string input)
    {
        var page = await OpenAsync(browser, "/field-focus");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Focus {field}", Exact = true }).ClickAsync();

        await Expect(page.GetByTestId(field).Locator(input)).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoFocus_ShouldFocusTheField_WhenThePageBecomesInteractive(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus");

        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoFocus_ShouldFocusTheField_WhenItAppearsOnThePageLater(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" })).ToBeFocusedAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Add a note", Exact = true }).ClickAsync();

        await Expect(page.GetByLabel("Note")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoFocus_ShouldLeaveFocusInTheFieldTheUserIsTypingIn_WhenAnotherFieldAppears(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" })).ToBeFocusedAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add a note later" }).ClickAsync();
        var name = page.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });

        await name.FillAsync("Ada");
        await Expect(page.GetByLabel("Note")).ToBeVisibleAsync();

        await Expect(name).ToBeFocusedAsync();
        await page.Keyboard.TypeAsync(" Lovelace");
        await Expect(name).ToHaveValueAsync("Ada Lovelace");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoFocus_ShouldFocusTheField_WhenACheckboxRevealsIt(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" })).ToBeFocusedAsync();

        await page.GetByRole(AriaRole.Checkbox, new() { Name = "Other trail" }).ClickAsync();

        await Expect(page.GetByLabel("Which trail")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoFocus_ShouldNotTakeFocusAgain_WhenAReorderMovesTheField(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus");
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" })).ToBeFocusedAsync();
        var reverse = page.GetByRole(AriaRole.Button, new() { Name = "Reverse the steps" });
        // From the keyboard, since WebKit doesn't focus a button that is clicked.
        await reverse.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.Locator("label", new() { HasText = "step" })).ToHaveTextAsync(["Second step", "First step"]);

        await Expect(page.GetByLabel("First step")).Not.ToBeFocusedAsync();
        await Expect(reverse).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoFocus_ShouldFocusTheFieldAndGiveFocusBack_WhenItsScopeLeavesFocusAlone(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus-scopes");
        var opener = page.GetByTestId("open-quiet");
        await opener.FocusAsync();

        await page.Keyboard.PressAsync("Enter");
        await Expect(page.GetByLabel("Note")).ToBeFocusedAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Close quiet" }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await Expect(opener).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoFocus_ShouldFocusTheField_WhenItAppearsInAScopeThatIsAlreadyOpen(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus-scopes");
        await page.GetByTestId("open-later").FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await page.GetByRole(AriaRole.Button, new() { Name = "Show the field" }).ClickAsync();

        await Expect(page.GetByLabel("Later")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AutoFocus_ShouldFocusTheField_WhenItAppearsInADialogThatIsAlreadyOpen(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus-scopes");
        await page.GetByRole(AriaRole.Button, new() { Name = "Open reply" }).ClickAsync();

        await page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Write a reply" }).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Dialog).GetByLabel("Reply")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task AriaLabel_ShouldNameTheFieldWithoutShowingALabel_WhenTheFieldHasNoLabel(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var filter = page.GetByRole(AriaRole.Textbox, new() { Name = "Filter trails", Exact = true });

        await Expect(filter).ToBeVisibleAsync();
        var label = await page.GetByTestId("filter").Locator("label").BoundingBoxAsync();
        Assert.True(label is null || label.Width <= 1);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OnBlur_ShouldBeRaisedOnlyOnLeaving_WhenFocusMovesBetweenTheFieldsOwnBoxes(string browser)
    {
        var page = await OpenAsync(browser, "/field-autofocus");
        // The page's autofocused select takes focus once interactive, so the test acts after that.
        await Expect(page.GetByRole(AriaRole.Combobox, new() { Name = "Trail" })).ToBeFocusedAsync();
        await page.GetByRole(AriaRole.Group, new() { Name = "Dates" }).Locator("[data-part=start-input]").FocusAsync();

        await page.Keyboard.PressAsync("Tab");
        await Expect(page.GetByRole(AriaRole.Group, new() { Name = "Dates" }).Locator("[data-part=end-input]"))
            .ToBeFocusedAsync();
        await Expect(page.GetByTestId("dates-left")).ToHaveTextAsync("0");
        await page.GetByRole(AriaRole.Button, new() { Name = "After the dates" }).FocusAsync();

        await Expect(page.GetByTestId("dates-left")).ToHaveTextAsync("1");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task OnBlur_ShouldBeRaised_WhenFocusLeavesTheField(string browser)
    {
        var page = await OpenAsync(browser, "/examples/text-field");
        var filter = page.GetByRole(AriaRole.Textbox, new() { Name = "Filter trails", Exact = true });
        await filter.FocusAsync();

        await page.Keyboard.PressAsync("Tab");

        await Expect(page.GetByTestId("filter-checked")).ToHaveTextAsync("Checked on leaving");
    }

    public static TheoryData<string, string> DisabledFieldsInEachBrowser => new(
        from browser in AllBrowsers.Names
        from disabled in new[] { "region", "site" }
        select (browser, disabled));

    [Theory, MemberData(nameof(DisabledFieldsInEachBrowser))]
    public async Task FocusAsync_ShouldLeaveFocusWhereItWas_WhenTheFieldIsDisabled(string browser, string field)
    {
        var page = await OpenAsync(browser, "/field-focus");

        await page.GetByRole(AriaRole.Button, new() { Name = $"Focus {field}" }).ClickAsync();
        await Expect(page.GetByTestId("calls")).ToHaveTextAsync("1");

        await Expect(page.GetByTestId(field).Locator("[data-part=input]")).Not.ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SelectTextAsync_ShouldFocusTheFieldAndSelectAllItsText_WhenCalled(string browser)
    {
        var page = await OpenAsync(browser, "/field-focus");
        var name = page.GetByLabel("Name");

        await page.GetByRole(AriaRole.Button, new() { Name = "Select the name" }).ClickAsync();

        await Expect(name).ToBeFocusedAsync();
        Assert.Equal([0, "Ada Lovelace".Length],
            await name.EvaluateAsync<int[]>("input => [input.selectionStart, input.selectionEnd]"));
    }
}
