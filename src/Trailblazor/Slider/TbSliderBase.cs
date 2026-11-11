using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>What <see cref="TbSlider{TValue}"/> and <see cref="TbRangeSlider{TValue}"/> share: a track between
/// <see cref="Min"/> and <see cref="Max"/> with native range inputs as its thumbs.</summary>
public abstract partial class TbSliderBase<TValue> : TbFieldBase<TValue>
{
    /// <summary>
    /// The size of the thumb, the track and the text. Defaults to <see cref="Trailblazor.Size.Medium"/>.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    /// <summary>The lowest value. Defaults to 0.</summary>
    [Parameter]
    public decimal Min { get; set; }

    /// <summary>The highest value. Defaults to 100.</summary>
    [Parameter]
    public decimal Max { get; set; } = 100;

    /// <summary>How far one arrow key press moves a thumb. Defaults to 1.</summary>
    [Parameter]
    public decimal Step { get; set; } = 1;

    /// <summary>Shows each thumb's value above it.</summary>
    [Parameter]
    public bool ShowValue { get; set; }

    /// <summary>Tick marks under the track, with optional labels.</summary>
    [Parameter]
    public IReadOnlyList<SliderMark>? Marks { get; set; }

    /// <summary>Raised once the user lets go of a thumb, or finishes moving it with the keyboard, with the value it
    /// settled on; <c>ValueChanged</c> is raised as the thumb moves.</summary>
    [Parameter]
    public EventCallback<TValue> ValueCommitted { get; set; }

    /// <summary>Extra CSS classes for the field's inner parts.</summary>
    [Parameter]
    public ChoiceClasses? Classes { get; set; }

    // The number type each thumb holds.
    private protected abstract Type NumberType { get; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!NumberTypes.IsNumber(NumberType))
        {
            throw new InvalidOperationException($"A slider needs a number type, not {NumberType.Name}.");
        }

        var (lowest, highest) = NumberTypes.Limits(NumberType);
        if (Min >= Max || Step <= 0 || Min < lowest || Max > highest)
        {
            throw new InvalidOperationException(
                $"A slider needs a Min below its Max, both within {NumberType.Name}, and a Step above 0.");
        }

        // A whole number type can't hold a fractional step, which would round back and leave the thumb stuck.
        if (NumberTypes.IsWhole(NumberType) && (Min % 1 != 0 || Max % 1 != 0 || Step % 1 != 0))
        {
            throw new InvalidOperationException(
                $"A slider of {NumberType.Name} needs a whole Min, Max and Step.");
        }
    }

    // Where a number sits along the track, from 0 at the start to 1 at the end.
    private protected double Fraction(decimal number) => (double)((Math.Clamp(number, Min, Max) - Min) / (Max - Min));

    private protected decimal? ToNumber(object? value) =>
        value is null ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);

    private protected object ToValue(decimal number) =>
        Convert.ChangeType(Math.Clamp(number, Min, Max), NumberType, CultureInfo.InvariantCulture);

    // A range input always holds its value with a period, whatever the user's culture.
    private protected static string Invariant(decimal number) => number.ToString(CultureInfo.InvariantCulture);

    // Tests wait for data-ready before moving a thumb, since a prerendered slider is replaced and loses the move;
    // data-static tells them not to wait.
    private protected string? StaticMark => AssignedRenderMode is null && !RendererInfo.IsInteractive ? "" : null;

    private protected string? ReadyMark => RendererInfo.IsInteractive ? "" : null;

    private protected static decimal? Parse(string? text) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;

    // Bound to each thumb's change event, which the browser fires once the user lets go.
    private protected Task CommitAsync() => ReadOnly ? Task.CompletedTask : ValueCommitted.InvokeAsync(Value!);
}
