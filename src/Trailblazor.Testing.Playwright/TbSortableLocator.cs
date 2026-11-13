using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbSortable</c>, found by its label. Its items are named by the list's <c>ItemLabel</c>, or by their
/// text when it has none.</summary>
public sealed class TbSortableLocator(ILocator scope, string name)
{
    private ILocator List => scope.GetByRole(AriaRole.List, new() { Name = name, Exact = true })
        .And(scope.Locator("[data-sortable]"));

    private ILocator Items => List.Locator(":scope > [data-part=item]");

    /// <summary>The items' names, in the order they show.</summary>
    public async Task<IReadOnlyList<string>> ItemsAsync() => await Items.EvaluateAllAsync<string[]>("items => items.map("
        + "item => item.dataset.text ?? item.querySelector(':scope > .tb-sortable__content').innerText.trim())");

    /// <summary>Moves the item named <paramref name="item"/> to <paramref name="index"/>, through its handle's menu as
    /// a user would: Move to start or Move to end for the first or last place, else Move up or Move down a place at a
    /// time, so <c>CanMove</c> and <c>OnMove</c> see each step. Waits for each step's answer, and a step the app
    /// refuses ends the move. Throws when the list is disabled or <paramref name="index"/> is past its last
    /// item.</summary>
    public Task MoveAsync(string item, int index) =>
        HandleMoves.MoveAsync(List, Handle(item), () => PlaceAsync(item), Items.CountAsync, index, item);

    /// <summary>Drags the item named <paramref name="item"/> by its handle onto the top half of the item
    /// <paramref name="target"/>, to move it just before that item, and waits for the app's answer.</summary>
    public Task DragBeforeAsync(string item, string target) => DragAsync(item, target, before: true);

    /// <summary>Drags the item named <paramref name="item"/> by its handle onto the bottom half of the item
    /// <paramref name="target"/>, to move it just after that item, and waits for the app's answer.</summary>
    public Task DragAfterAsync(string item, string target) => DragAsync(item, target, before: false);

    /// <summary>Moves the item named <paramref name="item"/> to the end of the list labeled <paramref name="list"/> in
    /// its group, through its handle's menu as a user would, and waits for the app's answer. Throws when either list
    /// is disabled or no other list in the group has that label.</summary>
    public Task MoveToListAsync(string item, string list) =>
        HandleMoves.ChooseAsync(List, Handle(item), item, async menu =>
        {
            foreach (var choice in await menu.Locator("[role=menuitem][data-list]").AllAsync())
            {
                if (await choice.GetAttributeAsync("data-list") == list)
                {
                    return choice;
                }
            }

            throw new InvalidOperationException($"\"{item}\" can't move to \"{list}\": no other list in its group has "
                + "that label.");
        });

    /// <summary>Drags the item named <paramref name="item"/> by its handle into the list labeled
    /// <paramref name="list"/> in its group, onto the top half of the item <paramref name="before"/> to land just
    /// before it, or below that list's last item (onto the list itself when it's empty) when
    /// <paramref name="before"/> is null, and waits for the app's answer. Throws when either list is disabled, when
    /// no other list in its group has that label, or when the item <paramref name="before"/> isn't there.</summary>
    public async Task DragToListAsync(string item, string list, string? before = null)
    {
        var other = new TbSortableLocator(scope, list);
        if (await other.List.CountAsync() is var found and not 1)
        {
            throw new InvalidOperationException($"Expected one sortable list labeled \"{list}\", but found {found}.");
        }

        // Only another list of the item's group takes it.
        var group = await List.GetAttributeAsync("data-group");
        if (list == name || group is null || await other.List.GetAttributeAsync("data-group") != group)
        {
            throw new InvalidOperationException($"\"{item}\" can't move to \"{list}\", which isn't another list of its "
                + "group.");
        }

        if (await other.List.Locator("xpath=..").EvaluateAsync<bool>(
            "root => root.classList.contains('tb-sortable--disabled')"))
        {
            throw new InvalidOperationException($"\"{item}\" can't move to \"{list}\", because that list is disabled.");
        }

        if (before is not null && await other.Item(before).CountAsync() != 1)
        {
            throw new InvalidOperationException($"\"{list}\" has no item named \"{before}\".");
        }

        var (target, at) = before is not null ? (other.Item(before), 0.25f)
            : await other.Items.CountAsync() > 0 ? (other.Items.Last, 0.75f) : (other.List, 0.5f);
        await HandleMoves.DragOntoAsync(List.Locator("xpath=.."), List, Handle(item), target, at, alongSource: false,
            answered: true, item);
    }

    private async Task DragAsync(string item, string target, bool before) =>
        await HandleMoves.DragAsync(List.Locator("xpath=.."), List,
            Handle(item), Item(target), before, await PlaceAsync(item), await PlaceAsync(target), item);

    private ILocator Item(string name) => List.Locator(
            $":scope > [data-part=item][data-text=\"{name.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"]")
        .Or(List.Locator(":scope > [data-part=item]:not([data-text]) > .tb-sortable__content")
            .Filter(new() { HasTextRegex = new Regex($"^{Regex.Escape(name)}$") }).Locator("xpath=parent::*"));

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
            var choice = index == 0 ? 2 : index == last ? 3 : index < from ? 0 : 1;
            await ChooseAsync(answers, trigger, name,
                menu => Task.FromResult(menu.Locator("[role=menuitem]").Nth(choice)));
            var now = await place();
            if (now == from)
            {
                return;
            }

            from = now;
        }
    }

    // Opens the handle's menu, chooses the menu item that pick returns, and waits for the app's answer.
    public static async Task ChooseAsync(ILocator answers, ILocator trigger, string name,
        Func<ILocator, Task<ILocator>> pick)
    {
        await ThrowIfLockedAsync(trigger, name);
        // The menu's id changes as the page turns interactive, so it's read once the trigger is ready.
        await Scope.ReadyAsync(trigger.Locator("xpath=.."), trigger);
        var menu = Scope.Document(answers).Locator($"[id='{await trigger.GetAttributeAsync("aria-controls")}']");
        var step = (await (await pick(menu)).TextContentAsync())!.Trim();
        var handle = await trigger.GetAttributeAsync("aria-label");
        await Commit.WaitAsync(answers, async () =>
        {
            await new TbMenuLocator(trigger.Locator("xpath=.."), handle!).ChooseAsync(step);
            return true;
        });
    }

    // The item lands before the one under the pointer over its top half, else after it; a drop that leaves it where it
    // is isn't sent to the app, so there's no answer to wait for.
    public static Task DragAsync(ILocator ready, ILocator answers, ILocator trigger, ILocator target, bool before,
        int from, int onto, string name)
    {
        var slot = onto + (before ? 0 : 1);
        return DragOntoAsync(ready, answers, trigger, target, before ? 0.25f : 0.75f, alongSource: true,
            answered: slot != from && slot != from + 1, name);
    }

    // Drags the handle onto the target, at the fraction at of the target's height, in the handle's column when
    // alongSource or else the target's, and waits for the app's answer when answered.
    public static async Task DragOntoAsync(ILocator ready, ILocator answers, ILocator trigger, ILocator target,
        float at, bool alongSource, bool answered, string name)
    {
        await ThrowIfLockedAsync(trigger, name);
        await Assertions.Expect(ready).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(answers, async () =>
        {
            await target.EvaluateAsync("target => target.scrollIntoView({ block: 'center' })");
            await trigger.ScrollIntoViewIfNeededAsync();
            // Within a list the pointer drops in the handle's column, since a grid's row can be wider than the view.
            // The page's script finds the drop target under the pointer, so the handle and the target must each be
            // what's there: not out of view, clipped by a scroll box or under a sticky header. The check runs in the
            // element's own frame, whose coordinates can differ from the page coordinates the mouse uses.
            var start = (await trigger.BoundingBoxAsync())!;
            const string middle =
                "element => element.getBoundingClientRect().left + element.getBoundingClientRect().width / 2";
            var x = await trigger.EvaluateAsync<double>(middle);
            var dropX = alongSource ? x : await target.EvaluateAsync<double>(middle);
            const string underPointer = """
                (element, [x, at]) => {
                    const box = element.getBoundingClientRect();
                    return element.contains(document.elementFromPoint(x, box.top + box.height * at));
                }
                """;
            if (!await trigger.EvaluateAsync<bool>(underPointer, new[] { x, 0.5 })
                || !await target.EvaluateAsync<bool>(underPointer, new[] { dropX, (double)at }))
            {
                throw new InvalidOperationException($"\"{name}\" can't be dragged there: its handle or its target is "
                    + "out of view or covered. Move it with MoveAsync, or MoveRowAsync for a grid's row.");
            }

            await PointerDrag.DragAsync(trigger.Page, start, async () => (await target.BoundingBoxAsync())!, at,
                alongSource);
            return answered;
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
