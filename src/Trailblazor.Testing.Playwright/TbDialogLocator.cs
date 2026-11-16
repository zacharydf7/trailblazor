using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A dialog, found by its title: a <c>TbDialog</c> or <c>TbDrawer</c>, or one from <c>IDialogService</c>
/// such as a confirm.</summary>
public sealed class TbDialogLocator(ILocator scope, string? title)
{
    // A dialog that is closing stays on the page while it fades.
    private ILocator Dialog => Dialogs(includeHidden: false).And(scope.Locator(":not([data-leaving])"));

    /// <summary>Waits for the dialog to open and returns it, to find what it shows.</summary>
    public async Task<ILocator> WaitForAsync()
    {
        await Assertions.Expect(Dialog.First).ToBeVisibleAsync();
        if (await Dialog.CountAsync() is > 1 and var count)
        {
            throw new InvalidOperationException(title is null
                ? $"{count} dialogs are open; give the title of the one to use."
                : $"{count} dialogs are titled \"{title}\"; give each its own title.");
        }

        return Dialog;
    }

    /// <summary>Waits until the app has closed the dialog, as it does once a save it started has finished, and the
    /// dialog has faded out and left the page. Returns at once when the dialog isn't open.</summary>
    public async Task WaitForClosedAsync() => await Assertions.Expect(
        Dialogs(includeHidden: true).And(scope.Locator(":is([open], [data-leaving])"))).ToHaveCountAsync(0);

    /// <summary>The dialog's title, which names it, read once the dialog is open.</summary>
    public async Task<string> TitleAsync() => await (await WaitForAsync()).EvaluateAsync<string>(
        "dialog => document.getElementById(dialog.getAttribute('aria-labelledby')).textContent.trim()");

    /// <summary>Whether the dialog is open, without waiting.</summary>
    public async Task<bool> IsOpenAsync() => await Dialog.CountAsync() > 0;

    /// <summary>Whether the dialog shows at its own size, minimized to its header or maximized to fill the window,
    /// read once the dialog is open.</summary>
    public async Task<DialogWindowState> WindowStateAsync() =>
        await (await WaitForAsync()).GetAttributeAsync("data-window-state") switch
        {
            "minimized" => DialogWindowState.Minimized,
            "maximized" => DialogWindowState.Maximized,
            _ => DialogWindowState.Normal,
        };

    /// <summary>Minimizes the dialog with its Minimize button, unless it's minimized already, and waits until it is.
    /// Throws for a dialog without the button, which shows only when the dialog is <c>Minimizable</c>, and on a static
    /// page, where the button does nothing.</summary>
    public Task MinimizeAsync() => SetWindowStateAsync(DialogWindowState.Minimized, "minimize");

    /// <summary>Maximizes the dialog with its Maximize button, unless it's maximized already, and waits until it is.
    /// Throws as <see cref="MinimizeAsync"/> does, for a dialog that isn't <c>Maximizable</c>.</summary>
    public Task MaximizeAsync() => SetWindowStateAsync(DialogWindowState.Maximized, "maximize");

    /// <summary>Brings a minimized or maximized dialog back to its own size with its Restore button, and waits until it
    /// is. Does nothing for a dialog at its own size.</summary>
    public async Task RestoreAsync() => await SetWindowStateAsync(DialogWindowState.Normal,
        await WindowStateAsync() == DialogWindowState.Minimized ? "minimize" : "maximize");

    /// <summary>Clicks the dialog's button with this name, such as a confirm's <c>Delete</c> or a form's <c>Save</c>,
    /// and waits for the dialog to fade out and leave the page, which is when the app gets the answer. For a
    /// <c>TbDialog</c> or <c>TbDrawer</c>, it also waits for the app's <c>OpenChanged</c> to finish. On a static page,
    /// it waits for the dialog to close instead.</summary>
    public async Task ChooseAsync(string button)
    {
        var dialog = await WaitForAsync();
        var open = await MarkAsync(dialog);
        var choice = dialog.GetByRole(AriaRole.Button, new() { Name = button, Exact = true });
        await AnsweredAsync(dialog, () => choice.ClickAsync());
        await Assertions.Expect(open).ToHaveCountAsync(0);
    }

    /// <summary>Dismisses the dialog with Escape, as a keyboard user would, and waits for it to fade out and leave the
    /// page, which is when the app gets the answer. For a <c>TbDialog</c> or <c>TbDrawer</c>, it also waits for the
    /// app's <c>OpenChanged</c> to finish. On a static page, it waits for the dialog to close instead.</summary>
    public async Task CancelAsync()
    {
        var dialog = await WaitForAsync();
        var open = await MarkAsync(dialog);
        await AnsweredAsync(dialog, () => scope.Page.Keyboard.PressAsync("Escape"));
        await Assertions.Expect(open).ToHaveCountAsync(0);
    }

    // A confirm or alert is an alertdialog. A closed dialog on a static page stays on the page, hidden. A popover's or
    // select's panel is a dialog too, so the part tells a dialog or drawer apart.
    private ILocator Dialogs(bool includeHidden)
    {
        ILocator ByRole(AriaRole role) => title is null ? scope.GetByRole(role, new() { IncludeHidden = includeHidden })
            : scope.GetByRole(role, new() { Name = title, Exact = true, IncludeHidden = includeHidden });
        return ByRole(AriaRole.Dialog).Or(ByRole(AriaRole.Alertdialog)).And(scope.Locator("[data-part=dialog]"));
    }

    // The header's Minimize and Maximize buttons each toggle between their state and the dialog's own size. The
    // script sets data-window-state once it has moved the dialog.
    private async Task SetWindowStateAsync(DialogWindowState state, string part)
    {
        var dialog = await WaitForAsync();
        if (await WindowStateAsync() == state)
        {
            return;
        }

        var button = dialog.Locator($"[data-part=header] [data-part={part}]");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException($"This dialog has no {char.ToUpperInvariant(part[0])}{part[1..]} "
                + "button.");
        }

        if (await button.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page the dialog's window buttons do nothing; render the "
                + "page interactively to use them.");
        }

        // A prerendered button is replaced once the page is interactive, and would lose the click.
        await Assertions.Expect(button).ToHaveAttributeAsync("data-clicks", new Regex(@"^\d+$"));
        await Commit.CountedAsync(dialog, "data-window-commits", async () =>
        {
            await button.ClickAsync();
            return true;
        });
        await Assertions.Expect(dialog).ToHaveAttributeAsync("data-window-state",
            state.ToString().ToLowerInvariant());
    }

    // TbRoot's dialog host counts the dialogs the app has answered. A page without one has no count, and a static
    // dialog, which the browser opens and closes, is never counted, even when an interactive TbRoot holds the host.
    private async Task AnsweredAsync(ILocator dialog, Func<Task> act)
    {
        var host = scope.Page.Locator("[data-tb-answered-dialogs]");
        if (await host.CountAsync() == 0 || await dialog.GetAttributeAsync("closedby") is not null)
        {
            await act();
            return;
        }

        await Commit.CountedAsync(host.First, "data-tb-answered-dialogs", async () =>
        {
            await act();
            return true;
        });
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
