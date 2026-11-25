using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A form field that opens a tree of checkboxes under it for choosing several items from a hierarchy, such as
/// regions or folders, with an optional filter. The tree stays open while the user checks items.</summary>
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
                shown.Add(string.Format(CultureInfo.CurrentCulture, Labels.More ?? Localizer[TbStrings.TreeSelectMore],
                    value.Count - shown.Count));
            }

            return string.Join(", ", shown);
        }
    }

    // Like a reset, a clear forgets headings checked without changing the value.
    private protected override Task ClearValueAsync()
    {
        (_treeKeys, _treeValue) = ([], null);
        return SetValueAsync([]);
    }

    private protected override bool ChecksRows => true;

    private protected override IReadOnlyCollection<TKey> SelectedKeys => [];

    // The tree's checked keys, including headings ItemSelectable keeps out of the value, and the value they produced.
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

    private protected override bool PastesSeveral => true;

    // Smart paste starts from the tree's checked keys and leaves them for the tree, as a check does, so a heading
    // checked before its items loaded stays checked. With cascading checks, it checks an item as the tree does: the
    // item brings the enabled items inside it, and a parent is checked only while every item inside it is.
    private protected override IReadOnlyList<TKey> ChoosePasted(IReadOnlyList<object?> named,
        IReadOnlyList<object?> offered)
    {
        var items = ByKey();
        var keys = OptionValues.Pasted<TKey>(null, [.. CheckedKeys], named, offered).ToHashSet();
        if (CascadeChecks)
        {
            foreach (var key in named)
            {
                keys.UnionWith(Under(items[(TKey)key!]).Where(item => ItemDisabled?.Invoke(item) != true)
                    .Select(KeyOf));
            }

            foreach (var item in Items)
            {
                InStep(item);
            }

            IEnumerable<TItem> Under(TItem item) => Inside(item).SelectMany(inside => Under(inside).Prepend(inside));

            bool InStep(TItem item)
            {
                List<TItem> inside = [.. Inside(item)];
                if (inside.Count == 0)
                {
                    return keys.Contains(KeyOf(item));
                }

                // ToList visits every item inside, even past an unchecked one, so parents deeper down are in step too.
                var all = inside.Select(InStep).ToList().All(isChecked => isChecked);
                if (all)
                {
                    keys.Add(KeyOf(item));
                }
                else
                {
                    keys.Remove(KeyOf(item));
                }

                return all;
            }
        }

        var value = ValueOf(keys, items);
        (_treeKeys, _treeValue) = (keys, value);
        return value;
    }

    // The value for the tree's checked keys: the items held in memory in tree order, then loaded children, then keys
    // whose items haven't loaded. When ItemSelectable leaves out a heading, its key isn't part of the value.
    private IReadOnlyList<TKey> ValueOf(IReadOnlyCollection<TKey> keys, OrderedDictionary<TKey, TItem> items)
    {
        var chosen = keys.ToHashSet();
        var known = items.Where(pair => chosen.Contains(pair.Key) && ItemSelectable?.Invoke(pair.Value) != false);
        return [.. known.Select(pair => pair.Key), .. keys.Where(key => !items.ContainsKey(key))];
    }

    // Checked in any order, the same keys are the same value.
    private protected override bool SameValue(IReadOnlyList<TKey>? value, IReadOnlyList<TKey>? other) =>
        SameItems(value, other, null);

    // When ItemSelectable leaves out a heading, its checkbox still checks the rows inside it.
    private protected override async Task CheckedAsync(IReadOnlyCollection<TKey> keys)
    {
        var value = ValueOf(keys, ByKey());
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
