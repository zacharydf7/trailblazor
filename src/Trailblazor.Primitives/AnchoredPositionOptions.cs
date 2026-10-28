namespace Trailblazor.Primitives;

internal sealed record AnchoredPositionOptions
{
    public AnchorSide Side { get; init; } = AnchorSide.Bottom;

    public AnchorAlign Align { get; init; } = AnchorAlign.Center;

    public double Offset { get; init; }
}
