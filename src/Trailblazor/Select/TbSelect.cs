namespace Trailblazor;

/// <summary>A field for choosing one of its <see cref="TbOption{TValue}"/>s. Bind it with <c>@bind-Value</c>.</summary>
public class TbSelect<TValue> : TbSelectBase<TValue>
{
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A value type's default is a real value, such as an enum's first member, so clearing needs a nullable type.
        if (Clearable && typeof(TValue).IsValueType && Nullable.GetUnderlyingType(typeof(TValue)) is null)
        {
            throw new InvalidOperationException(
                $"A clearable select needs a nullable value type, such as {typeof(TValue).Name}?.");
        }
    }

    private protected override bool Multiple => false;

    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private protected override bool HasChoice => !EqualityComparer<TValue>.Default.Equals(Value, default);

    private protected override TValue? Cleared => default;

    private protected override bool IsChosen(object? item) => Equals(item, Value);

    private protected override TValue? Choose(object? item) => (TValue?)item;

    private protected override string? FormatItem(object? item) => FormatValue((TValue?)item);
}
