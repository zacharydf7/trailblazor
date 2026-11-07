using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A list, always shown, for choosing any number of its <see cref="TbOption{TValue}"/>s. Bind it with
/// <c>@bind-Value</c> to an <see cref="IReadOnlyList{T}"/>. Values chosen interactively keep the order they were
/// chosen in; a static form post lists them in the options' order.</summary>
public class TbMultiListBox<TItem> : TbListBoxBase<IReadOnlyList<TItem>>
{
    /// <summary>Decides whether two values are the same, such as entities matched by id, so a value loaded again
    /// still matches its option. Defaults to the values' own equality.</summary>
    [Parameter]
    public IEqualityComparer<TItem>? Comparer { get; set; }

    private protected override bool Multiple => true;

    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TItem)) ?? typeof(TItem);

    private protected override bool IsChosen(object? item) =>
        Value?.Any(chosen => OptionValues.Same(Comparer, chosen, item)) ?? false;

    // Choosing an option that's already chosen takes it out again.
    private protected override IReadOnlyList<TItem> Choose(object? item) => IsChosen(item)
        ? Value!.Where(chosen => !OptionValues.Same(Comparer, chosen, item)).ToList()
        : [.. Value ?? [], (TItem)item!];

    private protected override IReadOnlyList<TItem> ChooseMany(IReadOnlyList<object?> items) =>
        [.. Value ?? [], .. items.Cast<TItem>()];

    private protected override string? FormatItem(object? item) =>
        BindConverter.FormatValue((TItem?)item, CultureFor(typeof(TItem)))?.ToString();
}
