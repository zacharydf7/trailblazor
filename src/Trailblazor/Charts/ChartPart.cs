namespace Trailblazor;

// One series as a chart draws it: a series component, or one of the series that a series with SplitBy makes, one for
// each value of its field, named by that value and showing the items that have it.
internal sealed class ChartPart<TItem>(TbChartSeries<TItem> source, object? key, string name, bool hidden)
{
    public TbChartSeries<TItem> Source { get; } = source;

    // The value of the series' SplitBy that this part shows, or null for a series that isn't split.
    public object? Key { get; } = key;

    public string Name { get; } = name;

    public bool IsHidden { get; } = hidden;

    public bool Bars => Source.Bars;

    public bool Filled => Source.Filled;

    public bool Dots => Source.Dots;

    public ChartCurve Line => Source.Line;

    public bool? HasMarkers => Source.HasMarkers;

    public bool ShowsValues => Source.ShowsValues;

    public Tone? Tone => Source.Tone;

    public string? Color => Source.Color;

    // Which of a place's items this part shows: the first, or for a split series the first with this part's value,
    // or -1 where the place has none.
    public int IndexAt(IReadOnlyList<TItem> items)
    {
        if (Key is null)
        {
            return 0;
        }

        for (var index = 0; index < items.Count; index++)
        {
            if (Equals(Source.SplitBy!(items[index]), Key))
            {
                return index;
            }
        }

        return -1;
    }

    public double? ValueAt(IReadOnlyList<TItem> items) => IndexAt(items) is >= 0 and var index
        ? ChartValues.Number(Source.Value!(items[index]), Name) : null;
}
