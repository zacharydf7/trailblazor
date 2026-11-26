using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Sets how a <see cref="TbChart{TItem}"/> labels the axis its items run along. Write it inside the chart;
/// without one, the axis shows each item's <see cref="TbChart{TItem}.X"/> value as it reads by default.</summary>
public sealed class TbXAxis : ComponentBase, IDisposable
{
    [CascadingParameter]
    private ChartAxes? Axes { get; set; }

    /// <summary>What the axis measures, shown under it, such as "Month".</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>How each item's <see cref="TbChart{TItem}.X"/> value is written, such as <c>"MMM d"</c> for a date
    /// or <c>"N0"</c> for a number, in the current culture. Without one, a date with no time of day reads as a short
    /// date, and an enum's value as its name in words. It writes the items in the tooltip, the data table and the
    /// name a screen reader hears for a point, and along an <see cref="ChartScale.Even"/> scale.
    /// <see cref="MarkFormat"/> writes a <see cref="ChartScale.Continuous"/> scale's marks, and number marks follow
    /// this one without it.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>How a <see cref="ChartScale.Continuous"/> scale's marks are written, such as <c>"yyyy"</c> or
    /// <c>"0 km"</c>. Without it, a number mark is written as <see cref="Format"/> writes the items, or else with as
    /// many decimals as its step needs, and a date mark reads as a time, a day, a month or a year, to suit its
    /// step.</summary>
    [Parameter]
    public string? MarkFormat { get; set; }

    /// <summary>How the items are placed along the chart: one evenly spaced place each, or each by its
    /// <see cref="TbChart{TItem}.X"/> value. <see cref="ChartScale.Even"/> by default.</summary>
    [Parameter]
    public ChartScale Scale { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Axes is null)
        {
            throw new InvalidOperationException($"{nameof(TbXAxis)} must be inside a {nameof(TbChart<>)}.");
        }

        Axes.Set(this);
    }

    /// <summary>Takes the axis's settings out of its chart.</summary>
    public void Dispose() => Axes?.Remove(this);
}
