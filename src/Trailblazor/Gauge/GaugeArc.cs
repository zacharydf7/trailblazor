namespace Trailblazor;

// The arc a radial gauge is drawn along: three quarters of a circle, open at the bottom, running clockwise from the
// lower left to the lower right, around the middle of a box as wide as a pie's.
internal static class GaugeArc
{
    // The box the arc is drawn in. It is shorter than it is wide, since the arc's open quarter is at the bottom.
    public const string Box = "0 0 100 86";

    // Where the arc starts, and how much of a full turn it covers.
    private const double Start = 0.625;
    private const double Sweep = 0.75;

    // The path along the arc between two shares of it, at a distance from the middle, or an empty string when it has
    // no length.
    public static string Path(double from, double to, double radius)
    {
        if (to <= from)
        {
            return "";
        }

        var (start, end) = (Start + from * Sweep, Start + to * Sweep);
        var large = end - start > 0.5 ? 1 : 0;
        var size = ChartArc.Number(radius);
        return $"M{ChartArc.Spot(start, radius)} A{size} {size} 0 {large} 1 {ChartArc.Spot(end, radius)}";
    }
}
