using Trailblazor.Primitives;

namespace Trailblazor;

internal static class Anchors
{
    public static AnchorSide Side(Placement placement) => placement switch
    {
        Placement.Top => AnchorSide.Top,
        Placement.Bottom => AnchorSide.Bottom,
        Placement.Start => AnchorSide.Start,
        Placement.End => AnchorSide.End,
        _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, null),
    };

    public static AnchorAlign Align(PlacementAlign align) => align switch
    {
        PlacementAlign.Start => AnchorAlign.Start,
        PlacementAlign.Center => AnchorAlign.Center,
        PlacementAlign.End => AnchorAlign.End,
        _ => throw new ArgumentOutOfRangeException(nameof(align), align, null),
    };
}
