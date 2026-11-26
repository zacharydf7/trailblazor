using System.Text.RegularExpressions;

namespace Trailblazor.Tests;

public class ChartPathTests
{
    [Fact]
    public void Line_ShouldWriteEachPointAsItsDistanceFromTheLast_WhenStraight()
    {
        var path = ChartPath.Line([(0, 1), (0.5, 0.5), (1, 0)], ChartCurve.Straight);

        Assert.Equal("M0 10000l5000 -5000l5000 -5000", path);
    }

    [Fact]
    public void Line_ShouldStopAndStartAgain_WhenAPointIsMissing()
    {
        var path = ChartPath.Line([(0, 0), (0.1, 0), null, (0.5, 0.5), (0.6, 0.5)], ChartCurve.Straight);

        Assert.Equal("M0 0l1000 0M5000 5000l1000 0", path);
    }

    [Fact]
    public void Line_ShouldDrawADot_WhenAPointHasAGapOnBothSides()
    {
        var path = ChartPath.Line([null, (0.5, 0.5), null], ChartCurve.Smooth);

        Assert.Equal("M5000 5000h0", path);
    }

    [Fact]
    public void Line_ShouldBeEmpty_WhenEveryPointIsMissing()
    {
        Assert.Equal("", ChartPath.Line([null, null], ChartCurve.Straight));
    }

    [Fact]
    public void Line_ShouldHoldEachValueThenJump_WhenStepped()
    {
        var path = ChartPath.Line([(0, 0.5), (0.5, 0.2), (1, 0.2)], ChartCurve.Stepped);

        Assert.Equal("M0 5000h5000v-3000h5000v0", path);
    }

    [Fact]
    public void Line_ShouldPassThroughEveryPoint_WhenSmooth()
    {
        (double X, double Y)?[] points = [(0, 0.9), (0.25, 0.9), (0.5, 0.1), (0.75, 0.1), (1, 0.6)];

        var curves = Curves(ChartPath.Line(points, ChartCurve.Smooth));

        Assert.Equal(points.Skip(1).Select(point => (Whole(point!.Value.X), Whole(point.Value.Y))),
            curves.Select(curve => curve.End));
    }

    // A gentle stretch beside a steep one, all falling: where a plain smooth curve bulges past the data.
    [Fact]
    public void Line_ShouldStayBetweenThePointsItJoins_WhenSmooth()
    {
        (double X, double Y)?[] points = [(0, 0.9), (0.25, 0.89), (0.5, 0.1), (0.75, 0.05), (1, 0.04)];

        var curves = Curves(ChartPath.Line(points, ChartCurve.Smooth));

        Assert.All(curves, curve =>
        {
            var (low, high) = (Math.Min(curve.Start.Y, curve.End.Y), Math.Max(curve.Start.Y, curve.End.Y));
            Assert.InRange(curve.First.Y, low, high);
            Assert.InRange(curve.Second.Y, low, high);
        });
    }

    [Fact]
    public void Line_ShouldLeaveAPeakFlat_WhenSmooth()
    {
        var curves = Curves(ChartPath.Line([(0, 0.8), (0.5, 0.2), (1, 0.5)], ChartCurve.Smooth));

        // The handles on either side of the peak are level with it.
        Assert.Equal(2000, curves[0].Second.Y);
        Assert.Equal(2000, curves[1].First.Y);
    }

    [Fact]
    public void Area_ShouldGoAlongTheLineThenBackAlongTheFloor_WhenStraight()
    {
        var path = ChartPath.Area([(0, 0.5), (0.5, 0.2), (1, 0.4)], [(0, 1), (0.5, 1), (1, 1)], ChartCurve.Straight);

        Assert.Equal("M0 5000l5000 -3000l5000 2000v6000l-5000 0l-5000 0z", path);
    }

    [Fact]
    public void Area_ShouldBeAShapeForEachRun_WhenAPointIsMissing()
    {
        // The last point has a gap on both sides, and no width to fill.
        var path = ChartPath.Area([(0, 0.5), (0.25, 0.5), null, (0.75, 0.5), null],
            [(0, 1), (0.25, 1), null, (0.75, 1), null], ChartCurve.Straight);

        Assert.Equal("M0 5000l2500 0v5000l-2500 0z", path);
    }

    [Fact]
    public void Area_ShouldFollowItsFloorsSteps_WhenStepped()
    {
        var path = ChartPath.Area([(0, 0.5), (0.5, 0.2)], [(0, 1), (0.5, 0.8)], ChartCurve.Stepped);

        // Back along the floor, the jump comes before the level stretch, so the floor is the line that Line draws.
        Assert.Equal("M0 5000h5000v-3000v6000v2000h-5000z", path);
    }

    [Fact]
    public void Area_ShouldFollowItsFloorsCurve_WhenSmooth()
    {
        (double X, double Y)?[] floor = [(0, 0.9), (0.25, 0.7), (0.5, 0.75), (0.75, 0.4), (1, 0.6)];
        (double X, double Y)?[] line = [.. floor.Select(point => ((double, double)?)(point!.Value.X, 0.1))];

        var area = ChartPath.Area(line, floor, ChartCurve.Smooth);

        // The way back starts at the floor's last point and is the floor's own line, end for end.
        var back = Curves("M10000 6000" + Regex.Match(area, @"v-?\d+(.*)z$").Groups[1].Value);
        var along = Curves(ChartPath.Line(floor, ChartCurve.Smooth));
        Assert.Equal(along.Count, back.Count);
        foreach (var (there, here) in along.AsEnumerable().Reverse().Zip(back))
        {
            Assert.Equal((there.End, there.Start), (here.Start, here.End));
            Assert.InRange(here.First.Y - there.Second.Y, -1, 1);
            Assert.InRange(here.Second.Y - there.First.Y, -1, 1);
            Assert.InRange(here.First.X - there.Second.X, -1, 1);
            Assert.InRange(here.Second.X - there.First.X, -1, 1);
        }
    }

    private static int Whole(double share) => (int)Math.Round(share * ChartPath.Grid);

    // Each curve of a smooth path, as the places of its start, its two handles and its end on the grid.
    private static List<((int X, int Y) Start, (int X, int Y) First, (int X, int Y) Second, (int X, int Y) End)>
        Curves(string path)
    {
        var start = Regex.Match(path, @"^M(-?\d+) (-?\d+)");
        var at = (X: int.Parse(start.Groups[1].Value), Y: int.Parse(start.Groups[2].Value));
        var curves = new List<((int, int), (int, int), (int, int), (int, int))>();
        foreach (Match curve in Regex.Matches(path, @"c(-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+) (-?\d+)"))
        {
            var by = curve.Groups.Values.Skip(1).Select(group => int.Parse(group.Value)).ToArray();
            var end = (X: at.X + by[4], Y: at.Y + by[5]);
            curves.Add((at, (at.X + by[0], at.Y + by[1]), (at.X + by[2], at.Y + by[3]), end));
            at = end;
        }

        return curves;
    }
}
