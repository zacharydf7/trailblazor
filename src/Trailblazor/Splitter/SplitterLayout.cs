namespace Trailblazor;

// A layout the browser saved: each pane's size in percent and whether it is collapsed.
internal sealed record SplitterLayout(double[] Sizes, bool[] Collapsed);
