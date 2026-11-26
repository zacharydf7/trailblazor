namespace Trailblazor;

// The axes written inside a chart. They aren't generic, so they find their chart through this, not the chart itself.
internal sealed class ChartAxes(Action changed)
{
    public TbXAxis? X { get; private set; }

    public TbYAxis? Y { get; private set; }

    public void Set(TbXAxis axis)
    {
        X = axis;
        changed();
    }

    public void Set(TbYAxis axis)
    {
        Y = axis;
        changed();
    }

    public void Remove(TbXAxis axis)
    {
        if (X == axis)
        {
            X = null;
            changed();
        }
    }

    public void Remove(TbYAxis axis)
    {
        if (Y == axis)
        {
            Y = null;
            changed();
        }
    }
}
