using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A field for choosing any number of its <see cref="TbOption{TValue}"/>s. Bind it with <c>@bind-Value</c>
/// to an <see cref="IReadOnlyList{T}"/>. Values chosen interactively keep the order they were chosen in; a static form
/// post lists them in the options' order.</summary>
public class TbMultiSelect<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TItem>
    : TbSelectBase<IReadOnlyList<TItem>>
{
    /// <summary>Decides whether two values are the same, such as entities matched by id, so a value loaded again
    /// still matches its option. Defaults to the values' own equality.</summary>
    [Parameter]
    public IEqualityComparer<TItem>? Comparer { get; set; }

    /// <summary>Shows the chosen options as chips, each with a button that removes it; Backspace removes the last.
    /// </summary>
    [Parameter]
    public bool Chips { get; set; }

    /// <summary>Starts the list with an option that chooses every enabled option, or takes them all out once they are
    /// all chosen. It is hidden while the filter has text, and a statically rendered select has none.</summary>
    [Parameter]
    public bool SelectAll { get; set; }

    private protected override bool ShowChips => Chips;

    private protected override bool ShowSelectAll => SelectAll;

    private protected override bool Multiple => true;

    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TItem)) ?? typeof(TItem);

    private protected override bool HasChoice => Value is { Count: > 0 };

    private protected override IReadOnlyList<TItem> Cleared => [];

    private protected override bool IsChosen(object? item) =>
        Value?.Any(chosen => OptionValues.Same(Comparer, chosen, item)) ?? false;

    // Choosing an option that's already chosen takes it out again.
    private protected override IReadOnlyList<TItem> Choose(object? item) => IsChosen(item)
        ? Value!.Where(chosen => !OptionValues.Same(Comparer, chosen, item)).ToList()
        : [.. Value ?? [], (TItem)item!];

    private protected override IReadOnlyList<TItem> ChooseAll(IEnumerable<object?> items, bool choose) => choose
        ? [.. Value ?? [], .. items.Where(item => !IsChosen(item)).Cast<TItem>()]
        : (Value ?? []).Where(chosen => !items.Any(item => OptionValues.Same(Comparer, chosen, item))).ToList();

    private protected override string? FormatItem(object? item) =>
        BindConverter.FormatValue((TItem?)item, CultureFor(typeof(TItem)))?.ToString();
}
