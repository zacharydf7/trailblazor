namespace Trailblazor;

// The reference lines and bands written inside a chart, in the order they first drew. They aren't generic, so they
// find their chart through this, not the chart itself.
internal sealed class ChartReferences(Action changed)
{
    private readonly List<TbChartReference> _all = [];

    public IReadOnlyList<TbChartReference> All => _all;

    public void Add(TbChartReference reference)
    {
        _all.Add(reference);
        changed();
    }

    public void Remove(TbChartReference reference)
    {
        if (_all.Remove(reference))
        {
            changed();
        }
    }

    // A reference whose parameters changed has its chart draw again.
    public void Changed() => changed();
}
