using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class FieldBoxTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private static ILocator Box(IPage page, string input) =>
        page.Locator("[data-part=control]").Filter(new() { Has = page.GetByLabel(input, new() { Exact = true }) });

    private static async Task PressPaddingAsync(IPage page, string input)
    {
        var box = (await Box(page, input).BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(box.X + 3, box.Y + box.Height / 2);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldFocusItsInput_WhenItsPaddingIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");

        foreach (var input in new[] { "Notes", "Copies", "Town", "Color", "Locked" })
        {
            await PressPaddingAsync(page, input);
            await Expect(Box(page, input).Locator(".tb-field__input")).ToBeFocusedAsync();
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldFocusItsFirstInput_WhenItHoldsTwo(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");
        var box = (await page.Locator("[data-part=control]:has([data-part=start-input])").BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + 3, box.Y + box.Height / 2);

        await Expect(page.Locator("[data-part=start-input]")).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldFocusItsInput_WhenItsStartIconIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");

        await page.GetByTestId("name-start").ClickAsync();

        await Expect(page.GetByLabel("Name", new() { Exact = true })).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldKeepFocusInItsInput_WhenItsPaddingIsPressedWhileTyping(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");
        var input = page.GetByLabel("Notes", new() { Exact = true });
        await input.PressSequentiallyAsync("Trail");

        await PressPaddingAsync(page, "Notes");
        await page.Keyboard.TypeAsync("s");

        await Expect(input).ToBeFocusedAsync();
        await Expect(input).ToHaveValueAsync("Trails");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldDropFocusToThePage_WhenTheFieldIsDisabled(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");

        foreach (var input in new[] { "Off", "Fenced" })
        {
            await page.GetByLabel("Name", new() { Exact = true }).FocusAsync();
            await PressPaddingAsync(page, input);
            Assert.True(await page.EvaluateAsync<bool>("() => document.activeElement === document.body"));
        }
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldLeaveItsInputUnfocused_WhenTheRightButtonIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");
        var box = (await Box(page, "Notes").BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + 3, box.Y + box.Height / 2, new() { Button = MouseButton.Right });

        await Expect(page.GetByLabel("Notes", new() { Exact = true })).Not.ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldLeaveAnOpenListOpen_WhenItsPaddingIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");
        var town = page.GetByRole(AriaRole.Combobox, new() { Name = "Town", Exact = true });
        await Expect(town).ToHaveAttributeAsync("data-ready", "");
        await town.PressSequentiallyAsync("pine");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeVisibleAsync();

        await PressPaddingAsync(page, "Town");
        // A close would be waiting on a timer, which fires before this one.
        await page.EvaluateAsync("() => new Promise(resolve => setTimeout(resolve))");

        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeVisibleAsync();
        await Expect(town).ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldLeaveItsPanelOpenAndItsInputUnfocused_WhenThePanelIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "Choose dates" });
        await Expect(trigger).ToHaveAttributeAsync("data-ready", "");
        await trigger.ClickAsync();
        var panel = page.GetByRole(AriaRole.Dialog, new() { Name = "Choose dates" });
        await Expect(panel.Locator("[data-part=day][tabindex='0']")).ToBeFocusedAsync();
        var box = (await panel.BoundingBoxAsync())!;

        await page.Mouse.ClickAsync(box.X + 3, box.Y + 3);

        await Expect(panel).ToBeVisibleAsync();
        await Expect(page.Locator("[data-part=start-input]")).Not.ToBeFocusedAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldCloseAnOpenList_WhenItsClearButtonIsPressedWithFocusInTheInput(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");
        var town = page.GetByRole(AriaRole.Combobox, new() { Name = "Town", Exact = true });
        await Expect(town).ToHaveAttributeAsync("data-ready", "");
        await town.PressSequentiallyAsync("pine");
        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeVisibleAsync();

        await Box(page, "Town").GetByRole(AriaRole.Button).ClickAsync();

        await Expect(page.GetByRole(AriaRole.Listbox, new() { Name = "Town" })).ToBeHiddenAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Box_ShouldLeaveFocusWithAControlInsideIt_WhenTheControlIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/field-box");

        await page.GetByLabel("Unit", new() { Exact = true }).ClickAsync();

        await Expect(page.GetByLabel("Unit", new() { Exact = true })).ToBeFocusedAsync();
    }
}
