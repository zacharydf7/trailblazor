using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A group of toggle buttons where at most one is pressed, such as text alignment. Bind it with
/// <c>@bind-Value</c>.</summary>
public class TbToggleGroup<TValue> : TbToggleGroupBase<TValue>
{
    /// <summary>Lets the user press the pressed item again to leave none pressed. Off by default, so one stays
    /// pressed once chosen.</summary>
    [Parameter]
    public bool Clearable { get; set; }

    /// <summary>Decides whether two values are the same, such as entities matched by id, so a value loaded again
    /// still matches its option. Defaults to the values' own equality.</summary>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // A value type's default is a real value, such as an enum's first member, so clearing needs a nullable type.
        if (Clearable && typeof(TValue).IsValueType && Nullable.GetUnderlyingType(typeof(TValue)) is null)
        {
            throw new InvalidOperationException(
                $"A clearable toggle group needs a nullable value type, such as {typeof(TValue).Name}?.");
        }
    }

    private protected override Type ItemType => Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

    private protected override bool IsPressed(object? item) => OptionValues.Same(Comparer, item, Value);

    // Pressing the pressed item clears the group only when it is clearable.
    private protected override TValue? Toggle(object? item) =>
        IsPressed(item) ? (Clearable ? default : Value) : (TValue?)item;
}
