namespace Trailblazor;

// The values a number axis marks, which are the multiples of its step between its ends. An axis the chart picks has
// a step of 1, 2 or 5 times a power of ten and its ends on marks; an app may set a step or an end of its own.
internal readonly record struct ChartTicks(double Min, double Max, double Step)
{
    // Float division lands a hair off a whole number of steps, such as 0.3 / 0.1; this much is taken as on it.
    internal const double Slack = 1e-9;

    // The digits after the decimal point that the step has, so labels neither round a tick nor pad it: 1 for a step
    // of 2.5, and 2 for one of 0.25.
    public int Decimals => DecimalsOf(Step);

    // Each tick is a whole number of steps, not a running sum, so a long axis doesn't drift. An end that isn't on a
    // step has no tick.
    public IReadOnlyList<double> Values
    {
        get
        {
            var decimals = Decimals;
            var step = Step;
            var first = (long)Math.Ceiling(Min / Step - Slack);
            var last = (long)Math.Floor(Max / Step + Slack);
            var ticks = new List<double>();
            for (var index = first; index <= last; index++)
            {
                ticks.Add(Math.Round(index * step, decimals));
            }

            return ticks;
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

        return Reach(lowest, highest, StepFor(highest - lowest, count));
    }

    // The nearest multiples of the step that hold the values from lowest to highest between them.
    public static ChartTicks Reach(double lowest, double highest, double step)
    {
        var decimals = DecimalsOf(step);
        return new(Math.Round(Math.Floor(lowest / step + Slack) * step, decimals),
            Math.Round(Math.Ceiling(highest / step - Slack) * step, decimals), step);
    }

    private static int DecimalsOf(double step)
    {
        var decimals = 0;
        while (decimals < 15 && Math.Abs(Math.Round(step, decimals) - step) > Math.Abs(step) * Slack)
        {
            decimals++;
        }

        return decimals;
    }

    // The shortest step of 1, 2 or 5 times a power of ten that is no shorter than a length.
    internal static double StepNoLessThan(double length)
    {
        var power = Math.Pow(10, Math.Floor(Math.Log10(length)));
        var ratio = length / power;
        return (ratio <= 1 + Slack ? 1 : ratio <= 2 + Slack ? 2 : ratio <= 5 + Slack ? 5 : 10) * power;
    }

    // The next shorter step of 1, 2 or 5 times a power of ten: 2 after 5, 1 after 2, and 0.5 after 1.
    internal static double StepBelow(double step)
    {
        var power = Math.Pow(10, Math.Floor(Math.Log10(step) + Slack));
        var ratio = step / power;
        return ratio > 5 - Slack ? 2 * power : ratio > 2 - Slack ? power : power / 2;
    }

    internal static double StepFor(double span, int count)
    {
        var rough = span / count;
        var power = Math.Pow(10, Math.Floor(Math.Log10(rough)));
        var ratio = rough / power;
        // The nearest of 1, 2, 5 and 10 by ratio: the line between two neighbors is their geometric mean.
        var factor = ratio < Math.Sqrt(2) ? 1 : ratio < Math.Sqrt(10) ? 2 : ratio < Math.Sqrt(50) ? 5 : 10;
        return factor * power;
    }
}
