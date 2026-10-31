using Microsoft.Extensions.Logging;

namespace Trailblazor;

// Tells the developer, once, when something was shown in a session with no host to show it. A page can show a toast
// or a dialog while it loads, before TbRoot's hosts initialize, so the check waits a moment before it warns.
internal sealed class HostCheck(string shown, TimeProvider time, ILogger? logger)
{
    private static readonly TimeSpan Grace = TimeSpan.FromSeconds(1);

    private readonly Lock _lock = new();
    private int _hosts;
    private bool _checking;
    // A prerendered page's hosts attach and are gone again before the check ends, so any attach counts.
    private bool _attached;
    private bool _warned;

    public void Attach()
    {
        lock (_lock)
        {
            _hosts++;
            _attached = true;
        }
    }

    public void Detach()
    {
        lock (_lock)
        {
            _hosts--;
        }
    }

    public void Shown()
    {
        lock (_lock)
        {
            if (_hosts > 0 || _checking || _warned)
            {
                return;
            }

            _checking = true;
            _attached = false;
        }

        ITimer? timer = null;
        timer = time.CreateTimer(_ =>
        {
            timer?.Dispose();
            bool warn;
            lock (_lock)
            {
                _checking = false;
                _warned = warn = _hosts == 0 && !_attached;
            }

            if (warn)
            {
                logger?.LogWarning("{Shown} was shown, but no TbRoot is rendering in this session to show it. Render "
                    + "a TbRoot in the layout, and when interactivity is set page by page, give it the HostRenderMode "
                    + "the pages use.", shown);
            }
        }, null, Grace, Timeout.InfiniteTimeSpan);
    }
}
