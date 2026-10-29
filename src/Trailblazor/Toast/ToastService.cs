using Microsoft.Extensions.Options;

namespace Trailblazor;

internal sealed class ToastService(IOptions<TrailblazorOptions> trailblazorOptions, IAnnouncer announcer)
    : IToastService
{
    private readonly List<ToastReference> _toasts = [];

    public IReadOnlyList<ToastReference> Toasts => _toasts;

    public ToastSettings Settings => trailblazorOptions.Value.Toasts;

    // Raised when a toast opens, changes or closes, so the hosts re-render.
    public event Action? Changed;

    public ToastReference Show(string message, Tone tone = Tone.Neutral, ToastOptions? options = null)
    {
        options ??= new();
        if (options.Key is { } key && _toasts.Find(toast => toast.Options.Key == key) is { } existing)
        {
            Update(existing, message, tone, options);
            return existing;
        }

        var added = new ToastReference(this, message, tone, options, LifetimeOf(tone, options));
        _toasts.Add(added);
        Announce(message, tone);
        Changed?.Invoke();
        return added;
    }

    public void Update(ToastReference toast, string message, Tone tone, ToastOptions? options)
    {
        if (!_toasts.Contains(toast))
        {
            return;
        }

        options ??= new();
        toast.Set(message, tone, options, LifetimeOf(tone, options));
        Announce(message, tone);
        Changed?.Invoke();
    }

    public void Dismiss(ToastReference toast)
    {
        if (_toasts.Remove(toast))
        {
            Changed?.Invoke();
        }
    }

    private TimeSpan? LifetimeOf(Tone tone, ToastOptions options) => options switch
    {
        { StayOpen: true } => null,
        { Duration: { } duration } => duration,
        _ when tone is Tone.Danger => null,
        _ => Settings.Duration,
    };

    private void Announce(string message, Tone tone)
    {
        if (tone is Tone.Danger or Tone.Warning)
        {
            announcer.Assertive(message);
        }
        else
        {
            announcer.Polite(message);
        }
    }
}
