using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbSortable</c>, found by its label.</summary>
public sealed class TbSortableLocator(ILocator scope, string name)
{
    private ILocator List => scope.GetByRole(AriaRole.List, new() { Name = name, Exact = true })
        .And(scope.Locator("[data-sortable]"));

    private ILocator Items => List.Locator(":scope > [data-part=item]");

    /// <summary>The items' text, in the order they show.</summary>
    public async Task<IReadOnlyList<string>> ItemsAsync() =>
        [.. (await Items.Locator(":scope > .tb-sortable__content").AllInnerTextsAsync()).Select(text => text.Trim())];

    /// <summary>Moves the item whose text is exactly <paramref name="item"/> to <paramref name="index"/>, through its
    /// handle's menu as a user would: Move to start or Move to end for the first or last place, else Move up or Move
    /// down a place at a time, so <c>CanMove</c> and <c>OnMove</c> see each step. Waits for each step's answer, and a
    /// step the app refuses ends the move. Throws when the list is disabled or <paramref name="index"/> is past its
    /// last item.</summary>
    public Task MoveAsync(string item, int index) =>
        HandleMoves.MoveAsync(List, Handle(item), () => PlaceAsync(item), Items.CountAsync, index, item);

    /// <summary>Drags the item whose text is exactly <paramref name="item"/> by its handle onto the top half of the
    /// item <paramref name="target"/>, to move it just before that item, and waits for the app's answer.</summary>
    public Task DragBeforeAsync(string item, string target) => DragAsync(item, target, before: true);

    /// <summary>Drags the item whose text is exactly <paramref name="item"/> by its handle onto the bottom half of the
    /// item <paramref name="target"/>, to move it just after that item, and waits for the app's answer.</summary>
    public Task DragAfterAsync(string item, string target) => DragAsync(item, target, before: false);

    private async Task DragAsync(string item, string target, bool before) =>
        await HandleMoves.DragAsync(List.Locator("xpath=.."), List,
            Handle(item), Item(target), before, await PlaceAsync(item), await PlaceAsync(target), item);

    private ILocator Item(string text) => Items.Locator(":scope > .tb-sortable__content")
        .Filter(new() { HasTextRegex = new Regex($"^{Regex.Escape(text)}$") }).Locator("xpath=parent::*");

    private ILocator Handle(string item) => Item(item).Locator(":scope > [data-part=handle] > [aria-haspopup=menu]");

    private Task<int> PlaceAsync(string item) => Item(item).EvaluateAsync<int>(
        "item => [...item.parentElement.children].filter(child => child.dataset.part === 'item').indexOf(item)");
}

// Moves an item or a grid's row by its handle, through the handle's menu or by a drag. The menu's items are Move up,
// Move down, Move to start and Move to end in that order, so they're found by place whatever the app's labels call
// them. The list or grid counts the moves it has answered, so each waits for its own.
internal static class HandleMoves
{
    public static async Task MoveAsync(ILocator answers, ILocator trigger, Func<Task<int>> place,
        Func<Task<int>> count, int index, string name)
    {
        // A grid whose rows can't move has no count, so finding the row first throws the reason, not an out-of-range
        // index.
        var from = await place();
        var last = await count() - 1;
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, last);
        while (from != index)
        {
            await ThrowIfLockedAsync(trigger, name);
            // The menu's id changes as the page turns interactive, so it's read once the trigger is ready.
            await Scope.ReadyAsync(trigger.Locator("xpath=.."), trigger);
            var menu = Scope.Document(answers).Locator($"[id='{await trigger.GetAttributeAsync("aria-controls")}']");
            var choice = index == 0 ? 2 : index == last ? 3 : index < from ? 0 : 1;
            var step = (await menu.Locator("[role=menuitem]").Nth(choice).TextContentAsync())!.Trim();
            var handle = await trigger.GetAttributeAsync("aria-label");
            await Commit.WaitAsync(answers, async () =>
            {
                await new TbMenuLocator(trigger.Locator("xpath=.."), handle!).ChooseAsync(step);
                return true;
            });
            var now = await place();
            if (now == from)
            {
                return;
            }

            from = now;
        }
    }

    // The item lands before the one under the pointer over its top half, else after it; a drop that leaves it where it
    // is isn't sent to the app, so there's no answer to wait for.
    public static async Task DragAsync(ILocator ready, ILocator answers, ILocator trigger, ILocator target, bool before,
        int from, int onto, string name)
    {
        await ThrowIfLockedAsync(trigger, name);
        await Assertions.Expect(ready).ToHaveAttributeAsync("data-ready", "");
        var slot = onto + (before ? 0 : 1);
        await Commit.WaitAsync(answers, async () =>
        {
            await target.EvaluateAsync("target => target.scrollIntoView({ block: 'center' })");
            await trigger.ScrollIntoViewIfNeededAsync();
            // The pointer presses the handle and drops in the handle's column, since a grid's row can be wider than the
            // view. The page's script finds the drop target under the pointer, so the handle and the target must each
            // be what's there: not out of view, clipped by a scroll box or under a sticky header. The check runs in the
            // element's own frame, whose coordinates can differ from the page coordinates the mouse uses.
            var start = (await trigger.BoundingBoxAsync())!;
            var x = await trigger.EvaluateAsync<double>(
                "handle => handle.getBoundingClientRect().left + handle.getBoundingClientRect().width / 2");
            var at = before ? 0.25f : 0.75f;
            const string underPointer = """
                (element, [x, at]) => {
                    const box = element.getBoundingClientRect();
                    return element.contains(document.elementFromPoint(x, box.top + box.height * at));
                }
                """;
            if (!await trigger.EvaluateAsync<bool>(underPointer, new[] { x, 0.5 })
                || !await target.EvaluateAsync<bool>(underPointer, new[] { x, (double)at }))
            {
                throw new InvalidOperationException($"\"{name}\" can't be dragged there: its handle or its target is "
                    + "out of view or covered. Move it with MoveAsync, or MoveRowAsync for a grid's row.");
            }

            await PointerDrag.DragAsync(trigger.Page, start, async () => (await target.BoundingBoxAsync())!, at,
                alongSource: true);
            return slot != from && slot != from + 1;
        });
    }

    private static async Task ThrowIfLockedAsync(ILocator trigger, string name)
    {
        if (await trigger.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException($"\"{name}\" can't move on a static page; render it on an interactive "
                + "page.");
        }

        if (await trigger.IsDisabledAsync())
        {
            throw new InvalidOperationException($"\"{name}\" can't move, because its list is disabled.");
        }
    }
}
