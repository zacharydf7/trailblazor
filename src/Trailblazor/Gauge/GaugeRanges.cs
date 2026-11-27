namespace Trailblazor;

// The ranges written inside a gauge. They find their gauge through this, so a range works in any kind of gauge.
internal sealed class GaugeRanges(Action changed)
{
    private readonly List<TbGaugeRange> _ranges = [];

    public void Add(TbGaugeRange range)
    {
        _ranges.Add(range);
        changed();
    }

    public void Remove(TbGaugeRange range)
    {
        if (_ranges.Remove(range))
        {
            changed();
        }
    }

    // A range whose parameters changed has its gauge draw again.
    public void Changed() => changed();

    // Each range with the values it runs between: from where the range below it ends, or the gauge's lowest value,
    // to its own end, or the gauge's highest value when it has none. They go by their ends, since a range written
    // later, or one that appears later, can end below the others. A range that ends outside the gauge stops at that
    // end, and one left with no length is left out.
    public IReadOnlyList<(double From, double To, TbGaugeRange Range)> Between(double min, double max)
    {
        var bands = new List<(double, double, TbGaugeRange)>(_ranges.Count);
        var from = min;
        foreach (var range in _ranges.OrderBy(range => range.To ?? double.PositiveInfinity))
        {
            var to = Math.Clamp(range.To ?? max, from, max);
            if (to > from)
            {
                bands.Add((from, to, range));
                from = to;
            }
        }

        return bands;
    }
}
