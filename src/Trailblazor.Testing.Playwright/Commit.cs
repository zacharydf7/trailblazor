using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

internal static class Commit
{
    // A field's root, a tab list, a sortable list, a grid or a toaster counts the commits the app has answered, so a
    // helper waits until the one its action raised lands. The action says whether it raised one: text the field already
    // holds raises no change.
    public static Task WaitAsync(ILocator control, Func<Task<bool>> act, int handlers = 1) =>
        CountedAsync(control.Locator("xpath=ancestor-or-self::*[@data-commits][1]"), "data-commits", act, handlers);

    // Marks the counter a wait is on and the dialogs open before the action, and reads the count so far.
    private const string MarkAndRead = """
        (counter, [mark, count]) => {
            counter.dataset.tbAwaited = mark;
            for (const dialog of counter.ownerDocument.querySelectorAll('dialog[open]')) {
                dialog.dataset.tbOpenBefore = mark;
            }

            return counter.getAttribute(count);
        }
        """;

    // A TbButton, TbIconButton or TbLink counts its finished OnClick, and a toggle's PressedChanged, in data-clicks.
    public static Task ClickedAsync(ILocator button) => CountedAsync(button, "data-clicks", async () =>
    {
        await button.ClickAsync();
        return true;
    });

    // Waits until the count in the counter's attribute has moved by the number of handlers the action raises, which it
    // does once the app's handlers have finished; a double-click raises three, its two clicks and the double-click. The
    // wait also ends when the counter leaves the page, as when the app re-keys a control or a reload replaces the page,
    // and when a dialog opens, since a handler that awaits the dialog's answer finishes only after the test answers
    // the dialog.
    public static async Task CountedAsync(ILocator counter, string count, Func<Task<bool>> act, int handlers = 1)
    {
        var mark = Guid.NewGuid().ToString("N");
        var before = await counter.EvaluateAsync<string?>(MarkAndRead, new[] { mark, count });
        var frame = await FrameAsync(counter);
        // A counter with no count yet, as on a static page, has no handlers to wait for.
        if (await act() && int.TryParse(before, out var start))
        {
            await EndAsync(frame, mark, $"number(@{count}) >= {start + handlers}");
        }
    }

    // Waits as CountedAsync does on a counter, and then on a second one the same action also moves, as a submit
    // button's click moves its form's count once the form has handled the submit. A dialog that opens ends both waits.
    public static async Task CountedAsync(ILocator counter, string count, ILocator next, string nextCount,
        Func<Task<bool>> act)
    {
        var mark = Guid.NewGuid().ToString("N");
        var before = await counter.EvaluateAsync<string?>(MarkAndRead, new[] { mark, count });
        var nextBefore = await next.EvaluateAsync<string?>("""
            (next, [mark, count]) => {
                next.dataset.tbAwaited = mark;
                return next.getAttribute(count);
            }
            """, new[] { $"{mark}-next", nextCount });
        var frame = await FrameAsync(counter);
        if (!await act())
        {
            return;
        }

        // A counter with no count yet, as on a static page, has no handlers to wait for.
        if (int.TryParse(before, out var start))
        {
            await EndAsync(frame, mark, $"number(@{count}) >= {start + 1}");
        }

        if (int.TryParse(nextBefore, out var nextStart))
        {
            await EndAsync(frame, $"{mark}-next", $"number(@{nextCount}) >= {nextStart + 1}", mark);
        }
    }

    // Waits as CountedAsync does, but on whichever counter the action's event reaches rather than one known
    // beforehand, since several hotkeys can share keys and only one fires. If the event reaches no counter, the wait
    // returns at once.
    public static async Task FiredAsync(ILocator anchor, string eventName, string count, Func<Task> act)
    {
        var mark = Guid.NewGuid().ToString("N");
        await anchor.EvaluateAsync("""
            (anchor, [mark, eventName, count]) => {
                const page = anchor.ownerDocument;
                for (const dialog of page.querySelectorAll('dialog[open]')) {
                    dialog.dataset.tbOpenBefore = mark;
                }

                // An earlier action whose event reached no counter left its listener behind.
                page.removeEventListener(eventName, page.tbFiredWatch, { capture: true });
                page.tbFiredWatch = event => {
                    event.target.dataset.tbAwaited = mark;
                    event.target.dataset.tbStart = event.target.getAttribute(count);
                };
                page.addEventListener(eventName, page.tbFiredWatch, { capture: true, once: true });
            }
            """, new[] { mark, eventName, count });
        var frame = await FrameAsync(anchor);
        await act();
        await EndAsync(frame, mark, $"number(@{count}) > number(@data-tb-start)");
    }

    private static async Task<IFrame> FrameAsync(ILocator element)
    {
        await using var handle = await element.ElementHandleAsync();
        return (await handle.OwnerFrameAsync())!;
    }

    // Ends when the awaited element meets the landed condition or leaves the page, or when a dialog opens. The wait
    // looks through the frame rather than the element, so it can still see the element leave. The dialogs that were
    // open before carry the mark of the action's first wait.
    private static Task EndAsync(IFrame frame, string mark, string landed, string? openBefore = null) =>
        Assertions.Expect(frame.Locator($"xpath=/html[not(.//*[@data-tb-awaited='{mark}'])]"
            + $" | //*[@data-tb-awaited='{mark}'][{landed}]"
            + $" | //dialog[@open][not(@data-tb-open-before='{openBefore ?? mark}')]").First).ToBeAttachedAsync();
}
