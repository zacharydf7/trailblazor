using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Sets how a <see cref="TbChart{TItem}"/> labels the axis its items run along. Write it inside the chart;
/// without one, the axis shows each item's <see cref="TbChart{TItem}.X"/> value as it reads by default.</summary>
public sealed class TbXAxis : ComponentBase, IDisposable
{
    private ChartView? _view;
    private ChartView? _viewGiven;
    private object? _shown;
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

    /// <summary>The value at the plot's start on a <see cref="ChartScale.Continuous"/> scale, such as <c>0</c> or
    /// a date, of the kind the items' <see cref="TbChart{TItem}.X"/> values are, and below <see cref="Max"/>.
    /// Without it the plot starts at the first item; or half a place before it when the chart shows bars; or, when
    /// it shows a scatter series and no bars on a scale of numbers, at the round value at or before it. It leaves
    /// no item out: an item before it is drawn outside the plot.</summary>
    [Parameter]
    public object? Min { get; set; }

    /// <summary>The value at the plot's end on a <see cref="ChartScale.Continuous"/> scale. Without it the plot ends
    /// at the last item, as <see cref="Min"/> says of its start.</summary>
    [Parameter]
    public object? Max { get; set; }

    /// <summary>The distance between the marks of a <see cref="ChartScale.Continuous"/> scale of numbers, such as
    /// <c>5</c>, above zero: the marks sit on its multiples. Without it the chart picks a round step. A step that
    /// would give more than about fifty marks, or fewer than two, is passed over for the chart's own. A scale of
    /// dates ignores it and picks its own marks.</summary>
    [Parameter]
    public double? Step { get; set; }

    /// <summary>Whether people can zoom into part of a <see cref="ChartScale.Continuous"/> scale and move along it:
    /// with buttons above the plot, with keys on it, by dragging it, and with Ctrl and the mouse wheel. The chart
    /// then shows only the items in its <see cref="View"/>. False by default.</summary>
    [Parameter]
    public bool Zoomable { get; set; }

    /// <summary>The part of the scale a <see cref="Zoomable"/> chart shows, or null for all of it. Bind it to
    /// follow or set what people zoom to; what they choose holds until the app gives this a new value.</summary>
    [Parameter]
    public ChartView? View { get; set; }

    /// <summary>Raised with the new view when people zoom or move along the chart, and with null when they show all
    /// of it.</summary>
    [Parameter]
    public EventCallback<ChartView?> ViewChanged { get; set; }

    // The part of the scale shown: what people last chose, or what the app last set if that came later.
    internal ChartView? CurrentView => Zoomable ? _view : null;

    internal async Task SetViewAsync(ChartView? view)
    {
        _view = view;
        await ViewChanged.InvokeAsync(view);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Axes is null)
        {
            throw new InvalidOperationException($"{nameof(TbXAxis)} must be inside a {nameof(TbChart<>)}.");
        }

        if (Step <= 0 || !ChartAlong.InOrder(Min, Max))
        {
            throw new InvalidOperationException(
                $"{nameof(TbXAxis)} needs a {nameof(Step)} above zero and a {nameof(Min)} below its {nameof(Max)}.");
        }

        if (Zoomable && Scale != ChartScale.Continuous)
        {
            throw new InvalidOperationException(
                $"A {nameof(Zoomable)} {nameof(TbXAxis)} needs a {nameof(ChartScale.Continuous)} {nameof(Scale)}.");
        }

        if (View is { } view && (!ChartAlong.InOrder(view.From, view.To) || Equals(view.From, view.To)))
        {
            throw new InvalidOperationException(
                $"{nameof(TbXAxis)} needs a {nameof(View)} whose {nameof(ChartView.From)} is before its "
                + $"{nameof(ChartView.To)}.");
        }

        if (!Equals(View, _viewGiven))
        {
            _viewGiven = _view = View;
        }

        // A view is an object, which Blazor counts as changed on every render of the chart, so the axis has the
        // chart draw again only when something it shows has changed, or the chart would draw again and again.
        object shown = (Title, Format, MarkFormat, Scale, Min, Max, Step, Zoomable, _view);
        if (!shown.Equals(_shown))
        {
            _shown = shown;
            Axes.Set(this);
        }
    }

    /// <summary>Takes the axis's settings out of its chart.</summary>
    public void Dispose() => Axes?.Remove(this);
}
