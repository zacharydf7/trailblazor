using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Sets how a <see cref="TbChart{TItem}"/> draws the axis its values are read against. Write it inside the
/// chart; without one, the axis runs between round values that hold every series, from zero when the chart has bars.
/// </summary>
public sealed class TbYAxis : ComponentBase, IDisposable
{
    [CascadingParameter]
    private ChartAxes? Axes { get; set; }

    /// <summary>What the axis measures, shown above it, such as "Jobs" or "Rainfall (mm)".</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>How values are written on the axis and in the data table, such as <c>"N0"</c> or <c>"C2"</c>, in the
    /// current culture. Without one, the axis shows as many decimals as its step has. With
    /// <see cref="ChartStacking.Percent"/> the axis's values are shares from 0 to 1, so give a percent format such as
    /// <c>"P1"</c>.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>The value at the low end of the axis, which must be below <see cref="Max"/>. Without it, the axis
    /// starts at a round value at or below the lowest value, or at zero when the chart has bars. A bar chart that
    /// doesn't start at zero misleads, so set it only for lines.</summary>
    [Parameter]
    public double? Min { get; set; }

    /// <summary>The value at the high end of the axis, which must be above <see cref="Min"/>. Without it, the axis
    /// ends at a round value at or above the highest value. Marks beyond either end are drawn outside the plot.
    /// </summary>
    [Parameter]
    public double? Max { get; set; }

    /// <summary>The distance between the values the axis marks, which are the multiples of it between the axis's
    /// ends. Without it, the axis picks a round step that gives about five marks. Must be above zero; one that would
    /// give more than about 50 marks is passed over for the axis's own.</summary>
    [Parameter]
    public double? Step { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Axes is null)
        {
            throw new InvalidOperationException($"{nameof(TbYAxis)} must be inside a {nameof(TbChart<>)}.");
        }

        if (Step <= 0 || Min >= Max)
        {
            throw new InvalidOperationException(
                $"{nameof(TbYAxis)} needs a {nameof(Step)} above zero and a {nameof(Min)} below its {nameof(Max)}.");
        }

        Axes.Set(this);
    }

    /// <summary>Takes the axis's settings out of its chart.</summary>
    public void Dispose() => Axes?.Remove(this);
}
