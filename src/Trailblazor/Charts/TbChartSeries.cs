using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A series of a <see cref="TbChart{TItem}"/>: one value for each of the chart's items, drawn as a
/// <see cref="TbLineSeries{TItem}"/> or a <see cref="TbBarSeries{TItem}"/>. Series draw in the order they first render,
/// and each takes the next of the chart's eight series colors.</summary>
public abstract class TbChartSeries<TItem> : ComponentBase, IDisposable
{
    private object? _shown;
    private Func<TItem, object?>? _value;

    [CascadingParameter]
    private TbChart<TItem>? Chart { get; set; }

    /// <summary>What the series is called in the legend, the data table and what screen readers hear, such as
    /// "Completed". Required.</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>An item's value in this series, such as <c>month => month.Completed</c>. Required. Any number type
    /// works; null, or a value that isn't finite, leaves a gap. A value that isn't a number throws. When what the
    /// function reads from outside the item changes, the chart draws again as its parent renders. A series inside a
    /// component of yours that renders by itself may be drawn a render behind, so have such a change render the
    /// chart's parent too.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, object?>? Value { get; set; }

    /// <summary>A CSS color for the series in place of the one its position gives it, such as
    /// <c>"var(--tb-color-danger)"</c>. Check that it has 3:1 contrast against the surface in both themes.</summary>
    [Parameter]
    public string? Color { get; set; }

    internal abstract bool Bars { get; }

    internal virtual ChartCurve Line => ChartCurve.Straight;

    internal virtual bool? HasMarkers => null;

    // The component's name as markup writes it, without the mark of its type parameter.
    private string Kind => GetType().Name.Split('`')[0];

    internal double? ValueOf(TItem item) => ChartValues.Number(Value!(item), Name);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Chart is null)
        {
            throw new InvalidOperationException($"{Kind} must be inside a {nameof(TbChart<>)}.");
        }

        Chart.AddSeries(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Name) || Value is null)
        {
            throw new InvalidOperationException($"{Kind} needs a {nameof(Name)} and a {nameof(Value)}.");
        }

        // The chart draws before its series take their parameters, so a series that changed has it draw again.
        object shown = (Name, Value.Method, Color, Line, HasMarkers);
        if (!shown.Equals(_shown))
        {
            _shown = shown;
            Chart!.SeriesChanged();
        }
        else if (!ReferenceEquals(Value, _value))
        {
            Chart!.FunctionChanged();
        }

        _value = Value;
    }

    /// <summary>Takes the series out of its chart.</summary>
    public void Dispose()
    {
        Chart?.RemoveSeries(this);
        GC.SuppressFinalize(this);
    }
}
