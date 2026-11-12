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

    public async Task<MoveCheck> CheckAsync(IReadOnlyList<TItem> items, int from, int to,
        Func<SortableMove<TItem>, Task<MoveCheck>>? canMove) =>
        from < 0 || from >= items.Count || to < 0 || to >= items.Count || from == to ? new(false)
            : canMove is null ? MoveCheck.Allow : await canMove(new(items[from], from, to));

    // True when the move was made. A move asked for while another runs, such as a held key's repeats, is dropped,
    // so the app isn't asked again about a list that is about to change. items reads the list as it is now.
    public async Task<bool> MoveAsync(Func<IReadOnlyList<TItem>> items, int from, int to,
        Func<SortableMove<TItem>, Task<MoveCheck>>? canMove, EventCallback<SortableMove<TItem>> onMove,
        Func<TItem, string> text, string? moved)
    {
        var before = items();
        if (_moving || from < 0 || from >= before.Count)
        {
            return false;
        }

        _moving = true;
        try
        {
            var item = before[from];
            var check = await CheckAsync(before, from, to, canMove);
            if (!check.Allowed)
            {
                if (check.Reason is { } reason)
                {
                    announcer.Polite(reason);
                }

                return false;
            }

            // The app can change or replace the list while CanMove runs, and then the indexes may no longer fit it.
            var now = items();
            if (from >= now.Count || to >= now.Count || !EqualityComparer<TItem>.Default.Equals(now[from], item))
            {
                return false;
            }

            await onMove.InvokeAsync(new(item, from, to));
            announcer.Polite(string.Format(CultureInfo.CurrentCulture, moved ?? localizer[TbStrings.SortableMoved],
                text(item), to + 1, now.Count));
            return true;
        }
        finally
        {
            _moving = false;
        }
    }
}
