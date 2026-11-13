using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A dialog, found by its title: a <c>TbDialog</c>, or one from <c>IDialogService</c> such as a confirm.
/// </summary>
public sealed class TbDialogLocator(ILocator scope, string title)
{
    // A confirm or alert is an alertdialog; a dialog that is closing stays on the page while it fades.
    private ILocator Dialog => scope.GetByRole(AriaRole.Dialog, new() { Name = title, Exact = true })
        .Or(scope.GetByRole(AriaRole.Alertdialog, new() { Name = title, Exact = true }))
        .And(scope.Locator(":not([data-leaving])"));

    /// <summary>Waits for the dialog to open and returns it, to find what it shows.</summary>
    public async Task<ILocator> WaitForAsync()
    {
        await Assertions.Expect(Dialog).ToBeVisibleAsync();
        return Dialog;
    }

    /// <summary>Clicks the dialog's button with this name, such as a confirm's <c>Delete</c> or a form's <c>Save</c>,
    /// and waits for the dialog to fade out and leave the page, which is when the app gets the answer. On a static page,
    /// it waits for the dialog to close instead.</summary>
    public async Task ChooseAsync(string button)
    {
        var dialog = await WaitForAsync();
        var open = await MarkAsync(dialog);
        await dialog.GetByRole(AriaRole.Button, new() { Name = button, Exact = true }).ClickAsync();
        await Assertions.Expect(open).ToHaveCountAsync(0);
    }

    /// <summary>Dismisses the dialog with Escape, as a keyboard user would, and waits for it to fade out and leave the
    /// page, which is when the app gets the answer. On a static page, it waits for the dialog to close instead.
    /// </summary>
    public async Task CancelAsync()
    {
        var open = await MarkAsync(await WaitForAsync());
        await scope.Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(open).ToHaveCountAsync(0);
    }

    // The app hears the answer once the dialog has faded and left the page. The mark tracks this dialog only, and the
    // wait also ends if a reload takes the page. A static page's dialog stays on the page, closed, and never fades.
    private async Task<ILocator> MarkAsync(ILocator dialog)
    {
        var mark = Guid.NewGuid().ToString("N");
        await dialog.EvaluateAsync("(dialog, mark) => dialog.dataset.tbAwaited = mark", mark);
        return scope.Locator($"[data-tb-awaited='{mark}']:is([open], [data-leaving])");
    }
}
