using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

internal static class Commit
{
    // A field's root, a tab list, a sortable list, a grid or a toaster counts the commits the app has answered, so a
    // helper waits until the one its action raised lands. The action says whether it raised one: text the field already
    // holds raises no change.
    public static Task WaitAsync(ILocator control, Func<Task<bool>> act) =>
        CountedAsync(control.Locator("xpath=ancestor-or-self::*[@data-commits][1]"), "data-commits", act);

    // A TbButton or TbIconButton counts its finished OnClick, and a toggle's PressedChanged, in data-clicks.
    public static Task ClickedAsync(ILocator button) => CountedAsync(button, "data-clicks", async () =>
    {
        await button.ClickAsync();
        return true;
    });

    // Waits until the count in the counter's attribute has moved by the number of handlers the action raises, which it
    // does once the app's handlers have finished; a double-click raises three, its two clicks and the double-click. The
    // wait also ends when the counter leaves the page, as when the app re-keys a control or a reload replaces the page,
    // and when a dialog opens, since a handler that awaits the dialog's answer finishes only after the test answers
    // the dialog. The wait looks through the counter's frame rather than the counter itself, so it can still see the
    // counter leave.
    public static async Task CountedAsync(ILocator counter, string count, Func<Task<bool>> act, int handlers = 1)
    {
        var mark = Guid.NewGuid().ToString("N");
        var before = await counter.EvaluateAsync<string?>("""
            (counter, [mark, count]) => {
                counter.dataset.tbAwaited = mark;
                for (const dialog of counter.ownerDocument.querySelectorAll('dialog[open]')) {
                    dialog.dataset.tbOpenBefore = mark;
                }

                return counter.getAttribute(count);
            }
            """, new[] { mark, count });
        IFrame frame;
        await using (var handle = await counter.ElementHandleAsync())
        {
            frame = (await handle.OwnerFrameAsync())!;
        }

        // A counter with no count yet, as on a static page, has no handlers to wait for.
        if (await act() && int.TryParse(before, out var start))
        {
            await Assertions.Expect(frame.Locator($"xpath=/html[not(.//*[@data-tb-awaited='{mark}'])]"
                + $" | //*[@data-tb-awaited='{mark}'][number(@{count}) >= {start + handlers}]"
                + $" | //dialog[@open][not(@data-tb-open-before='{mark}')]").First).ToBeAttachedAsync();
        }
    }
}
