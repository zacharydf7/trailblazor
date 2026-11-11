using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A list, always shown, for choosing one of its <see cref="TbOption{TValue}"/>s. Bind it with
/// <c>@bind-Value</c>.</summary>
public class TbListBox<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : TbListBoxBase<TValue>
{
    /// <summary>Decides whether two values are the same, such as entities matched by id, so a value loaded again
    /// still matches its option. Defaults to the values' own equality.</summary>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    private protected override bool SameValue(TValue? value, TValue? other) => OptionValues.Same(Comparer, value, other);

    private protected override bool Multiple => false;

    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private protected override bool IsChosen(object? item) => OptionValues.Same(Comparer, item, Value);

    private protected override TValue? Choose(object? item) => (TValue?)item;

    private protected override string? FormatItem(object? item) => FormatValue((TValue?)item);
}
