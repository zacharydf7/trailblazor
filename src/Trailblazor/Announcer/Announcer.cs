namespace Trailblazor;

internal sealed class Announcer(DialogService dialogs) : IAnnouncer
{
    // Enough to hear messages that arrive together; older ones are dropped, which screen readers don't announce.
    private const int Kept = 5;

    private readonly List<Announcement> _polite = [];
    private readonly List<Announcement> _assertive = [];
    private int _nextId;

    public IReadOnlyList<Announcement> PoliteMessages => _polite;

    public IReadOnlyList<Announcement> AssertiveMessages => _assertive;

    // Raised when a message is added, so the hosts re-render.
    public event Action? Changed;

    public void Polite(string message) => Add(_polite, message);

    public void Assertive(string message) => Add(_assertive, message);

    private void Add(List<Announcement> messages, string message)
    {
        // Each message gets its own element, so the same text said twice is still announced twice. It goes to the
        // topmost modal open right now, even one whose host hasn't rendered yet.
        messages.Add(new Announcement(_nextId++, message, dialogs.TopmostModal));
        if (messages.Count > Kept)
        {
            messages.RemoveAt(0);
        }

        Changed?.Invoke();
    }
}

// Dialog is the modal the message was announced into, or null for the page.
internal sealed record Announcement(int Id, string Message, DialogReference? Dialog);
