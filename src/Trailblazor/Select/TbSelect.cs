namespace Trailblazor;

/// <summary>A field for choosing one of its <see cref="TbOption{TValue}"/>s. Bind it with <c>@bind-Value</c>.</summary>
public class TbSelect<TValue> : TbSelectBase<TValue>
{
    private protected override bool Multiple => false;

    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private protected override bool HasChoice => !EqualityComparer<TValue>.Default.Equals(Value, default);

    private protected override TValue? Cleared => default;

    private protected override bool IsChosen(object? item) => Equals(item, Value);

    private protected override TValue? Choose(object? item) => (TValue?)item;

    private protected override string? FormatItem(object? item) => FormatValue((TValue?)item);
}
