using System.Collections.Immutable;

namespace Trailblazor.Testing;

/// <summary>Records what Trailblazor's components say to screen readers in bUnit. <c>AddTrailblazor()</c> registers
/// it as the <see cref="IAnnouncer"/>; get it from <c>Services</c> to check that a search said "3 results" or a field
/// read out its error. Messages still reach the live regions as on a page, so markup tests see them too.</summary>
public sealed class FakeAnnouncer : IAnnouncer
{
    private readonly Announcer _announcer;
    // An app can announce off the dispatcher, as toasts from a hub handler do, while a test reads the lists.
    private readonly Lock _lock = new();
    private ImmutableList<string> _polite = [];
    private ImmutableList<string> _assertive = [];

    internal FakeAnnouncer(Announcer announcer) => _announcer = announcer;

    /// <summary>Every polite message, oldest first, including ones since withdrawn or cleared.</summary>
    public IReadOnlyList<string> Polite => _polite;

    /// <summary>Every assertive message, oldest first, including ones since withdrawn or cleared.</summary>
    public IReadOnlyList<string> Assertive => _assertive;

    AnnouncementHandle IAnnouncer.Polite(string message)
    {
        lock (_lock)
        {
            _polite = _polite.Add(message);
        }

        return _announcer.Polite(message);
    }

    AnnouncementHandle IAnnouncer.Assertive(string message)
    {
        lock (_lock)
        {
            _assertive = _assertive.Add(message);
        }

        return _announcer.Assertive(message);
    }

    void IAnnouncer.Withdraw(AnnouncementHandle handle) => _announcer.Withdraw(handle);

    void IAnnouncer.Withdraw(string message) => _announcer.Withdraw(message);

    void IAnnouncer.Clear() => _announcer.Clear();
}
