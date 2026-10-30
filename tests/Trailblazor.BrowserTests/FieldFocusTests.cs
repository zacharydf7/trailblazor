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
