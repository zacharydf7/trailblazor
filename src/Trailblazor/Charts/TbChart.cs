using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Trailblazor.Primitives;

namespace Trailblazor;

/// <summary>A chart of one or more series over a list of items: lines for how something changes, bars for comparing
/// amounts. It is drawn as markup that needs no script to show, with its values in a data table under it for anyone
/// who can't read the drawing. On an interactive page a tooltip follows the pointer, and the arrow keys move between
/// its points.</summary>
[CascadingTypeParameter(nameof(TItem))]
public partial class TbChart<TItem>
{
    // A line with more points than this has no markers unless it asks for them: they would run together.
    private const int MostMarkers = 30;

    // The series colors the theme has; a ninth series starts over at the first.
    private const int Slots = 8;

    // The lines' drawing has this box however large the plot is, and is stretched to fit it.
    private static readonly string LinesBox = $"0 0 {ChartPath.Grid} {ChartPath.Grid}";

    private readonly string _plotId = $"tb-chart-{Guid.NewGuid():N}-plot";
    private readonly List<TbChartSeries<TItem>> _series = [];
    private IReadOnlyList<TItem> _items = [];
    private ElementReference _plot;
    private IJSObjectReference? _handle;
    private DotNetObjectReference<ChartEvents>? _events;
    private (string Point, string NoValue, bool Activates) _sent;
    private int _commits;
    private bool _changed;
    private bool _redrawn;
    private bool _disposed;

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

    /// <summary>How several <see cref="TbBarSeries{TItem}"/>, or several <see cref="TbAreaSeries{TItem}"/>, share a
    /// place: each from zero, stacked, or stacked as shares of each place's total. <see cref="ChartStacking.None"/>
    /// by default. Bars pile with bars and areas with areas, and line and scatter series are never stacked.
    /// </summary>
    [Parameter]
    public ChartStacking Stacking { get; set; }

    /// <summary>Whether the chart is turned on its side, with its items down the side and its bars running across,
    /// which gives long names room and suits a ranking. Only bar series can be turned: a chart with any other
    /// series throws an <see cref="InvalidOperationException"/>. False by default.</summary>
    [Parameter]
    public bool Horizontal { get; set; }

    /// <summary>The height of the area the series are drawn in, as a CSS length such as <c>"12rem"</c>. Without it,
    /// the <c>--tb-chart-height</c> token sets it, 20rem by default. The chart is as wide as its container.</summary>
    [Parameter]
    public string? Height { get; set; }

    /// <summary>Whether the user can hide and show each series from the legend, whose entries are then buttons on
    /// an interactive page. True by default. A series' own <c>Hidden</c> hides it either way.</summary>
    [Parameter]
    public bool CanHideSeries { get; set; } = true;

    /// <summary>Raised when the user clicks or taps a point, or presses Enter or Space on one, with its item, its
    /// series and its value. With it, the plot shows a pointer cursor once the page is interactive.</summary>
    [Parameter]
    public EventCallback<ChartPoint<TItem>> OnPointClick { get; set; }

    /// <summary>The words the chart shows and says to screen readers.</summary>
    [Parameter]
    public ChartLabels Labels { get; set; } = new();

    /// <summary>Extra CSS classes for the chart's parts.</summary>
    [Parameter]
    public ChartClasses? Classes { get; set; }

    /// <summary>The chart's series, its axes when they need settings, and its reference lines and bands:
    /// <see cref="TbLineSeries{TItem}"/>, <see cref="TbAreaSeries{TItem}"/>, <see cref="TbScatterSeries{TItem}"/>,
    /// <see cref="TbBarSeries{TItem}"/>, <see cref="TbXAxis"/>, <see cref="TbYAxis"/>,
    /// <see cref="TbReferenceLine"/> and <see cref="TbReferenceBand"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private ChartAxes Axes => field ??= new(SeriesChanged);

    private ChartReferences References => field ??= new(SeriesChanged);

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
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _redrawn = false;
        Telemetry.Rendered(TelemetryName);
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        try
        {
            if (firstRender)
            {
                await AttachAsync();
            }
            else if (_handle is not null && ScriptSettings() is var settings && settings != _sent)
            {
                _sent = settings;
                await _handle.InvokeVoidAsync("update", settings.Point, settings.NoValue, settings.Activates);
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the chart went with it.
        }
    }

    private async Task AttachAsync()
    {
        var module = await Modules.ImportAsync("./_content/Trailblazor/chart.js");
        if (_disposed)
        {
            return;
        }

        _sent = ScriptSettings();
        _events = DotNetObjectReference.Create(new ChartEvents(DispatchExceptionAsync, OnActivateAsync, _plotId,
            (action, at) => ZoomAsync(action, at, at is null ? "keyboard" : "pointer"),
            share => MoveAsync(share, "pointer")));
        var handle = await module.InvokeAsync<IJSObjectReference?>("attachChart", _plot, _events, _sent.Point,
            _sent.NoValue, _sent.Activates);
        if (_disposed && handle is not null)
        {
            await handle.DisposeHandleAsync();
            return;
        }

        _handle = handle;
    }

    // What the script needs beyond the markup: how a point is named, and whether activating one is heard.
    private (string Point, string NoValue, bool Activates) ScriptSettings() =>
        (Labels.Point ?? Localizer[TbStrings.ChartPoint], Labels.NoValue ?? Localizer[TbStrings.ChartNoValue],
            OnPointClick.HasDelegate);

    // The script names the series by its number among the chart's series and the place along the chart. A call that
    // arrives after the data changed may name a point that is gone, and is passed over.
    private async Task OnActivateAsync(int series, int place, string input)
    {
        if (series < 0 || series >= _series.Count || place < 0 || place >= _items.Count || _series[series].IsHidden
            || _series[series].ValueOf(_items[place]) is not { } value)
        {
            return;
        }

        Telemetry.Used(TelemetryName, "activate", input == "keyboard" ? "keyboard" : "pointer");
        try
        {
            await OnPointClick.InvokeAsync(new(_items[place], _series[series].Name, value));
        }
        finally
        {
            // Counted once the app's handler has run, which the testing helpers wait for.
            _commits++;
            StateHasChanged();
        }
    }

    // The legend's button for a series: the app hears the change when it has bound the series' Hidden.
    private async Task ToggleAsync(TbChartSeries<TItem> series)
    {
        var hidden = !series.IsHidden;
        Telemetry.Used(TelemetryName, hidden ? "hide" : "show");
        try
        {
            await series.SetHiddenAsync(hidden);
        }
        finally
        {
            _commits++;
            StateHasChanged();
        }
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
        if (Horizontal && _series.FirstOrDefault(one => !one.Bars) is { } other)
        {
            throw new InvalidOperationException($"A {nameof(TbChart<>)} that is {nameof(Horizontal)} draws bar "
                + $"series only, and \"{other.Name}\" isn't one.");
        }

        var all = Items as IReadOnlyList<TItem> ?? [.. Items ?? []];
        // A zoomable chart shows the items in its view, and the scale's ends are the view's.
        var zoomable = Axes.X is { Zoomable: true } axisX ? axisX : null;
        _extent = zoomable is null ? null : Extent(all, zoomable);
        var view = _extent is null ? null : zoomable?.CurrentView;
        if (view is not null && !(ChartAlong.Alike(view.From, _extent!.Value.Sample)
            && ChartAlong.Alike(view.To, _extent.Value.Sample)))
        {
            throw new InvalidOperationException($"{nameof(TbXAxis)} needs a {nameof(TbXAxis.View)} of the kind its "
                + "items' X values are: numbers for numbers, dates for dates.");
        }

        var items = _items = view is null ? all : [.. all.Where(item => InView(X!(item), view))];
        List<string> places = [.. items.Select(item => ChartValues.Text(X!(item), Axes.X?.Format, culture))];
        List<ChartSeriesValues> series = [.. _series.Select(one =>
            new ChartSeriesValues(one.Name, one.Bars, one.Line, [.. items.Select(one.ValueOf)], one.IsHidden,
                one.Filled))];
        var axis = Axes.Y is { } y ? new ChartValueAxis(y.Min, y.Max, y.Step, y.Format) : default;
        var bars = series.Any(one => one.Bars && !one.Hidden);
        // A scatter's marks sit clear of the plot's edges where they can: its axis runs between round values, as the
        // value axis does. Bars already have room at each end.
        var round = !bars && _series.Any(one => one.Dots && !one.IsHidden);
        var along = Axes.X is { Scale: ChartScale.Continuous } x ? ChartAlong.Continuous([.. items.Select(X!)], bars,
            x.Format, x.MarkFormat, culture, new(view?.From ?? x.Min, view?.To ?? x.Max, x.Step, round)) : null;
        List<double> alsoReach = [.. References.All.Select(one => one.Values).OfType<(double Low, double High)>()
            .SelectMany(values => new[] { values.Low, values.High })];
        return ChartPlan.Build(places, series, Stacking, axis, culture, along, alsoReach);
    }

    // The whole of a zoomable scale: its first and last positions, widened to the axis's ends where the app set
    // them, the room between the closest two items, and an item's X value, whose type a view's ends take. Null with
    // fewer than two places.
    private readonly record struct ChartExtent(double First, double Last, double Closest, object Sample);

    private ChartExtent? _extent;

    private ChartExtent? Extent(IReadOnlyList<TItem> items, TbXAxis axis)
    {
        double[] positions = [.. items.Select(item => ChartAlong.Position(X!(item))).Distinct().Order()];
        if (positions.Length < 2)
        {
            return null;
        }

        var closest = positions.Zip(positions.Skip(1), (one, next) => next - one).Min();
        var first = axis.Min is { } min ? Math.Min(ChartAlong.Position(min), positions[0]) : positions[0];
        var last = axis.Max is { } max ? Math.Max(ChartAlong.Position(max), positions[^1]) : positions[^1];
        return new(first, last, closest, X!(items[0])!);
    }

    private static bool InView(object? x, ChartView view) => ChartAlong.Position(x) >= ChartAlong.Position(view.From)
        && ChartAlong.Position(x) <= ChartAlong.Position(view.To);

    // The part of the scale shown, as positions along it: the view, or the whole scale.
    private (double From, double To)? Shown => _extent is not { } extent ? null
        : Axes.X?.CurrentView is { } view ? (ChartAlong.Position(view.From), ChartAlong.Position(view.To))
        : (extent.First, extent.Last);

    // A zoom shows at least twice the room between the closest two items, and four days or four whole numbers on a
    // scale of them, so a quarter of it moves by at least one.
    private static double Narrowest(ChartExtent extent) => Math.Min(extent.Last - extent.First,
        Math.Max(extent.Closest * 2, ChartAlong.Unit(extent.Sample) * 4));

    private bool CanZoomIn => Shown is { } shown && _extent is { } extent
        && shown.To - shown.From > Narrowest(extent) * (1 + ChartTicks.Slack);

    private bool CanZoomOut => _extent is not null && Axes.X?.CurrentView is not null;

    private bool CanMoveBack => Shown is { } shown && shown.From > _extent!.Value.First;

    private bool CanMoveForward => Shown is { } shown && shown.To < _extent!.Value.Last;

    // What the zoom buttons say is shown, written as the items are.
    private string ViewText()
    {
        var (from, to) = Shown!.Value;
        var sample = _extent!.Value.Sample;
        string Text(double position) =>
            ChartValues.Text(ChartAlong.ValueAt(position, sample), Axes.X?.Format, CultureInfo.CurrentCulture);
        return string.Format(CultureInfo.CurrentCulture, Labels.View ?? Localizer[TbStrings.ChartView], Text(from),
            Text(to));
    }

    // Zooms in or out around a share of the plot, its middle unless the pointer gives one, moves back or forward by
    // a quarter of what is shown, or shows all of the scale. A view never reaches past the scale's ends.
    private async Task ZoomAsync(string action, double? at, string input)
    {
        if (Shown is not { } shown || _extent is not { } extent)
        {
            return;
        }

        if (action is "back" or "forward")
        {
            await MoveAsync(action == "back" ? -0.25 : 0.25, input);
            return;
        }

        var span = shown.To - shown.From;
        (double From, double To)? next = action switch
        {
            "in" => Around(shown, Math.Max(span / 2, Narrowest(extent)), at),
            "out" => Around(shown, Math.Min(span * 2, extent.Last - extent.First), at),
            "all" => null,
            _ => shown,
        };
        await ShowAsync(next, input);
    }

    // Moves what is shown by a share of it, as a drag does: by whole days or whole numbers on a scale of them, so
    // the view keeps its width.
    private async Task MoveAsync(double share, string input)
    {
        if (Shown is { } shown)
        {
            var (by, unit) = ((shown.To - shown.From) * share, ChartAlong.Unit(_extent!.Value.Sample));
            by = unit > 0 ? Math.Round(by / unit, MidpointRounding.AwayFromZero) * unit : by;
            await ShowAsync(Within((shown.From + by, shown.To + by)), input);
        }
    }

    // How a zoom button was pressed: a click has a count, and Enter or Space has none.
    private static string Input(MouseEventArgs args) => args.Detail > 0 ? "pointer" : "keyboard";

    // A view `span` wide around the place a share `at` of the way along what is shown, which stays where it was on
    // the plot.
    private (double From, double To) Around((double From, double To) shown, double span, double? at)
    {
        var anchor = shown.From + Math.Clamp(at ?? 0.5, 0, 1) * (shown.To - shown.From);
        var from = anchor - (anchor - shown.From) * span / (shown.To - shown.From);
        return Within((from, from + span));
    }

    // A view moved back inside the scale's ends where it reaches past one.
    private (double From, double To) Within((double From, double To) view)
    {
        var (first, last) = (_extent!.Value.First, _extent.Value.Last);
        var span = Math.Min(view.To - view.From, last - first);
        return view.From < first ? (first, first + span) : view.To > last ? (last - span, last) : view;
    }

    private async Task ShowAsync((double From, double To)? next, string input)
    {
        var extent = _extent!.Value;
        // A view of the whole scale is no view.
        ChartView? view = next is not { } shown || (shown.From <= extent.First && shown.To >= extent.Last) ? null
            : new(ChartAlong.ValueAt(shown.From, extent.Sample), ChartAlong.ValueAt(shown.To, extent.Sample));
        if (Equals(view, Axes.X!.CurrentView))
        {
            return;
        }

        Telemetry.Used(TelemetryName, "zoom", input);
        await Axes.X.SetViewAsync(view);
        _commits++;
        StateHasChanged();
    }

    // Where a value's label sits against its spot: over a bar's end, under the end of a bar that hangs down, in the
    // middle of a bar in a stack, or over a point, clear of its marker.
    private enum ValueSide
    {
        Above,
        Below,
        Middle,
        Point,
    }

    // A value written on its mark, placed as shares of an upright plot from its left and top: X across the mark's
    // middle, Y at its value end, or at its middle for a bar in a stack. Wide is the share of the plot's width the
    // mark leaves for the label, and High its share of the plot's height, 1 where nothing limits it. Chars is the
    // length of the series' longest label, so a series shows all its labels past their marks or none; in a stack
    // it is the label's own length.
    private readonly record struct ValueLabel(double X, double Y, ValueSide Side, double Wide, double High, int Chars,
        string Text);

    private List<ValueLabel> ValueLabelsOnMarks(ChartPlan plan)
    {
        var stacked = Stacking != ChartStacking.None;
        // A point's label has the room between the closest two places. One on the plot's start or end isn't
        // centered on its point, so it reaches as far as one and a half centered labels toward its neighbor: with
        // a point on an edge, a series' labels have two thirds of the gaps next to the edges at most.
        var at = plan.Along.At;
        var room = plan.Along.Slot;
        if (at.Count > 1 && (at[0] <= 0 || at[^1] >= 1))
        {
            room = Math.Min(room, Math.Min(at[1] - at[0], at[^1] - at[^2]) * 2 / 3);
        }

        List<ValueLabel> labels = [];
        for (var index = 0; index < _series.Count; index++)
        {
            var drawn = plan.Series[index];
            if (!_series[index].ShowsValues || drawn.Hidden)
            {
                continue;
            }

            var chars = drawn.Text.Max(text => text?.Length ?? 0);
            for (var place = 0; place < drawn.Text.Count; place++)
            {
                if (drawn.Text[place] is not { } text)
                {
                    continue;
                }

                if (drawn.Bars && drawn.Marks[place] is { } bar)
                {
                    var middle = bar.X + bar.Width / 2;
                    labels.Add(stacked
                        ? new(middle, bar.Y + bar.Height / 2, ValueSide.Middle, bar.Width, bar.Height, text.Length,
                            text)
                        : new(middle, bar.Up ? bar.Y : bar.Y + bar.Height, bar.Up ? ValueSide.Above : ValueSide.Below,
                            bar.Width, 1, chars, text));
                }
                else if (!drawn.Bars && drawn.Points[place] is { } point)
                {
                    labels.Add(new(point.X, point.Y, ValueSide.Point, room, 1, chars, text));
                }
            }
        }

        return labels;
    }

    // Where a value's label sits in the plot. A horizontal chart turns the spot, and what limits the label turns
    // with it: past a bar's end it has to fit the bar's thickness, and in a stacked bar its length too.
    private string ValueStyle(ValueLabel label)
    {
        var (x, y) = Turn(label.X, label.Y);
        var (wide, high) = !Horizontal ? (label.Wide, label.High)
            : (label.Side == ValueSide.Middle ? label.High : 1, label.Wide);
        return string.Create(CultureInfo.InvariantCulture, $"inset-inline-start: {ChartPath.Percent(x)}; "
            + $"inset-block-start: {ChartPath.Percent(y)}; --_tb-chart-wide: {wide:0.#####}; "
            + $"--_tb-chart-high: {high:0.#####}; --_tb-chart-chars: {label.Chars}");
    }

    // How the stylesheet names a label's side: in a horizontal chart, past a bar's end is along the plot.
    private string SideName(ValueLabel label) => label.Side switch
    {
        ValueSide.Above => Horizontal ? "end" : "above",
        ValueSide.Below => Horizontal ? "start" : "below",
        ValueSide.Middle => "middle",
        _ => "point",
    };

    // A point on the plot's start or end, as a continuous scale's first and last are, has its label start or end
    // there, as the axis's labels do, instead of centering on it.
    private static string? ValueEdge(ValueLabel label) => label.Side != ValueSide.Point ? null
        : label.X <= 0 ? "start" : label.X >= 1 ? "end" : null;

    // The room the chart keeps past its plot for the labels of the marks that reach its ends: past the highest
    // values, and past the lowest when a bar hangs down. In a horizontal chart that room is along the plot, as wide
    // as the longest label.
    // A label in a stacked bar's middle stays inside the plot and needs none of it.
    private static string? ValueRoom(List<ValueLabel> labels) => ClassNames.Join(
        labels.Any(label => label.Side != ValueSide.Middle) ? "tb-chart__body--values" : null,
        labels.Any(label => label.Side == ValueSide.Below) ? "tb-chart__body--values-low" : null);

    // A reference line or band as the chart shows it: the shares of its axis it runs between, which are the same for
    // a line, and what the plot and the list under the data table say for it. On the value axis the shares count
    // from the axis's low end, and along the items from the plot's start. A reference that lies outside the plot, or
    // at a place the chart doesn't have, is listed and not drawn.
    private readonly record struct PlacedReference(TbChartReference Reference, bool Band, bool Along, double Low,
        double High, string Text)
    {
        public bool Drawn => Band ? High > Low : Low is >= 0 and <= 1;
    }

    // A reference with neither values nor places hasn't checked its parameters yet, since the chart can draw
    // first; it throws once it does.
    private List<PlacedReference> PlaceReferences(ChartPlan plan) => [.. References.All
        .Where(one => one.Values is not null || one.Places is not null).Select(one =>
    {
        var band = one is TbReferenceBand;
        var (from, to, low, high) = one.Values is { } values
            ? (plan.Ticks.Share(values.Low), plan.Ticks.Share(values.High), plan.ReferenceText(values.Low),
                plan.ReferenceText(values.High))
            : AlongItems(plan, one, band);
        // A line's wording has no place for the second value, which is the same as its first.
        var text = string.Format(CultureInfo.CurrentCulture, band
            ? Labels.ReferenceBand ?? Localizer[TbStrings.ChartReferenceBand]
            : Labels.ReferenceLine ?? Localizer[TbStrings.ChartReferenceLine], one.Name, low, high);
        // A band is cut off at the plot's edges.
        return band ? new PlacedReference(one, true, one.Values is null, Math.Max(0, from), Math.Min(1, to), text)
            : new(one, false, one.Values is null, from, to, text);
    })];

    // Where two places along the chart lie, as shares of the plot from its start, with their text as the places are
    // written. On a continuous scale a place is where its value falls. On an even one it is the middle of the item
    // whose X it equals, and a band covers the whole of its two items' places. A place the chart can't find has no
    // share, and a band whose places are out of order ends before it starts; neither is drawn.
    private (double From, double To, string Low, string High) AlongItems(ChartPlan plan, TbChartReference reference,
        bool band)
    {
        var places = reference.Places!.Value;
        var culture = CultureInfo.CurrentCulture;
        var (low, high) = (ChartValues.Text(places.From, Axes.X?.Format, culture),
            ChartValues.Text(places.To, Axes.X?.Format, culture));
        if (plan.Along.ByValue)
        {
            foreach (var place in new[] { places.From, places.To }.Distinct())
            {
                if (!plan.Along.Fits(place))
                {
                    throw new InvalidOperationException($"{reference.GetType().Name} \"{reference.Name}\" needs "
                        + $"{plan.Along.Kind} as its place on this chart's continuous scale, and got {place} "
                        + $"({place.GetType().Name}).");
                }
            }

            return (plan.Along.Share(places.From) ?? double.NaN, plan.Along.Share(places.To) ?? double.NaN, low, high);
        }

        var (first, last) = (PlaceOf(places.From), PlaceOf(places.To));
        var half = band ? plan.Along.Slot / 2 : 0;
        return first < 0 || last < 0 ? (double.NaN, double.NaN, low, high)
            : (plan.Along.At[first] - half, plan.Along.At[last] + half, low, high);
    }

    // The first item whose X equals this one, or -1. Numbers of different types are equal when their values are, so
    // a whole number written in markup finds an item whose X is a double.
    private int PlaceOf(object x)
    {
        for (var place = 0; place < _items.Count; place++)
        {
            var item = X!(_items[place]);
            var numbers = item is not null && ChartValues.IsNumber(item) && ChartValues.IsNumber(x);
            if (numbers ? Number(item!) == Number(x) : Equals(item, x))
            {
                return place;
            }
        }

        return -1;
    }

    private static double Number(object value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);

    // A reference on the value axis runs across an upright chart and up a horizontal one, and a reference along the
    // items the other way around.
    private bool RunsUp(PlacedReference placed) => placed.Along != Horizontal;

    // A reference line's two ends in the plot, as shares from its left and top.
    private ((double X, double Y) From, (double X, double Y) To) Ends(PlacedReference line) => line.Along
        ? (Turn(line.Low, 0), Turn(line.Low, 1)) : (Turn(0, 1 - line.Low), Turn(1, 1 - line.Low));

    // A band's box in the plot, as shares from its left and top.
    private (double X, double Y, double Width, double Height) Box(PlacedReference band)
    {
        var (from, to) = band.Along ? (Turn(band.Low, 0), Turn(band.High, 1))
            : (Turn(0, 1 - band.High), Turn(1, 1 - band.Low));
        return (Math.Min(from.X, to.X), Math.Min(from.Y, to.Y), Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
    }

    // How far down the plot a reference that runs across it has its text: at the line, or at the band's upper edge.
    private static double Top(PlacedReference placed) => placed.Along ? placed.Low : 1 - placed.High;

    // The text of a reference that runs across the plot hangs under its place for a band, inside it, and for a line
    // too near the plot's top to have room over it.
    private bool TextUnder(PlacedReference placed) => !RunsUp(placed) && (placed.Band || Top(placed) < 0.15);

    // Where a reference's text sits. For one that runs across the plot it is as far down the plot as the line, or as
    // the band's upper edge. For one that runs up the plot it starts at the line, or at the band's first edge; past
    // the middle of the plot it ends at the line, or at the band's last edge. Either way it is no wider than the
    // plot is on that side, so it stays inside the plot.
    private string NameStyle(PlacedReference placed)
    {
        if (!RunsUp(placed))
        {
            return $"inset-block-start: {ChartPath.Percent(Top(placed))}";
        }

        var (side, at, room) = placed.Low > 0.5 ? ("end", 1 - placed.High, placed.High)
            : ("start", placed.Low, 1 - placed.Low);
        return $"inset-inline-{side}: {ChartPath.Percent(at)}; max-inline-size: {ChartPath.Percent(room)}";
    }

    // A series' classes: the color its position gives it, and its tone's color in place of that when it has one.
    private string Slot(int index) =>
        ClassNames.Join($"tb-chart__series--{index % Slots + 1}", ToneClass(_series[index].Tone));

    private static string? ToneClass(Tone? tone) =>
        tone is { } one ? ClassNames.Modifier("tb-chart__tone", one) : null;

    private static string? ToneName(Tone? tone) => tone?.ToString().ToLowerInvariant();

    // The tone each item's mark has in each series: nothing for a series that gives its marks none, or whose line
    // has no markers to show one, and none where the series has no value.
    private List<Tone?[]?> ItemTones(ChartPlan plan, bool markers) => [.. _series.Select((one, index) =>
        one.ItemTone is { } tone && (one.Bars || one.Dots || (one.HasMarkers ?? markers))
            ? _items.Select((item, place) => plan.Series[index].Text[place] is null ? null : tone(item)).ToArray()
            : null)];

    // Each place's tone, for the script's tooltip to give its key: a dash where the mark keeps its series' color,
    // and nothing when every mark does.
    private static string? Tones(Tone?[]? tones) => tones is null || Array.TrueForAll(tones, tone => tone is null)
        ? null : string.Join(' ', tones.Select(tone => ToneName(tone) ?? "-"));

    // Which shape a scatter series' marks have: its place among the chart's scatter series, hidden ones too, so the
    // first four always differ and none changes shape as others hide.
    private int ShapeOf(int index) => _series.Take(index).Count(one => one.Dots);

    private string? Color(int index) => _series[index].Color is { } color ? $"--_tb-chart-series: {color}" : null;

    // A bar's end that shows its value is rounded: the top or the bottom, or, on a bar that runs across, the end
    // toward the higher values or the start toward the lower ones.
    private string BarClass(ChartBarEnd end) => end switch
    {
        ChartBarEnd.Top => Horizontal ? "tb-chart__bar tb-chart__bar--end" : "tb-chart__bar tb-chart__bar--top",
        ChartBarEnd.Bottom => Horizontal ? "tb-chart__bar tb-chart__bar--start" : "tb-chart__bar tb-chart__bar--bottom",
        _ => "tb-chart__bar",
    };

    // A spot in the plot, as shares from its left and its top. A horizontal chart is the plan turned: what was up
    // the plot runs across it, and what ran along it runs down.
    private (double X, double Y) Turn(double x, double y) => Horizontal ? (1 - y, x) : (x, y);

    // A bar's box, as shares of the plot from its left and its top.
    private (double X, double Y, double Width, double Height) Box(ChartBar bar) => Horizontal
        ? (1 - bar.Y - bar.Height, bar.X, bar.Height, bar.Width) : (bar.X, bar.Y, bar.Width, bar.Height);

    // The value axis's labels: each with its share of the plot along that axis, from the top or, on a horizontal
    // chart, from the start, with its text and its stride.
    private IEnumerable<(double At, string Text, int Stride)> ValueLabels(ChartPlan plan) => plan.ValueTicks.Select(
        (tick, index) => (Horizontal ? 1 - tick.Y : tick.Y, tick.Text, ChartPlan.Stride(index, plan.ValueTicks.Count)));

    // The room a label needs, in characters: the longest one's, and half as much again when a label is on one of
    // the plot's edges, since that one isn't centered on its mark and reaches farther toward its neighbor. With a
    // label on each edge the two may be the only ones showing, and they need a whole label's room each.
    private static int Longest(IReadOnlyList<(double At, string Text, int Stride)> labels)
    {
        // A value axis whose ends hold no multiple of its step has no labels.
        if (labels.Count == 0)
        {
            return 0;
        }

        var longest = labels.Max(label => label.Text.Length);
        var (start, end) = (labels.Any(label => label.At <= 0), labels.Any(label => label.At >= 1));
        return start && end ? longest * 2 : start || end ? (longest * 3 + 1) / 2 : longest;
    }

    // A label up the plot's side: how far down it is, and on a horizontal chart, where the labels are the places'
    // names, its stride for the stylesheet to thin them by.
    private string SideStyle(double at, int stride) => Horizontal
        ? string.Create(CultureInfo.InvariantCulture, $"top: {ChartPath.Percent(at)}; --_tb-chart-stride: {stride}")
        : $"top: {ChartPath.Percent(at)}";

    // How many steps of the labels along the base fit across the plot: the places', or the value axis's on a
    // horizontal chart.
    private string BaseSteps(ChartPlan plan) => Horizontal
        ? (plan.ValueTicks.Count - 1).ToString(CultureInfo.InvariantCulture) : Steps(plan);

    // Where a label is along the axis: at its mark, or against the plot's far end when its mark is on that edge.
    private static string Along(double x, string? edge) =>
        edge == "end" ? "inset-inline-end: 0" : $"inset-inline-start: {ChartPath.Percent(x)}";

    private static string Steps(ChartPlan plan) =>
        Math.Round(plan.Along.Steps, 3).ToString(CultureInfo.InvariantCulture);

    // Where each place is along a continuous chart, for the script to find the one nearest the pointer, as whole
    // steps of the 10,000 grid. The script works out an even chart's places itself.
    private static string? At(ChartPlan plan) => !plan.Along.ByValue ? null
        : string.Join(' ', plan.Along.At.Select(at => Math.Round(at * ChartPath.Grid)
            .ToString(CultureInfo.InvariantCulture)));

    // One line for each place, for the script to read. A name with a line break in it would count as two.
    private static string Places(ChartPlan plan) =>
        string.Join('\n', plan.Places.Select(place => place.ReplaceLineEndings(" ")));

    // One line for each place, for the script to read: a value as the table writes it, empty where there is none.
    private static string Values(ChartSeriesPlan drawn) => string.Join('\n', drawn.Text);

    // Where the keys and the pointer find each of a series' values, as whole steps of the 10,000 grid: the middle of
    // a bar's value end, or a line's point. A place with no value has a dash.
    private string Points(ChartSeriesPlan drawn) => string.Join(' ', drawn.Bars
        ? drawn.Marks.Select(mark => mark is { } bar
            ? Spot(bar.X + bar.Width / 2, bar.Up ? bar.Y : bar.Y + bar.Height) : "-")
        : drawn.Points.Select(point => point is { } at ? Spot(at.X, at.Y) : "-"));

    private string Spot(double x, double y)
    {
        var (across, down) = Turn(x, y);
        return string.Create(CultureInfo.InvariantCulture,
            $"{Math.Round(across * ChartPath.Grid)},{Math.Round(down * ChartPath.Grid)}");
    }

    // What a screen reader hears for the drawing: the kind of chart and the series it shows, as "Bar chart:
    // Completed, Failed".
    private string Summary()
    {
        var shown = _series.Where(one => !one.IsHidden).ToList();
        // Any other mix of lines, areas and scatters reads as a line chart, and anything beside bars as a bar and
        // line chart.
        var kind = shown.All(one => one.Bars) ? Labels.Bars ?? Localizer[TbStrings.ChartBars]
            : shown.Any(one => one.Bars) ? Labels.BarsAndLines ?? Localizer[TbStrings.ChartBarsAndLines]
            : shown.All(one => one.Filled) ? Labels.Areas ?? Localizer[TbStrings.ChartAreas]
            : shown.All(one => one.Dots) ? Labels.Scatter ?? Localizer[TbStrings.ChartScatter]
            : Labels.Lines ?? Localizer[TbStrings.ChartLines];
        return string.Format(CultureInfo.CurrentCulture, Labels.Summary ?? Localizer[TbStrings.ChartSummary], kind,
            string.Join(", ", shown.Select(one => one.Name)));
    }

    /// <summary>Stops the chart's script from following the pointer and the keys.</summary>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_handle is not null)
        {
            await _handle.DisposeHandleAsync();
        }

        _events?.Dispose();
    }
}
