using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The toasts on a page, from <c>IToastService</c>.</summary>
public sealed class TbToastsLocator(ILocator scope)
{
    /// <summary>Waits for a toast whose text contains this and returns it, to read it or click its action. When
    /// several match, as when the same action ran twice, this returns the newest.</summary>
    public async Task<ILocator> WaitForAsync(string text)
    {
        // A toast that is leaving stays on the page while it fades; newer toasts come later on the page.
        var toast = scope.Locator("[data-part=toast]:not([data-leaving])").Filter(new() { HasText = text }).Last;
        await Assertions.Expect(toast).ToBeVisibleAsync();
        return toast;
    }

    /// <summary>Waits for a toast whose text contains this, as <see cref="WaitForAsync"/> does, closes it with its
    /// dismiss button and waits until it has started to fade.</summary>
    public async Task DismissAsync(string text)
    {
        var toast = await MarkAsync(await WaitForAsync(text));
        await ClickAsync(toast.Locator("[data-part=close]"));
    }

    /// <summary>Waits for a toast whose text contains this, as <see cref="WaitForAsync"/> does, clicks its action
    /// button, such as <c>Undo</c>, and waits until the app has run the action, or until a dialog opens, since an
    /// action that awaits the dialog's answer finishes only once the test answers it. Throws when the toast's action
    /// has another name, or it has none.</summary>
    public async Task ChooseAsync(string text, string action)
    {
        var toast = await MarkAsync(await WaitForAsync(text));
        // A toast turns inert as it starts to fade, which hides its button from role queries, so the click and the
        // wait find the button by its part.
        var button = toast.Locator("[data-part=action]");
        if (await button.And(toast.GetByRole(AriaRole.Button, new() { Name = action, Exact = true })).CountAsync() == 0)
        {
            throw new InvalidOperationException($"The toast showing \"{text}\" has no action named \"{action}\".");
        }

        // The action runs after its toast has started to fade, and can outlast it, so the wait follows the toaster's
        // count of finished actions.
        await Commit.WaitAsync(button, async () =>
        {
            await button.ClickAsync();
            return true;
        });
    }

    // The toast is marked, since another one may take its place as the newest match once it fades.
    private async Task<ILocator> MarkAsync(ILocator toast)
    {
        var mark = Guid.NewGuid().ToString("N");
        await toast.EvaluateAsync("(toast, mark) => toast.dataset.tbAwaited = mark", mark);
        return scope.Locator($"[data-tb-awaited='{mark}']");
    }

    // A button counts the clicks it has finished handling. The toast starts to fade as the click is handled.
    private static async Task ClickAsync(ILocator button)
    {
        await Assertions.Expect(button).ToHaveAttributeAsync("data-clicks", new Regex(@"^\d+$"));
        await Commit.ClickedAsync(button);
    }
}
