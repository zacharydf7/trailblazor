using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>One range of a gauge's values, such as "Normal" up to 60: a band along the gauge in its tone, and the
/// name the gauge shows and reads out while its value is in the range. It goes inside a gauge, and starts where the
/// range below it ends, whatever order the ranges are written in.</summary>
public sealed class TbGaugeRange : ComponentBase, IDisposable
{
    private object? _shown;

    [CascadingParameter]
    private GaugeRanges? Ranges { get; set; }

    /// <summary>The value the range ends at. Without it the range runs to the gauge's <c>Max</c>, as the last range
    /// usually does.</summary>
    [Parameter]
    public double? To { get; set; }

    /// <summary>What the range is called, such as "High". The gauge shows it with its value and reads it out, so
    /// the range is never told by its color alone.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The tone of the range's band, which the gauge's value takes too while it is in the range, such as
    /// <see cref="Trailblazor.Tone.Danger"/> for a critical range. Without one the band is neutral and the value
    /// keeps the gauge's own tone.</summary>
    [Parameter]
    public Tone? Tone { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Ranges is null)
        {
            throw new InvalidOperationException($"{nameof(TbGaugeRange)} must be inside a gauge.");
        }

        Ranges.Add(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // The gauge draws before its ranges take their parameters, so a range that changed has it draw again.
        object shown = (To, Name, Tone);
        if (_shown is not null && !shown.Equals(_shown))
        {
            Ranges!.Changed();
        }

        _shown = shown;
    }

    /// <summary>Takes the range out of its gauge.</summary>
    public void Dispose() => Ranges?.Remove(this);
}
