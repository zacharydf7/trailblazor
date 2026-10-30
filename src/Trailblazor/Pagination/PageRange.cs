namespace Trailblazor;

// Which page buttons a pagination shows: the boundary pages at each end, the current page with its siblings, and a
// gap (null) where pages are left out. A gap of a single page shows that page instead, so the count stays the same.
internal static class PageRange
{
    public static IReadOnlyList<int?> For(int page, int pageCount, int siblingCount, int boundaryCount)
    {
        var start = Enumerable.Range(1, Math.Min(boundaryCount, pageCount));
        var endFrom = Math.Max(pageCount - boundaryCount + 1, boundaryCount + 1);
        var end = Enumerable.Range(endFrom, Math.Max(pageCount - endFrom + 1, 0));
        var siblingsFrom = Math.Max(Math.Min(page - siblingCount, pageCount - boundaryCount - siblingCount * 2 - 1),
            boundaryCount + 2);
        var siblingsTo = Math.Min(Math.Max(page + siblingCount, boundaryCount + siblingCount * 2 + 2),
            end.Any() ? end.First() - 2 : pageCount - 1);

        var pages = new List<int?>(start.Select(number => (int?)number));
        if (siblingsFrom > boundaryCount + 2)
        {
            pages.Add(null);
        }
        else if (boundaryCount + 1 < pageCount - boundaryCount)
        {
            pages.Add(boundaryCount + 1);
        }

        var siblings = Enumerable.Range(siblingsFrom, Math.Max(siblingsTo - siblingsFrom + 1, 0));
        pages.AddRange(siblings.Select(number => (int?)number));
        if (siblingsTo < pageCount - boundaryCount - 1)
        {
            pages.Add(null);
        }
        else if (pageCount - boundaryCount > boundaryCount)
        {
            pages.Add(pageCount - boundaryCount);
        }

        pages.AddRange(end.Select(number => (int?)number));
        return pages;
    }
}
