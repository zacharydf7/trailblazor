using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// Asks whether a move is allowed, has the app make it, and says what happened, for TbSortable and the data grid's rows.
internal sealed class ItemMover<TItem>(IAnnouncer announcer, ITbLocalizer localizer)
{
    private bool _moving;

    // Where a move key sends the item at index, or null when it would stay where it is.
    public static int? TargetOf(string key, int index, int count)
    {
        var to = key switch
        {
            "ArrowUp" => index - 1,
            "ArrowDown" => index + 1,
            "Home" => 0,
            "End" => count - 1,
            _ => index,
        };
        return to < 0 || to >= count || to == index ? null : to;
    }

    // Where an item dropped before or after the one at target ends up, or null when it would stay where it is.
    public static int? DropIndex(int from, int target, bool before)
    {
        var slot = target + (before ? 0 : 1);
        var to = slot > from ? slot - 1 : slot;
        return from < 0 || target < 0 || to == from ? null : to;
    }

    // A list in a sortable group names itself as both ends of a move within it.
    public Task<MoveCheck> CheckAsync(IReadOnlyList<TItem> items, int from, int to,
        Func<SortableMove<TItem>, Task<MoveCheck>>? canMove, string? list) =>
        CheckAsync(items, from, list, items, to, list, canMove);

    // Whether the item at index from may move to index to of into, as into's canMove decides. Within a
    // list the item must move and stay among its items; another list also takes it just past its last item.
    public static async Task<MoveCheck> CheckAsync(IReadOnlyList<TItem> items, int from, string? fromList,
        IReadOnlyList<TItem> into, int to, string? toList, Func<SortableMove<TItem>, Task<MoveCheck>>? canMove)
    {
        var within = ReferenceEquals(items, into);
        return from < 0 || from >= items.Count || to < 0 || to > LastSlot(into, within) || (within && from == to)
            ? new(false)
            : canMove is null ? MoveCheck.Allow
            : await canMove(new(items[from], from, to) { FromList = fromList, ToList = toList });
    }

    public Task<bool> MoveAsync(Func<IReadOnlyList<TItem>> items, int from, int to,
        Func<SortableMove<TItem>, Task<MoveCheck>>? canMove, EventCallback<SortableMove<TItem>> onMove,
        Func<TItem, string> text, string? moved, string? list) =>
        MoveAsync(items, from, list, items, to, list, canMove, onMove, (item, count) => string.Format(
            CultureInfo.CurrentCulture, moved ?? localizer[TbStrings.SortableMoved], text(item), to + 1, count));

    // True when the move was made. A move asked for while another runs, such as a held key's repeats, is dropped,
    // so the app isn't asked again about a list that is about to change. items and into read the lists as they are
    // now; the same function for both means the item stays in its list. said builds the announcement from the
    // item and the number of items in the list it went to.
    public async Task<bool> MoveAsync(Func<IReadOnlyList<TItem>> items, int from, string? fromList,
        Func<IReadOnlyList<TItem>> into, int to, string? toList, Func<SortableMove<TItem>, Task<MoveCheck>>? canMove,
        EventCallback<SortableMove<TItem>> onMove, Func<TItem, int, string> said)
    {
        var before = items();
        if (_moving || from < 0 || from >= before.Count)
        {
            return false;
        }

        _moving = true;
        try
        {
            var within = items == into;
            var item = before[from];
            var check = await CheckAsync(before, from, fromList, within ? before : into(), to, toList, canMove);
            if (!check.Allowed)
            {
                if (check.Reason is { } reason)
                {
                    announcer.Polite(reason);
                }

                return false;
            }

            // The app can change or replace either list while CanMove runs, and then the indexes may no longer fit.
            var now = items();
            var target = within ? now : into();
            if (from >= now.Count || to > LastSlot(target, within)
                || !EqualityComparer<TItem>.Default.Equals(now[from], item))
            {
                return false;
            }

            var count = target.Count + (within ? 0 : 1);
            await onMove.InvokeAsync(new(item, from, to) { FromList = fromList, ToList = toList });
            announcer.Polite(said(item, count));
            return true;
        }
        finally
        {
            _moving = false;
        }
    }

    private static int LastSlot(IReadOnlyList<TItem> into, bool within) => within ? into.Count - 1 : into.Count;
}
