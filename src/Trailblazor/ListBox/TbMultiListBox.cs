using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A list, always shown, for choosing any number of its <see cref="TbOption{TValue}"/>s. Bind it with
/// <c>@bind-Value</c> to an <see cref="IReadOnlyList{T}"/>. Values chosen interactively keep the order they were
/// chosen in; a static form post lists them in the options' order.</summary>
public class TbMultiListBox<TItem> : TbListBoxBase<IReadOnlyList<TItem>>
{
    private protected override bool Multiple => true;

    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TItem)) ?? typeof(TItem);

    private protected override bool IsChosen(object? item) => Value?.Any(chosen => Equals(chosen, item)) ?? false;

    // Choosing an option that's already chosen takes it out again.
    private protected override IReadOnlyList<TItem> Choose(object? item) => IsChosen(item)
        ? Value!.Where(chosen => !Equals(chosen, item)).ToList()
        : [.. Value ?? [], (TItem)item!];

    private protected override string? FormatItem(object? item) =>
        BindConverter.FormatValue((TItem?)item, CultureInfo.CurrentCulture)?.ToString();
}
