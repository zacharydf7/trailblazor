namespace Trailblazor;

/// <summary>A group of toggle buttons where any number can be pressed, such as bold, italic and underline. Bind it
/// with <c>@bind-Value</c> to an <see cref="IReadOnlyList{T}"/>, which keeps the order the items were pressed in.
/// </summary>
public class TbMultiToggleGroup<TItem> : TbToggleGroupBase<IReadOnlyList<TItem>>
{
    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TItem)) ?? typeof(TItem);

    private protected override bool IsPressed(object? item) => Value?.Any(pressed => Equals(pressed, item)) ?? false;

    private protected override IReadOnlyList<TItem> Toggle(object? item) => IsPressed(item)
        ? Value!.Where(pressed => !Equals(pressed, item)).ToList()
        : [.. Value ?? [], (TItem)item!];
}
