using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A form field that opens a tree of checkboxes under it for choosing several items from a hierarchy, such as
/// regions or folders, with an optional search. The tree stays open while the user checks items.</summary>
public class TbMultiTreeSelect<TItem, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey>
    : TbTreeSelectBase<TItem, TKey, IReadOnlyList<TKey>> where TKey : notnull
{
    /// <summary>Checking an item checks the items inside it, and a parent shows whether all, some or none of them are
    /// checked. Defaults to true.</summary>
    [Parameter]
    public bool CascadeChecks { get; set; } = true;

    /// <summary>How many checked items the field names before it counts the rest, as in "Pine Hollow, Cedar Flats,
    /// Fern Gully, 4 more", which keeps the field and its accessible name short. Null names them all. Defaults to 3.
    /// </summary>
    [Parameter]
    public int? MaxShown { get; set; } = 3;

    /// <summary>The text that counts the checked items the field doesn't name, with {0} for how many. Defaults to the
    /// app's text for <see cref="TbStrings.TreeSelectMore"/>, "{0} more" in English.</summary>
    [Parameter]
    public string? MoreLabel { get; set; }

    private protected override string? ValueText
    {
        get
        {
            if (Value is not { Count: > 0 } value)
            {
                return null;
            }

            var items = ByKey();
            var shown = value.Take(MaxShown ?? value.Count).Select(key => TextOf(key, items)).ToList();
            if (shown.Count < value.Count)
            {
                shown.Add(string.Format(CultureInfo.CurrentCulture, MoreLabel ?? Localizer[TbStrings.TreeSelectMore],
                    value.Count - shown.Count));
            }

            return string.Join(", ", shown);
        }
    }

    private protected override bool ChecksRows => true;

    private protected override IReadOnlyCollection<TKey> SelectedKeys => [];

    // The tree's checked keys, including headings IsSelectable keeps out of the value, and the value they produced.
    // While Value is still that same list, the tree keeps these keys, so a heading stays checked and reaches items
    // that load later.
    private IReadOnlyCollection<TKey> _treeKeys = [];
    private IReadOnlyList<TKey>? _treeValue;

    private protected override IReadOnlyCollection<TKey> CheckedKeys =>
        ReferenceEquals(Value, _treeValue) ? _treeKeys : Value ?? [];

    private protected override bool CascadesChecks => CascadeChecks;

    // A heading checked without changing the value would otherwise stay checked through a form's reset.
    private protected override Task OnResetAsync()
    {
        (_treeKeys, _treeValue) = ([], null);
        return base.OnResetAsync();
    }

    // Checked in any order, the same keys are the same value.
    private protected override bool SameValue(IReadOnlyList<TKey>? value, IReadOnlyList<TKey>? other) =>
        SameItems(value, other, null);

    // The value lists the items held in memory in tree order, then loaded children, then keys whose items haven't
    // loaded. When IsSelectable leaves out a heading, its checkbox still checks the rows inside it, but its key isn't
    // part of the value.
    private protected override async Task CheckedAsync(IReadOnlyCollection<TKey> keys)
    {
        var items = ByKey();
        var chosen = keys.ToHashSet();
        var known = items.Where(pair => chosen.Contains(pair.Key) && IsSelectable?.Invoke(pair.Value) != false);
        IReadOnlyList<TKey> value = [.. known.Select(pair => pair.Key), .. keys.Where(key => !items.ContainsKey(key))];
        // Checking a heading whose items haven't loaded changes the tree's keys but not the value.
        if (SameValue(value, Value ?? []))
        {
            value = Value!;
        }

        (_treeKeys, _treeValue) = (keys, value);
        await SetValueAsync(value);
        StateHasChanged();
    }
}
