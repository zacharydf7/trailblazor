using Trailblazor.Testing.Playwright;

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
    public async Task Helper_ShouldEnterTheCodeAndWaitUntilTheAppHasIt_WhenThePageIsInteractive(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field");

        await page.Tb().TextField("Verification code").EnterAsync("246810");

        Assert.Equal("Code accepted.", await page.GetByLabel("Result").TextContentAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldEnterOnlyWhatTheFieldTakes_WhenTheTextHasOtherCharacters(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field");
        var field = page.Tb().TextField("Verification code");

        await field.EnterAsync("abc");
        var ignored = await field.TextAsync();
        await field.EnterAsync("24-6");

        Assert.Equal("", ignored);
        Assert.Equal("246", await field.TextAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Field_ShouldCommitTheTextItsScriptPutIn_WhenEnterCommittedEarlierInTheSameFocus(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field");
        var code = await OpenCodeAsync(page);
        await code.FillAsync("12");
        await code.PressAsync("Enter");

        await page.Tb().TextField("Verification code").EnterAsync("9-8");

        Assert.Equal("98", await page.Tb().TextField("Verification code").TextAsync());
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldEnterTheCodeOnceInteractive_WhenThePageIsStillPrerendered(string browser)
    {
        var (page, held) = await OpenHeldAsync(browser, "/examples/code-field");

        var entering = page.Tb().TextField("Verification code").EnterAsync("246810");
        await page.WaitForTimeoutAsync(300);
        held.SetResult();
        await entering;

        await Expect(page.GetByLabel("Result")).ToHaveTextAsync("Code accepted.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Helper_ShouldFillTheField_WhenThePageIsStatic(string browser)
    {
        var page = await OpenStaticAsync(browser, "/static-code-field");

        await page.Tb().TextField("Verification code").EnterAsync("246810");

        await Expect(page.GetByLabel("Verification code")).ToHaveValueAsync("246810");
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
    public async Task Field_ShouldTakeEachDigitAsItsAsciiDigit_WhenTypedOrComposedInAnotherScript(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field");
        var code = await OpenCodeAsync(page);
        await code.FocusAsync();

        await page.Keyboard.TypeAsync("٢٤٦");
        await code.EvaluateAsync("""
            input => {
                input.value += '۸۱۰';
                input.dispatchEvent(new CompositionEvent('compositionend', { data: '۸۱۰' }));
            }
            """);

        await Expect(code).ToHaveValueAsync("246810");
        await Expect(page.GetByLabel("Result")).ToHaveTextAsync("Code accepted.");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Field_ShouldKeepASelectedCode_WhenAKeyItDoesntTakeIsPressed(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field");
        var code = await OpenCodeAsync(page);
        await code.FocusAsync();
        await page.Keyboard.TypeAsync("246810");

        await code.SelectTextAsync();
        await page.Keyboard.TypeAsync("a");

        await Expect(code).ToHaveValueAsync("246810");
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

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Field_ShouldDrawItsSlotsInTheTextColor_WhenColorsAreForced(string browser)
    {
        var page = await OpenAsync(browser, "/examples/code-field", new() { ForcedColors = ForcedColors.Active });
        var code = await OpenCodeAsync(page);

        var slots = await code.EvaluateAsync<string>("code => getComputedStyle(code).backgroundImage");

        Assert.Contains(await SystemColorAsync(page, "CanvasText"), slots);
    }
}
