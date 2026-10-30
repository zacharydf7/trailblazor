using System.Collections.Immutable;

namespace Trailblazor;

internal sealed class Announcer(DialogService dialogs, TimeProvider time) : IAnnouncer
{
    // Enough to hear messages that arrive together; older ones are dropped, which screen readers don't announce.
    private const int Kept = 5;

    // Long enough for a screen reader to finish speaking; after that, browsing the page finds no stale text.
    private static readonly TimeSpan ClearAfter = TimeSpan.FromSeconds(7);

    // Messages clear on a timer's thread while the hosts render them, so each list is replaced rather than changed.
    private readonly Lock _lock = new();
    private ImmutableList<Announcement> _polite = [];
    private ImmutableList<Announcement> _assertive = [];
    private int _nextId;

    public IReadOnlyList<Announcement> PoliteMessages => _polite;

    public IReadOnlyList<Announcement> AssertiveMessages => _assertive;

    // Raised when a message is added or removed, so the hosts re-render.
    public event Action? Changed;

    public void Polite(string message) => Add(ref _polite, message);

    public void Assertive(string message) => Add(ref _assertive, message);

    public void Clear()
    {
        lock (_lock)
        {
            _polite = [];
            _assertive = [];
        }

        Changed?.Invoke();
    }

    private void Add(ref ImmutableList<Announcement> messages, string message)
    {
        Announcement announcement;
        lock (_lock)
        {
            // Each message gets its own element, so the same text said twice is still announced twice. It goes to the
            // topmost modal open right now, even one whose host hasn't rendered yet.
            announcement = new Announcement(_nextId++, message, dialogs.TopmostModal);
            messages = messages.Add(announcement);
            if (messages.Count > Kept)
            {
                messages = messages.RemoveAt(0);
            }
        }

        ITimer? timer = null;
        timer = time.CreateTimer(_ =>
        {
            Remove(announcement);
            timer?.Dispose();
        }, null, ClearAfter, Timeout.InfiniteTimeSpan);
        Changed?.Invoke();
    }

    // A message already cleared or dropped leaves the hosts as they are.
    private void Remove(Announcement announcement)
    {
        bool removed;
        lock (_lock)
        {
            var (polite, assertive) = (_polite, _assertive);
            _polite = _polite.Remove(announcement);
            _assertive = _assertive.Remove(announcement);
            removed = _polite != polite || _assertive != assertive;
        }

        if (removed)
        {
            Changed?.Invoke();
        }
    }
}

// Dialog is the modal the message was announced into, or null for the page.
internal sealed record Announcement(int Id, string Message, DialogReference? Dialog);
