using Trailblazor.Testing.Playwright;

namespace Trailblazor.BrowserTests;

public class HotkeysTests(BrowserFixture fixture) : BrowserTest(fixture)
{
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Hotkey_ShouldFireAndPreventTheBrowsersAction_WhenItsKeysArePressedOnThePage(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.EvaluateAsync("() => document.addEventListener('keydown', event => window.prevented = event.defaultPrevented)");

        await page.Tb().Hotkey("Mod+S").PressAsync();

        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("save");
        Assert.True(await page.EvaluateAsync<bool>("() => window.prevented"));
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Hotkey_ShouldFire_WhenItsKeyIsSpace(string browser)
    {
        var page = await OpenReadyAsync(browser);

        await page.Tb().Hotkey("Mod+Space").PressAsync();

        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("search");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task CharacterKey_ShouldFireOnThePageButNotInAField_WhenAllowed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        var note = page.GetByRole(AriaRole.Textbox, new() { Name = "Note" });

        await note.FillAsync("");
        await page.Keyboard.PressAsync("g");
        await Expect(note).ToHaveValueAsync("g");
        await page.Locator("body").ClickAsync(new() { Position = new() { X = 1, Y = 1 } });
        await page.Keyboard.PressAsync("g");

        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("go");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Hotkey_ShouldLeaveTheKeysAlone_WhenDisabledAndFireOnceEnabled(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.EvaluateAsync("() => document.addEventListener('keydown', event => window.prevented = event.defaultPrevented)");
        await page.Keyboard.PressAsync("Alt+D");
        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("");
        Assert.False(await page.EvaluateAsync<bool>("() => window.prevented"));

        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle Alt+D" }).ClickAsync();
        await Expect(page.Locator("[data-keys='Alt+D']")).Not.ToHaveAttributeAsync("data-disabled", "");
        await page.Keyboard.PressAsync("Alt+D");

        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("disabled");
    }

    // What a keyboard sends that Playwright's doesn't: Alt typing another character, as on a Mac, and Shift with "?".
    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Hotkey_ShouldMatchItsKeysByCodeAndAShiftedSymbol_WhenTheKeyTypesAnotherCharacter(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Toggle Alt+D" }).ClickAsync();
        await Expect(page.Locator("[data-keys='Alt+D']")).Not.ToHaveAttributeAsync("data-disabled", "");
        await page.Locator("body").ClickAsync(new() { Position = new() { X = 1, Y = 1 } });

        await page.EvaluateAsync("""
            () => {
                const press = init => document.body.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, ...init }));
                press({ key: '∂', code: 'KeyD', altKey: true });
                press({ key: ' ', code: 'Space', ctrlKey: true });
                press({ key: '?', code: 'Slash', shiftKey: true });
            }
            """);

        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("disabled, search");
        await Expect(page.GetByRole(AriaRole.Dialog, new() { Name = "Keyboard shortcuts" })).ToBeVisibleAsync();
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ParentHotkey_ShouldWinInsideItsElementAndNotFireOutside_WhenItsKeysArePressed(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Draft" }).FocusAsync();

        await page.Tb().Hotkey("Mod+B").PressAsync();
        await page.Keyboard.PressAsync("ControlOrMeta+S");
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Note" }).FocusAsync();
        await page.Keyboard.PressAsync("ControlOrMeta+B");
        await page.Keyboard.PressAsync("ControlOrMeta+S");

        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("bold, save draft, page bold, save");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task ParentHotkey_ShouldFireANamedKeyInsideAField_WhenFocusIsInIt(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Draft" }).FocusAsync();

        await page.Keyboard.PressAsync("Shift+Enter");

        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("new line");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Hotkey_ShouldFireOnlyInsideTheDialog_WhenAModalDialogIsOpen(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.GetByRole(AriaRole.Button, new() { Name = "Open dialog" }).ClickAsync();
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Trail" });
        await Expect(dialog).ToBeVisibleAsync();

        await page.Keyboard.PressAsync("ControlOrMeta+S");
        await dialog.Tb().Hotkey("Mod+Enter").PressAsync();

        await Expect(page.GetByTestId("pressed")).ToHaveTextAsync("done");
    }

    [Theory, ClassData(typeof(AllBrowsers))]
    public async Task Help_ShouldListTheHotkeysByGroupAndClose_WhenItsKeyIsPressedTwice(string browser)
    {
        var page = await OpenReadyAsync(browser);
        await page.Locator("body").ClickAsync(new() { Position = new() { X = 1, Y = 1 } });

        await page.Tb().Hotkey("?").PressAsync();

        var help = page.GetByRole(AriaRole.Dialog, new() { Name = "Keyboard shortcuts" });
        await Expect(help).ToBeVisibleAsync();
        await Expect(help.GetByRole(AriaRole.Table)).ToHaveCountAsync(2);
        await Expect(help.GetByRole(AriaRole.Table, new() { Name = "Editor" }).GetByRole(AriaRole.Row))
            .ToHaveCountAsync(4);
        await Expect(help.GetByRole(AriaRole.Cell, new() { Name = "Show keyboard shortcuts" })).ToBeVisibleAsync();
        await Expect(help.GetByText("Mod", new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(help.Locator(".tb-kbd__mod-other").First).ToBeVisibleAsync();
        await page.EvaluateAsync("() => document.documentElement.dataset.tbApple = ''");
        await Expect(help.Locator(".tb-kbd__mod-apple").First).ToBeVisibleAsync();
        await Expect(help.Locator(".tb-kbd__mod-other").First).ToBeHiddenAsync();
        await page.Keyboard.PressAsync("?");
        await Expect(help).ToBeHiddenAsync();
    }

    private async Task<IPage> OpenReadyAsync(string browser)
    {
        var page = await OpenAsync(browser, "/hotkeys");
        await Expect(page.Locator("[data-part=hotkey]:not([data-ready])")).ToHaveCountAsync(0);
        return page;
    }
}
