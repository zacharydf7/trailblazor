namespace Trailblazor.BrowserTests;

public class CodeFieldTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    private async Task<ILocator> OpenCodeAsync(IPage page)
    {
        var code = page.GetByLabel("Verification code");
        await Expect(code).ToHaveAttributeAsync("data-ready", "");
        return code;
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Field_ShouldTakeOnlyTheCodesCharacters_WhenTyped(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field");
        var code = await OpenCodeAsync(page);
        await code.FocusAsync();

        // A letter blocked mid-code leaves the caret where it was, so the next digit lands there.
        await page.Keyboard.TypeAsync("24810");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.PressAsync("ArrowLeft");
        await page.Keyboard.TypeAsync("a7");
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.TypeAsync("6");

        await Expect(code).ToHaveValueAsync("246810");
        await Expect(page.GetByLabel("Result")).ToHaveTextAsync("Code accepted.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Paste_ShouldKeepOnlyTheDigits_WhenTheCodeHasADash(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field");
        var code = await OpenCodeAsync(page);

        await code.EvaluateAsync("""
            input => {
                const data = new DataTransfer();
                data.setData('text', '123-456');
                input.focus();
                // Firefox ignores clipboardData passed to the constructor, so set it as a property instead.
                const paste = new ClipboardEvent('paste', { bubbles: true, cancelable: true });
                Object.defineProperty(paste, 'clipboardData', { value: data });
                input.dispatchEvent(paste);
            }
            """);

        await Expect(code).ToHaveValueAsync("123456");
        await Expect(page.GetByLabel("Result")).ToHaveTextAsync("123456 isn't the code.");
    }
}
