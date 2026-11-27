namespace Trailblazor;

// Picks the points worth drawing from a long line, so that a few hundred keep the shape of thousands: the
// largest-triangle-three-buckets method (Steinarsson, 2013). Each bucket keeps the point that makes the biggest
// triangle with the point kept before it and the average of the next bucket's points, which holds on to peaks and
// dips. A gap in the line stays a gap.
internal static class ChartThinning
{
    // The places to draw a line through, in order: every place when there are no more points than `most`, or else
    // about `most` of them shared out among the line's unbroken runs by their length, each run keeping its ends.
    // The first missing place of each gap is kept too, so the line still breaks there.
    public static IReadOnlyList<int> Keep(IReadOnlyList<(double X, double Y)?> points, int most)
    {
        var count = points.Count(point => point is not null);
        if (most <= 0 || count <= most)
        {
            return [.. Enumerable.Range(0, points.Count)];
        }

        var kept = new List<int>(most + 16);
        var place = 0;
        while (place < points.Count)
        {
            if (points[place] is null)
            {
                if (place == 0 || points[place - 1] is not null)
                {
                    kept.Add(place);
                }

                place++;
                continue;
            }

            var end = place;
            while (end + 1 < points.Count && points[end + 1] is not null)
            {
                end++;
            }

            var length = end - place + 1;
            Run(points, place, length, Math.Max(2, (int)Math.Round((double)most * length / count)), kept);
            place = end + 1;
        }

        return kept;
    }

    // Keeps `take` of the run's `length` points from `start`, or all of them when `take` is at least `length`.
    private static void Run(IReadOnlyList<(double X, double Y)?> points, int start, int length, int take,
        List<int> kept)
    {
        if (take >= length)
        {
            kept.AddRange(Enumerable.Range(start, length));
            return;
        }

        (double X, double Y) At(int offset) => points[start + offset]!.Value;
        var every = (length - 2) / (double)(take - 2);
        var chosen = 0;
        kept.Add(start);
        for (var bucket = 0; bucket < take - 2; bucket++)
        {
            var (from, to) = (1 + (int)(bucket * every), Math.Min(length - 1, 1 + (int)((bucket + 1) * every)));
            // The average of the next bucket's points, which after the last bucket is the last point.
            var (nextFrom, nextTo) = (to, Math.Min(length, 1 + (int)((bucket + 2) * every)));
            var (middleX, middleY) = (0d, 0d);
            for (var next = nextFrom; next < nextTo; next++)
            {
                (middleX, middleY) = (middleX + At(next).X, middleY + At(next).Y);
            }

            (middleX, middleY) = (middleX / (nextTo - nextFrom), middleY / (nextTo - nextFrom));
            var (a, biggest, best) = (At(chosen), -1d, from);
            for (var candidate = from; candidate < to; candidate++)
            {
                var (x, y) = At(candidate);
                var area = Math.Abs((a.X - middleX) * (y - a.Y) - (a.X - x) * (middleY - a.Y));
                if (area > biggest)
                {
                    (biggest, best) = (area, candidate);
                }
            }

            kept.Add(start + best);
            chosen = best;
        }

        kept.Add(start + length - 1);
    }
}
