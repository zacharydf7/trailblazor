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
        Announce(message, tone, options);
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
        Announce(message, tone, options);
        Changed?.Invoke();
    }

    public void Close(ToastReference toast, ToastCloseReason reason)
    {
        if (_toasts.Remove(toast))
        {
            toast.Finish(reason);
            Changed?.Invoke();
        }
    }

    public void DismissAll()
    {
        if (_toasts.Count == 0)
        {
            return;
        }

        var closing = _toasts.ToList();
        _toasts.Clear();
        closing.ForEach(toast => toast.Finish(ToastCloseReason.App));
        Changed?.Invoke();
    }

    private TimeSpan? LifetimeOf(Tone tone, ToastOptions options) => options switch
    {
        { StayOpen: true } => null,
        { Duration: { } duration } => duration,
        _ when tone is Tone.Danger => null,
        _ => Settings.Duration,
    };

    // The title comes first, as it shows, so "Upload failed. The file is too large." reads as one message.
    private void Announce(string message, Tone tone, ToastOptions options)
    {
        var title = options.Title?.Trim();
        var text = string.IsNullOrEmpty(title) ? message
            : title[^1] is '.' or '!' or '?' ? $"{title} {message}" : $"{title}. {message}";
        if (tone is Tone.Danger or Tone.Warning)
        {
            announcer.Assertive(text);
        }
        else
        {
            announcer.Polite(text);
        }
    }
}
