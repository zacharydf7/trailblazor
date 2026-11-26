using System.Text;

namespace Trailblazor;

// SVG paths for a line through a series' points, and for the area under one. Points are shares of the plot, x from
// the left and y from the top, and the path is drawn on a grid of 10,000 by 10,000 that the plot stretches. Each
// point is written as a whole-number offset from the one before, which takes about half the text of full positions.
internal static class ChartPath
{
    public const int Grid = 10000;

    // A missing point (null) leaves a gap: the line stops before it and starts again after it.
    public static string Line(IReadOnlyList<(double X, double Y)?> points, ChartCurve curve)
    {
        var path = new StringBuilder();
        foreach (var (start, end) in Runs(points))
        {
            var run = OnGrid(points, start, end);
            path.Append('M').Append(run[0].X).Append(' ').Append(run[0].Y);
            if (run.Length == 1)
            {
                // A point with a gap on both sides: a line of no length, which a round line end draws as a dot.
                path.Append("h0");
            }
            else
            {
                Forward(path, run, curve);
            }
        }

        return path.ToString();
    }

    // The shape between a line and its floor: along the line, down to the floor at the last point, and back along the
    // floor, which a stacked area shares with the one under it when the two have the same curve. The floor has a
    // point wherever the line has one. Each run between gaps is a shape of its own, and a point with a gap on both
    // sides has no width to fill.
    public static string Area(IReadOnlyList<(double X, double Y)?> line, IReadOnlyList<(double X, double Y)?> floor,
        ChartCurve curve)
    {
        var path = new StringBuilder();
        foreach (var (start, end) in Runs(line))
        {
            if (end == start)
            {
                continue;
            }

            var (over, under) = (OnGrid(line, start, end), OnGrid(floor, start, end));
            path.Append('M').Append(over[0].X).Append(' ').Append(over[0].Y);
            Forward(path, over, curve);
            path.Append('v').Append(under[^1].Y - over[^1].Y);
            Backward(path, under, curve);
            path.Append('z');
        }

        return path.ToString();
    }

    // Where each unbroken run of points starts and ends.
    private static IEnumerable<(int Start, int End)> Runs(IReadOnlyList<(double X, double Y)?> points)
    {
        var start = 0;
        while (start < points.Count)
        {
            if (points[start] is null)
            {
                start++;
                continue;
            }

            var end = start;
            while (end + 1 < points.Count && points[end + 1] is not null)
            {
                end++;
            }

            yield return (start, end);
            start = end + 1;
        }
    }

    private static (int X, int Y)[] OnGrid(IReadOnlyList<(double X, double Y)?> points, int start, int end)
    {
        var run = new (int X, int Y)[end - start + 1];
        for (var index = start; index <= end; index++)
        {
            var (x, y) = points[index]!.Value;
            run[index - start] = ((int)Math.Round(x * Grid), (int)Math.Round(y * Grid));
        }

        return run;
    }

    // From the run's first point, which the path is already at, to its last.
    private static void Forward(StringBuilder path, (int X, int Y)[] run, ChartCurve curve)
    {
        var slopes = curve == ChartCurve.Smooth ? Slopes(run) : [];
        for (var index = 1; index < run.Length; index++)
        {
            var (fromX, fromY) = run[index - 1];
            var (toX, toY) = run[index];
            switch (curve)
            {
                case ChartCurve.Stepped:
                    path.Append('h').Append(toX - fromX).Append('v').Append(toY - fromY);
                    break;
                case ChartCurve.Smooth:
                    // A cubic curve whose handles reach a third of the way along each end's slope.
                    var third = (toX - fromX) / 3d;
                    path.Append('c').Append(Whole(third)).Append(' ').Append(Whole(slopes[index - 1] * third))
                        .Append(' ').Append(Whole(toX - fromX - third)).Append(' ')
                        .Append(Whole(toY - fromY - slopes[index] * third)).Append(' ').Append(toX - fromX)
                        .Append(' ').Append(toY - fromY);
                    break;
                default:
                    path.Append('l').Append(toX - fromX).Append(' ').Append(toY - fromY);
                    break;
            }
        }
    }

    // From the run's last point, which the path is already at, back to its first, along the same line Forward draws.
    private static void Backward(StringBuilder path, (int X, int Y)[] run, ChartCurve curve)
    {
        var slopes = curve == ChartCurve.Smooth ? Slopes(run) : [];
        for (var index = run.Length - 1; index > 0; index--)
        {
            var (fromX, fromY) = run[index];
            var (toX, toY) = run[index - 1];
            switch (curve)
            {
                case ChartCurve.Stepped:
                    path.Append('v').Append(toY - fromY).Append('h').Append(toX - fromX);
                    break;
                case ChartCurve.Smooth:
                    // The curve Forward draws, with its handles in the other order.
                    var third = (toX - fromX) / 3d;
                    path.Append('c').Append(Whole(third)).Append(' ').Append(Whole(slopes[index] * third))
                        .Append(' ').Append(Whole(toX - fromX - third)).Append(' ')
                        .Append(Whole(toY - fromY - slopes[index - 1] * third)).Append(' ').Append(toX - fromX)
                        .Append(' ').Append(toY - fromY);
                    break;
                default:
                    path.Append('l').Append(toX - fromX).Append(' ').Append(toY - fromY);
                    break;
            }
        }
    }

    private static int Whole(double value) => (int)Math.Round(value);

    // The slope at each point for a curve that never rises above or dips below the points it joins (Fritsch and
    // Carlson, 1980): the mean of the slopes on either side, flat at a peak or a dip, and held back where it would
    // overshoot.
    private static double[] Slopes((int X, int Y)[] run)
    {
        var between = new double[run.Length - 1];
        for (var index = 0; index < between.Length; index++)
        {
            double width = run[index + 1].X - run[index].X;
            between[index] = width == 0 ? 0 : (run[index + 1].Y - run[index].Y) / width;
        }

        var slopes = new double[run.Length];
        slopes[0] = between[0];
        slopes[^1] = between[^1];
        for (var index = 1; index < between.Length; index++)
        {
            slopes[index] = between[index - 1] * between[index] <= 0 ? 0 : (between[index - 1] + between[index]) / 2;
        }

        for (var index = 0; index < between.Length; index++)
        {
            if (between[index] == 0)
            {
                slopes[index] = slopes[index + 1] = 0;
                continue;
            }

            var before = slopes[index] / between[index];
            var after = slopes[index + 1] / between[index];
            var reach = Math.Sqrt(before * before + after * after);
            if (reach > 3)
            {
                slopes[index] = 3 / reach * before * between[index];
                slopes[index + 1] = 3 / reach * after * between[index];
            }
        }

        return slopes;
    }
}
