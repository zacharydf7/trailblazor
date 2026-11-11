using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A field for choosing one of its <see cref="TbOption{TValue}"/>s. Bind it with <c>@bind-Value</c>.</summary>
public class TbSelect<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : TbSelectBase<TValue>
{
    /// <summary>Decides whether two values are the same, such as entities matched by id, so a value loaded again
    /// still matches its option. Defaults to the values' own equality.</summary>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    private protected override bool SameValue(TValue? value, TValue? other) => OptionValues.Same(Comparer, value, other);

    /// <summary>The text shown for a value that none of the options has, such as one that has left the list.
    /// Defaults to the value's text.</summary>
    [Parameter]
    public Func<TValue, string>? UnmatchedText { get; set; }

    /// <summary>The values to offer, such as a long list or one loaded from a database, in place of
    /// <see cref="TbOption{TValue}"/> children. They render without a component each.</summary>
    [Parameter]
    public IEnumerable<TValue>? Items { get; set; }

    /// <summary>An item's text, which the list shows, type-ahead matches and screen readers read. Defaults to the
    /// value's text.</summary>
    [Parameter]
    public Func<TValue, string>? ItemText { get; set; }

    /// <summary>Shows an item in the list and, once chosen, in the select, in place of its text. The text still names
    /// the item for screen readers and type-ahead.</summary>
    [Parameter]
    public RenderFragment<TValue>? ItemTemplate { get; set; }

    /// <summary>Whether an item can't be chosen.</summary>
    [Parameter]
    public Func<TValue, bool>? ItemDisabled { get; set; }

    private protected override void UseItems() => UseItems(Items, ItemText, ItemTemplate, ItemDisabled);

    private protected override bool Multiple => false;

    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private protected override bool HasChoice => !EqualityComparer<TValue>.Default.Equals(Value, default);

    private protected override TValue? Cleared => default;

    // Empty text, such as a string field's "", shows the placeholder, as no choice does.
    private protected override string? UnmatchedValueText => HasChoice && SelectedOptions.Count == 0
        && (UnmatchedText?.Invoke(Value!) ?? FormatValue(Value)) is { Length: > 0 } text ? text : null;

    private protected override bool IsChosen(object? item) => OptionValues.Same(Comparer, item, Value);

    private protected override TValue? Choose(object? item) => (TValue?)item;

    private protected override string? FormatItem(object? item) => FormatValue((TValue?)item);
}
