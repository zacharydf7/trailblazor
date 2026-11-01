using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A dialog, found by its title: a <c>TbDialog</c>, or one from <c>IDialogService</c> such as a confirm.
/// </summary>
public sealed class TbDialogLocator(IPage page, string title)
{
    // A confirm or alert is an alertdialog; a dialog that is closing stays on the page while it fades.
    private ILocator Dialog => page.GetByRole(AriaRole.Dialog, new() { Name = title, Exact = true })
        .Or(page.GetByRole(AriaRole.Alertdialog, new() { Name = title, Exact = true }))
        .And(page.Locator(":not([data-leaving])"));

    /// <summary>Waits for the dialog to open and returns it, to find what it shows.</summary>
    public async Task<ILocator> WaitForAsync()
    {
        await Assertions.Expect(Dialog).ToBeVisibleAsync();
        return Dialog;
    }

    /// <summary>Clicks the dialog's button with this name, such as a confirm's <c>Delete</c> or a form's
    /// <c>Save</c>, and waits for the dialog to close.</summary>
    public async Task ChooseAsync(string button)
    {
        var dialog = await WaitForAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = button, Exact = true }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    /// <summary>Dismisses the dialog with Escape, as a keyboard user would, and waits for it to close.</summary>
    public async Task CancelAsync()
    {
        await WaitForAsync();
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Dialog).ToBeHiddenAsync();
    }
}
