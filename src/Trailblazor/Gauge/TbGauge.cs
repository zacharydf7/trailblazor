using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A dial that shows where one value is between two ends, such as a server's load or a tank's level, with
/// the value as text in its middle. <see cref="TbGaugeRange"/> children mark ranges of its values, such as "High",
/// along it. It is a <c>meter</c>, drawn as markup with no script. For how far along a task is, use a
/// <see cref="TbProgress"/>.</summary>
public partial class TbGauge
{
    // How far from the arc's middle each is drawn, in the arc's box: the value's arc alone, the value's arc inside
    // the ranges' bands, and the bands.
    private const double Radius = 45;
    private const double RadiusInsideBands = 39.5;
    private const double BandRadius = 48;

    // The share of the arc a band stops short of where it meets another, so the surface shows between them.
    private const double BandGap = 0.004;

    /// <summary>How wide the gauge is, as a CSS length such as <c>"8rem"</c>. Its text scales with it. Without it,
    /// the <c>--tb-gauge-diameter</c> token sets it, 10rem by default.</summary>
    [Parameter]
    public string? Diameter { get; set; }

    // A band stops short only on a side where another band follows or comes before it.
    private string BandPath(IReadOnlyList<(double From, double To, TbGaugeRange Range)> bands, int place) =>
        GaugeArc.Path(Share(bands[place].From) + (place > 0 ? BandGap : 0),
            Share(bands[place].To) - (place < bands.Count - 1 ? BandGap : 0), BandRadius);
}
