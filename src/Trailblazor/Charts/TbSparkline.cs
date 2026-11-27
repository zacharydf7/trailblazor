using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A small chart with no axes, for the shape of a few values beside a number or in a table's cell: a line,
/// an area or bars. It is a picture, drawn as markup with no script, that <see cref="AriaLabel"/> names. For values
/// people need to read, use a <see cref="TbChart{TItem}"/>.</summary>
public partial class TbSparkline<TValue>
{
    // The share of a bar's room that the bar takes; the rest is the gap between it and its neighbors.
    private const double BarShare = 0.8;

    // The drawing has this box however large the sparkline is, and is stretched to fit it.
    private static readonly string Box = $"0 0 {ChartPath.Grid} {ChartPath.Grid}";

    /// <summary>A label this sparkline's use is counted under, such as <c>"hikers-this-week"</c>, so the app can
    /// count how often people see it through the <c>Trailblazor</c> meter. Controls that share a label count
    /// together; a control without one isn't counted. It names what the control shows, never the user.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => field ??= new(Services, GetType());

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    /// <summary>The values to draw, in order, such as the hikers on each of the last twelve days. Required. Any
    /// number type works; null, or a value that isn't finite, leaves a gap. A value that isn't a number throws.
    /// </summary>
    [Parameter, EditorRequired]
    public IEnumerable<TValue>? Values { get; set; }

    /// <summary>What screen readers hear for the picture, such as "Hikers a day over the last two weeks, rising".
    /// Say what the shape shows, since no value is read out. Without it the picture is hidden from screen readers,
    /// which is right only when the text beside it already says the same.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    /// <summary>Whether the values are drawn as a line, an area or bars. <see cref="SparklineKind.Line"/> by
    /// default.</summary>
    [Parameter]
    public SparklineKind Kind { get; set; }

    /// <summary>A tone for a sparkline whose color should say what it means, such as
    /// <see cref="Trailblazor.Tone.Danger"/> for something going wrong. A <see cref="Color"/> wins over it.</summary>
    [Parameter]
    public Tone? Tone { get; set; }

    /// <summary>A CSS color for the sparkline, in place of the first of the theme's series colors. Check that it
    /// has 3:1 contrast against the surface in both themes.</summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>The value at the bottom of the picture. Without it, a line starts at its lowest value, and an area
    /// or bars at zero, or lower when a value is below zero. Give several sparklines the same <c>Min</c> and
    /// <see cref="Max"/> to compare them with each other.</summary>
    [Parameter]
    public double? Min { get; set; }

    /// <summary>The value at the top of the picture. Without it, the highest value is at the top, or zero for an
    /// area or bars whose values are all below zero.</summary>
    [Parameter]
    public double? Max { get; set; }

    /// <summary>How wide the picture is, as a CSS length such as <c>"8rem"</c>. Without it, the
    /// <c>--tb-sparkline-width</c> token sets it, 6rem by default.</summary>
    [Parameter]
    public string? Width { get; set; }

    /// <summary>How high the picture is, as a CSS length such as <c>"2rem"</c>. Without it, the
    /// <c>--tb-sparkline-height</c> token sets it, 1.5em by default, about a line of the text around it.</summary>
    [Parameter]
    public string? Height { get; set; }

    // A blank label names nothing, and the picture is hidden from screen readers as it is without one.
    private string? Name => string.IsNullOrWhiteSpace(AriaLabel) ? null : AriaLabel;

    // A bar, as shares of the picture from its left and its top.
    private readonly record struct Bar(double X, double Y, double Width, double Height);

    // What is drawn: the bars, or a line's path and, for an area, the path of its fill.
    private sealed record Drawing(string Line, string Area, IReadOnlyList<Bar> Bars);

    private Drawing Draw()
    {
        List<double?> values = [.. (Values ?? []).Select(Number)];
        List<double> given = [.. values.OfType<double>()];
        if (given.Count == 0)
        {
            return new("", "", []);
        }

        // A line shows its shape from its lowest value to its highest; an area and bars measure from zero.
        var fromZero = Kind != SparklineKind.Line;
        var lowest = Min ?? (fromZero ? Math.Min(0, given.Min()) : given.Min());
        var highest = Max ?? (fromZero ? Math.Max(0, given.Max()) : given.Max());
        // A value beyond a Min or a Max is drawn at the picture's edge, and so is every value when the app set that
        // end beyond them all. With every value the same there is nothing to scale by, and the values sit halfway up.
        double Up(double value) => highest > lowest ? Math.Clamp((highest - value) / (highest - lowest), 0, 1)
            : value > highest ? 0 : value < lowest ? 1 : 0.5;
        var floor = Up(0);
        if (Kind == SparklineKind.Bars)
        {
            // Each value has an equal share of the width, and its bar runs between its value and zero.
            var room = 1d / values.Count;
            List<Bar> bars = [];
            for (var place = 0; place < values.Count; place++)
            {
                if (values[place] is { } value)
                {
                    bars.Add(new((place + (1 - BarShare) / 2) * room, Math.Min(Up(value), floor), room * BarShare,
                        Math.Abs(Up(value) - floor)));
                }
            }

            return new("", "", bars);
        }

        // The first value is at the left edge and the last at the right. One value alone sits in the middle, where
        // a line of no length is drawn as a dot.
        double Along(int place) => values.Count == 1 ? 0.5 : (double)place / (values.Count - 1);
        List<(double X, double Y)?> points = [.. values.Select((value, place) =>
            value is { } number ? ((double X, double Y)?)(Along(place), Up(number)) : null)];
        List<(double X, double Y)?> under = [.. points.Select(point =>
            point is { } at ? ((double X, double Y)?)(at.X, floor) : null)];
        return new(ChartPath.Line(points, ChartCurve.Straight),
            Kind == SparklineKind.Area ? ChartPath.Area(points, under, ChartCurve.Straight) : "", []);
    }

    private static double? Number(TValue value) => value is not null && !ChartValues.IsNumber(value)
        ? throw new InvalidOperationException(
            $"{nameof(TbSparkline<>)} got a value of type {value.GetType().Name}, which isn't a number.")
        : ChartValues.Number(value, "");

}
