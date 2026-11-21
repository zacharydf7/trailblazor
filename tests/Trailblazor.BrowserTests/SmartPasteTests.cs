using System.Text.Json;
using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class SmartPasteTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private const string Signature = "Robin Vale, robin@ridge.example, 555-0100, a party of 4, steady pace";

    private const string Refused =
        "navigator.clipboard.readText = () => Promise.reject(new DOMException('Refused', 'NotAllowedError'));";

    private static string Sharing(string text) =>
        $"navigator.clipboard.readText = () => Promise.resolve({JsonSerializer.Serialize(text)});";

    private async Task<IPage> OpenWithAsync(string browser, string? script = null)
    {
        var page = await OpenAsync(browser, "/examples/smart-paste");
        if (script is not null)
        {
            await page.AddInitScriptAsync(script);
            await page.ReloadAsync();
        }

        return page;
    }

    private static async Task ExpectFilledAsync(IPage page)
    {
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Leader's name" })).ToHaveValueAsync("Robin Vale");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Email" })).ToHaveValueAsync("robin@ridge.example");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Phone" })).ToHaveValueAsync("555-0100");
        await Expect(page.GetByRole(AriaRole.Spinbutton, new() { Name = "Party size" })).ToHaveValueAsync("4");
        await Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Steady" })).ToBeCheckedAsync();
        await Expect(page.Locator(".tb-smart-paste [data-part=status]")).ToHaveTextAsync("Filled 5 fields");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SmartPaste_ShouldFillTheFormFromTheClipboard_WhenTheBrowserSharesIt(string browser)
    {
        var page = await OpenWithAsync(browser, Sharing(Signature));

        await page.Tb().SmartPaste().Button.ClickAsync();

        await ExpectFilledAsync(page);
        await Expect(page.Locator("dialog.tb-smart-paste__box")).ToHaveCountAsync(0);
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task SmartPaste_ShouldAskForTheTextAndGiveFocusBack_WhenTheBrowserRefusesTheClipboard(string browser)
    {
        var page = await OpenWithAsync(browser, Refused);
        var paste = page.Tb().SmartPaste();

        // The button turns on once the page is interactive.
        await Expect(paste.Button).ToBeEnabledAsync();
        await paste.Button.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        var text = page.GetByRole(AriaRole.Dialog, new() { Name = "Paste text to fill the form" })
            .GetByRole(AriaRole.Textbox, new() { Name = "Text to fill from" });
        await Expect(text).ToBeFocusedAsync();
        await page.Keyboard.InsertTextAsync(Signature);
        await page.GetByRole(AriaRole.Button, new() { Name = "Fill", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");

        await ExpectFilledAsync(page);
        await Expect(paste.Button).ToBeFocusedAsync();
    }

    // Without a stand-in clipboard, so the locator has to make the page's own refuse in every browser.
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PasteAsync_ShouldFillTheFormThroughTheBox_WhenCalled(string browser)
    {
        var page = await OpenWithAsync(browser);

        await page.Tb().SmartPaste().PasteAsync(Signature);

        await ExpectFilledAsync(page);
        Assert.Equal("Filled 5 fields", await page.Tb().SmartPaste().StatusAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task PasteAsync_ShouldWaitUntilTheFormIsFilled_WhenTheModelTakesAMoment(string browser)
    {
        var page = await OpenAsync(browser, "/smart-paste");

        await page.Tb().SmartPaste().PasteAsync("Robin Vale");

        // Reads that don't retry, so they'd fail if PasteAsync returned before the model answered.
        Assert.Equal("Filled 1 field", await page.Tb().SmartPaste().StatusAsync());
        Assert.Equal("Robin Vale", await page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).InputValueAsync());
    }
}
