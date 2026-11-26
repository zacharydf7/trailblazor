namespace Trailblazor;

// The round values a number axis marks, and the ends they give the axis: steps of 1, 2 or 5 times a power of ten.
internal readonly record struct ChartTicks(double Min, double Max, double Step)
{
    // Float division lands a hair off a whole number of steps, such as 0.3 / 0.1; this much is taken as on it.
    private const double Slack = 1e-9;

    // The digits after the decimal point that the step needs, so labels neither round a tick nor pad it.
    public int Decimals => Math.Clamp(-(int)Math.Floor(Math.Log10(Step) + Slack), 0, 15);

    // Each tick is counted from the low end, not added up, so a long axis doesn't drift.
    public IReadOnlyList<double> Values
    {
        get
        {
            var decimals = Decimals;
            var (min, step) = (Min, Step);
            var steps = (int)Math.Round((Max - Min) / Step);
            return [.. Enumerable.Range(0, steps + 1).Select(index => Math.Round(min + index * step, decimals))];
        }
    }

    // Where a value sits between the axis's ends: 0 at the low end and 1 at the high one.
    public double Share(double value) => (value - Min) / (Max - Min);

    // About `count` steps that cover the values from lowest to highest, with the ends on ticks.
    public static ChartTicks Cover(double lowest, double highest, int count = 5)
    {
        if (!(highest > lowest))
        {
            // One value, or none, still needs an axis with some height: from zero to the value, or to one.
            (lowest, highest) = (Math.Min(lowest, 0), Math.Max(highest, 0));
            if (lowest == highest)
            {
                highest = 1;
            }
        }

        var step = StepFor(highest - lowest, count);
        var decimals = Math.Clamp(-(int)Math.Floor(Math.Log10(step) + Slack), 0, 15);
        return new(Math.Round(Math.Floor(lowest / step + Slack) * step, decimals),
            Math.Round(Math.Ceiling(highest / step - Slack) * step, decimals), step);
    }

    private static double StepFor(double span, int count)
    {
        var rough = span / count;
        var power = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var ratio = rough / power;
        // The nearest of 1, 2, 5 and 10 by ratio: the line between two neighbors is their geometric mean.
        var factor = ratio < Math.Sqrt(2) ? 1 : ratio < Math.Sqrt(10) ? 2 : ratio < Math.Sqrt(50) ? 5 : 10;
        return factor * power;
    }
}
