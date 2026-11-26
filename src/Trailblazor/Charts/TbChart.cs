using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A chart of one or more series over a list of items: lines for how something changes, bars for comparing
/// amounts. It is drawn as markup that needs no script to show, with its values in a data table under it for anyone
/// who can't read the drawing.</summary>
[CascadingTypeParameter(nameof(TItem))]
public partial class TbChart<TItem>
{
    // A line with more points than this has no markers unless it asks for them: they would run together.
    private const int MostMarkers = 30;

    // The series colors the theme has; a ninth series starts over at the first.
    private const int Slots = 8;

    // The lines' drawing has this box however large the plot is, and is stretched to fit it.
    private static readonly string LinesBox = $"0 0 {ChartPath.Grid} {ChartPath.Grid}";

    private readonly List<TbChartSeries<TItem>> _series = [];
    private bool _changed;
    private bool _redrawn;

    /// <summary>A label this chart's use is counted under, such as <c>"jobs-by-month"</c>, so the app can count how
    /// often people see it through the <c>Trailblazor</c> meter. Controls that share a label count together; a control
    /// without one isn't counted. It names what the control shows, never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    /// <summary>The items the chart draws, one place along the chart for each, in the order given. Required.</summary>
    [Parameter, EditorRequired]
    public IEnumerable<TItem>? Items { get; set; }

    /// <summary>What each item is called along the chart, such as <c>month => month.Name</c> or
    /// <c>day => day.Date</c>. Required. <see cref="TbXAxis.Format"/> sets how a date or a number is written.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, object?>? X { get; set; }

    /// <summary>The chart's name, shown above it, such as "Jobs by month". Give this or <see cref="AriaLabel"/>.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Names the chart for screen readers when it has no <see cref="Title"/>, such as when a heading beside
    /// it already says what it shows.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>How several <see cref="TbBarSeries{TItem}"/> share a place: side by side, stacked, or stacked as
    /// shares of each place's total. <see cref="ChartStacking.None"/> by default. Line series are never stacked.
    /// </summary>
    [Parameter]
    public ChartStacking Stacking { get; set; }

    /// <summary>The height of the area the series are drawn in, as a CSS length such as <c>"12rem"</c>. Without it,
    /// the <c>--tb-chart-height</c> token sets it, 20rem by default. The chart is as wide as its container.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>The words the chart shows and says to screen readers.</summary>
    [Parameter]
    public ChartLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the chart's parts.</summary>
    [Parameter]
    public ChartClasses? Classes { get; set; }

    /// <summary>The chart's series, and its axes when they need settings: <see cref="TbLineSeries{TItem}"/>,
    /// <see cref="TbBarSeries{TItem}"/>, <see cref="TbXAxis"/> and <see cref="TbYAxis"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private ChartAxes Axes => field ??= new(SeriesChanged);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (X is null)
        {
            throw new InvalidOperationException($"{nameof(TbChart<>)} needs an {nameof(X)}.");
        }

        // A static page never reaches OnAfterRender, so a render the chart's parent starts opens a new round too.
        _redrawn = false;
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        _redrawn = false;
        Telemetry.Rendered(TelemetryName);
    }

    internal void AddSeries(TbChartSeries<TItem> series)
    {
        _series.Add(series);
        SeriesChanged();
    }

    internal void RemoveSeries(TbChartSeries<TItem> series)
    {
        if (_series.Remove(series))
        {
            SeriesChanged();
        }
    }

    // Series and axes take their parameters after the chart has drawn, so each one that changed has the chart draw
    // again: once for all of them, however many changed.
    internal void SeriesChanged()
    {
        _redrawn = true;
        if (!_changed)
        {
            _changed = true;
            StateHasChanged();
        }
    }

    // A function that reads a local is a new object on every render, with nothing to say whether what it reads
    // changed, so a series that gets one has the chart draw again: once in a round of rendering. The render that
    // follows hands every series a new function again, and counting those would have the two go around forever.
    internal void FunctionChanged()
    {
        if (!_redrawn)
        {
            SeriesChanged();
        }
    }

    // Everything the drawing shows, read from the series and axes as they stand when it is drawn.
    private ChartPlan Plan()
    {
        var culture = CultureInfo.CurrentCulture;
        var items = Items as IReadOnlyList<TItem> ?? [.. Items ?? []];
        List<string> places = [.. items.Select(item => ChartValues.Text(X!(item), Axes.X?.Format, culture))];
        List<ChartSeriesValues> series = [.. _series.Select(one =>
            new ChartSeriesValues(one.Name, one.Bars, one.Line, [.. items.Select(one.ValueOf)]))];
        var axis = Axes.Y is { } y ? new ChartValueAxis(y.Min, y.Max, y.Step, y.Format) : default;
        return ChartPlan.Build(places, series, Stacking, axis, culture);
    }

    private static string Slot(int index) => $"tb-chart__series--{index % Slots + 1}";

    private string? Color(int index) => _series[index].Color is { } color ? $"--_tb-chart-series: {color}" : null;

    private static string BarClass(ChartBarEnd end) => end switch
    {
        ChartBarEnd.Top => "tb-chart__bar tb-chart__bar--top",
        ChartBarEnd.Bottom => "tb-chart__bar tb-chart__bar--bottom",
        _ => "tb-chart__bar",
    };

    private static int Longest(ChartPlan plan) => plan.Places.Max(place => place.Length);

    private static string Percent(double share) =>
        Math.Round(share * 100, 3).ToString(CultureInfo.InvariantCulture) + "%";

    // What a screen reader hears for the drawing: the kind of chart and its series, as "Bar chart: Completed, Failed".
    private string Summary()
    {
        var kind = _series.All(one => one.Bars) ? Labels.Bars ?? Localizer[TbStrings.ChartBars]
            : _series.Any(one => one.Bars) ? Labels.BarsAndLines ?? Localizer[TbStrings.ChartBarsAndLines]
            : Labels.Lines ?? Localizer[TbStrings.ChartLines];
        return string.Format(CultureInfo.CurrentCulture, Labels.Summary ?? Localizer[TbStrings.ChartSummary], kind,
            string.Join(", ", _series.Select(one => one.Name)));
    }
}
