namespace Trailblazor;

// How far a provider that doesn't count its items has been read: the furthest item reached, and whether a page has said
// no more follow. Until one has, a virtualized view is given room for a page past the items reached.
internal sealed class UncountedReach
{
    public int Reached { get; private set; }

    public bool Ended { get; private set; }

    public void Reset() => (Reached, Ended) = (0, false);

    public void Set(int reached, bool ended) => (Reached, Ended) = (reached, ended);

    public void Take(int start, int count, bool hasMore)
    {
        // A page that comes back empty ends the items where it starts, unless it starts past the items known, as after a
        // filter while scrolled down.
        if (count == 0 && start > 0)
        {
            if (start <= Reached)
            {
                (Reached, Ended) = (start, true);
            }
        }
        else if (hasMore)
        {
            // A page that reaches past the items known, as after items were added, takes back an end learned before.
            if (start + count > Reached)
            {
                (Reached, Ended) = (start + count, false);
            }
        }
        else
        {
            (Reached, Ended) = (start + count, true);
        }
    }

    public int Room(int page) => Ended ? Reached : Reached + Math.Max(page, 1);
}
