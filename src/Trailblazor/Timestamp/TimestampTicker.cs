namespace Trailblazor;

// One timer per circuit for every relative timestamp, set for whichever is due to change soonest, rather than a
// timer for each.
internal sealed class TimestampTicker(TimeProvider time) : IDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<object, (DateTimeOffset Due, Action Tick)> _waiting = [];
    private ITimer? _timer;
    private DateTimeOffset? _armedFor;

    // Calls tick once, at the next whole multiple of the wait, so owners with the same wait tick together; the owner
    // asks again for its next change.
    public void Wait(object owner, TimeSpan after, Action tick)
    {
        lock (_gate)
        {
            var now = time.GetUtcNow();
            _waiting[owner] = (now + after - TimeSpan.FromTicks(now.UtcTicks % after.Ticks), tick);
            Arm();
        }
    }

    public void Stop(object owner)
    {
        lock (_gate)
        {
            _waiting.Remove(owner);
        }
    }

    private void Arm()
    {
        if (_waiting.Count == 0)
        {
            return;
        }

        var next = _waiting.Values.Min(waiting => waiting.Due);
        if (_armedFor is { } armed && armed <= next)
        {
            return;
        }

        _timer?.Dispose();
        _armedFor = next;
        var wait = next - time.GetUtcNow();
        _timer = time.CreateTimer(_ => Fire(), null, wait > TimeSpan.Zero ? wait : TimeSpan.Zero,
            Timeout.InfiniteTimeSpan);
    }

    private void Fire()
    {
        List<Action> due;
        lock (_gate)
        {
            var now = time.GetUtcNow();
            var owners = _waiting.Where(waiting => waiting.Value.Due <= now).Select(waiting => waiting.Key).ToList();
            due = [.. owners.Select(owner => _waiting[owner].Tick)];
            owners.ForEach(owner => _waiting.Remove(owner));
            _armedFor = null;
            Arm();
        }

        due.ForEach(tick => tick());
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _waiting.Clear();
        }
    }
}
