namespace Trailblazor;

// Where each bar of a stack starts and ends. Values above zero pile up from zero, and values below zero pile down
// from it, so a loss never hides inside a gain.
internal static class ChartStack
{
    // values[series][place]; a missing value (null) takes no room. The answer has the same shape.
    public static (double From, double To)?[][] Pile(IReadOnlyList<IReadOnlyList<double?>> values, bool asShares)
    {
        var places = values.Count == 0 ? 0 : values[0].Count;
        var piled = new (double From, double To)?[values.Count][];
        for (var series = 0; series < values.Count; series++)
        {
            piled[series] = new (double From, double To)?[places];
        }

        for (var place = 0; place < places; place++)
        {
            // As shares, each value is its part of the place's total, and the parts add up to 1. A value below zero
            // has no share of a whole, so it counts as nothing there.
            var total = asShares ? values.Sum(series => Math.Max(series[place] ?? 0, 0)) : 1;
            double above = 0, below = 0;
            for (var series = 0; series < values.Count; series++)
            {
                if (values[series][place] is not { } value)
                {
                    continue;
                }

                if (asShares)
                {
                    value = total == 0 ? 0 : Math.Max(value, 0) / total;
                }

                piled[series][place] = value >= 0 ? (above, above += value) : (below, below += value);
            }
        }

        return piled;
    }
}
