using System.Diagnostics.CodeAnalysis;

namespace Trailblazor;

/// <summary>A form field that opens a tree under it for choosing one item from a hierarchy, such as a folder or a
/// region, with an optional search. The key type's default, such as 0 for an <c>int</c>, means nothing is chosen.
/// </summary>
public class TbTreeSelect<TItem, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey>
    : TbTreeSelectBase<TItem, TKey, TKey> where TKey : notnull
{
    private protected override string? ValueText => IsMissing(Value) ? null : TextOf(Value!, ByKey());

    private protected override Task ClearValueAsync() => SetValueAsync(default);

    private protected override IReadOnlyCollection<TKey> SelectedKeys => IsMissing(Value) ? [] : [Value!];

    private protected override bool IsMissing(TKey? value) =>
        value is null || EqualityComparer<TKey>.Default.Equals(value, default);

    // Choosing an item closes the tree, as a select closes its list, even when it's the item already chosen.
    private protected override async Task ChosenAsync(TKey key)
    {
        await CloseAsync();
        await SetValueAsync(key);
        StateHasChanged();
    }
}
