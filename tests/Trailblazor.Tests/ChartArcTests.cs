namespace Trailblazor.Tests;

public class ChartArcTests
{
    [Fact]
    public void Slices_ShouldGiveEachValueItsShareClockwiseFromTheTop_WhenGivenValues()
    {
        var slices = ChartArc.Slices([50, 25, 25], hole: 0);

        Assert.Equal([0.5, 0.25, 0.25], slices.Select(slice => slice!.Share));
        // The first slice is the right half, from the top to the bottom, and the next the lower left quarter.
        Assert.Equal(["M50 50 L50 0 A50 50 0 0 1 50 100 Z", "M50 50 L50 100 A50 50 0 0 1 0 50 Z",
            "M50 50 L0 50 A50 50 0 0 1 50 0 Z"], slices.Select(slice => slice!.Path));
        // A slice's middle is halfway around it and six tenths of the way out.
        Assert.Equal((0.8, 0.5), Rounded(slices[0]!.Middle));
        Assert.Equal((0.288, 0.712), Rounded(slices[1]!.Middle));
    }

    [Fact]
    public void Slices_ShouldGoAroundInTwoArcsThroughTheMiddle_WhenASliceIsMoreThanHalf()
    {
        var slices = ChartArc.Slices([75, 25], hole: 0);

        Assert.Equal("M50 50 L50 0 A50 50 0 0 1 85.36 85.36 A50 50 0 0 1 0 50 Z", slices[0]!.Path);
        Assert.Equal("M50 50 L0 50 A50 50 0 0 1 50 0 Z", slices[1]!.Path);
    }

    // One arc for such a slice would start and end on the same rounded spot, and a browser draws nothing for that.
    [Theory]
    [InlineData(0d, "M50 50 L50 0 A50 50 0 0 1 50 100 A50 50 0 0 1 50 0 Z")]
    [InlineData(0.6, "M50 0 A50 50 0 0 1 50 100 A50 50 0 0 1 50 0 L50 20 A30 30 0 0 0 50 80 A30 30 0 0 0 50 20 Z")]
    public void Slices_ShouldStillDrawTheLargeSlice_WhenItIsNearlyTheWholeTotal(double hole, string path)
    {
        var slices = ChartArc.Slices([1_000_000, 10], hole);

        Assert.Equal(path, slices[0]!.Path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    [InlineData(-5d)]
    public void Slices_ShouldLeaveAValueOut_WhenItIsMissingOrNotAboveZero(double? none)
    {
        var slices = ChartArc.Slices([30, none, 10], hole: 0);

        // The value takes no part of the total, and the slice after it starts where the one before it ended.
        Assert.Null(slices[1]);
        Assert.Equal((0.75, 0.25), (slices[0]!.Share, slices[2]!.Share));
        Assert.Equal("M50 50 L0 50 A50 50 0 0 1 50 0 Z", slices[2]!.Path);
    }

    [Fact]
    public void Slices_ShouldDrawARingBetweenTwoArcs_WhenThereIsAHole()
    {
        var slices = ChartArc.Slices([50, 50], hole: 0.6);

        // Out along the outer arc, in to the hole's edge, and back along the inner arc the other way.
        Assert.Equal("M50 0 A50 50 0 0 1 50 100 L50 80 A30 30 0 0 0 50 20 Z", slices[0]!.Path);
        // The middle is halfway between the hole's edge and the rim.
        Assert.Equal((0.9, 0.5), Rounded(slices[0]!.Middle));
    }

    [Theory]
    [InlineData(0d, "M50 0 A50 50 0 0 1 50 100 A50 50 0 0 1 50 0 Z")]
    [InlineData(0.6, "M50 0 A50 50 0 0 1 50 100 A50 50 0 0 1 50 0 Z M50 20 A30 30 0 0 0 50 80 A30 30 0 0 0 50 20 Z")]
    public void Slices_ShouldDrawAWholeCircleAsTwoHalves_WhenOneValueIsTheWholeTotal(double hole, string path)
    {
        var slices = ChartArc.Slices([0, 12], hole);

        Assert.Null(slices[0]);
        Assert.Equal((1d, path), (slices[1]!.Share, slices[1]!.Path));
    }

    [Fact]
    public void Slices_ShouldHaveNone_WhenNoValueIsAboveZero()
    {
        Assert.All(ChartArc.Slices([0, null], hole: 0), Assert.Null);
    }

    private static (double X, double Y) Rounded((double X, double Y) spot) =>
        (Math.Round(spot.X, 3), Math.Round(spot.Y, 3));
}
