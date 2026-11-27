using System.Globalization;

namespace Trailblazor;

// One slice of a pie: its share of the whole, its outline in the pie's box, and the spot in its middle, as shares of
// that box, where the keys' focus ring and the tooltip sit.
internal sealed record ChartSlice(double Share, string Path, (double X, double Y) Middle);

// The slices of a pie: each value as its share of the total, drawn clockwise from twelve o'clock in the order given.
internal static class ChartArc
{
    // The pie's box is this wide and this high, and the pie fills it.
    public const double Box = 100;

    private const double Radius = Box / 2;

    // How far out a pie slice's middle is, as a share of the radius: past halfway, where a thin slice is wider.
    private const double MiddleOut = 0.6;

    // A value that is missing, zero or below zero has no slice. The hole is the share of the radius a donut leaves
    // open, and zero for a pie.
    public static IReadOnlyList<ChartSlice?> Slices(IReadOnlyList<double?> values, double hole)
    {
        var total = values.Sum(value => value > 0 ? value.Value : 0);
        var slices = new ChartSlice?[values.Count];
        var from = 0d;
        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] is > 0 and var value)
            {
                var share = value / total;
                slices[index] = new(share, Outline(from, from + share, hole), Middle(from + share / 2, hole));
                from += share;
            }
        }

        return slices;
    }

    // A slice from one turn of the circle to another, each a share of a full turn: along the rim, then back to the
    // pie's middle or along the edge of a donut's hole. A whole pie has no edge to its middle, and a whole donut's
    // hole is a circle of its own, drawn the other way.
    private static string Outline(double from, double to, double hole)
    {
        var inner = Radius * hole;
        var rim = $"{Spot(from, Radius)}{Arcs(from, to, Radius)}";
        if (to - from > 1 - 1e-9)
        {
            return hole > 0 ? $"M{rim} Z M{Spot(to, inner)}{Arcs(to, from, inner)} Z" : $"M{rim} Z";
        }

        return hole > 0 ? $"M{rim} L{Spot(to, inner)}{Arcs(to, from, inner)} Z"
            : $"M{Number(Radius)} {Number(Radius)} L{rim} Z";
    }

    // The arcs along a circle from one turn to another, clockwise when the second is the greater. More than half a
    // turn is two arcs through its middle: one arc can't go all the way around, and one that nearly does would
    // start and end on the same spot once its ends are rounded, which draws nothing.
    private static string Arcs(double from, double to, double radius)
    {
        var arc = $" A{Number(radius)} {Number(radius)} 0 0 {(to > from ? 1 : 0)} ";
        return Math.Abs(to - from) > 0.5 ? $"{arc}{Spot((from + to) / 2, radius)}{arc}{Spot(to, radius)}"
            : $"{arc}{Spot(to, radius)}";
    }

    // Halfway around the slice, and halfway across a donut's ring.
    private static (double X, double Y) Middle(double turn, double hole)
    {
        var (x, y) = Point(turn, Radius * (hole > 0 ? (1 + hole) / 2 : MiddleOut));
        return (x / Box, y / Box);
    }

    private static (double X, double Y) Point(double turn, double radius) =>
        (Radius + radius * Math.Sin(turn * Math.Tau), Radius - radius * Math.Cos(turn * Math.Tau));

    private static string Spot(double turn, double radius)
    {
        var (x, y) = Point(turn, radius);
        return $"{Number(x)} {Number(y)}";
    }

    // Adding zero turns a rounded "-0" into "0".
    private static string Number(double value) =>
        (Math.Round(value, 2) + 0d).ToString(CultureInfo.InvariantCulture);
}
