using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Trailblazor;

internal sealed class ToastService(IOptions<TrailblazorOptions> trailblazorOptions, IAnnouncer announcer,
    ITbLocalizer localizer, TimeProvider time, ILogger<ToastService>? logger = null) : IToastService
{
    // An app can show toasts off the dispatcher, such as from a hub handler, while the hosts render them, so the list
    // is replaced rather than changed.
    private readonly Lock _lock = new();
    private ImmutableList<ToastReference> _toasts = [];

    public IReadOnlyList<ToastReference> Toasts => _toasts;

    internal HostCheck Hosts { get; } = new("A toast", time, logger);

    public ToastSettings Settings => trailblazorOptions.Value.Toasts;

    // Raised when a toast opens, changes or closes, so the hosts re-render.
    public event Action? Changed;

    public ToastReference Show(string message, Tone tone = Tone.Neutral, ToastOptions? options = null)
    {
        options ??= new();
        ToastReference toast;
        bool added;
        lock (_lock)
        {
            var existing = options.Key is { } key ? _toasts.Find(open => open.Options.Key == key) : null;
            toast = existing ?? new ToastReference(this, message, tone, options, LifetimeOf(tone, options));
            added = existing is null;
            if (added)
            {
                _toasts = _toasts.Add(toast);
            }
        }

        if (!added)
        {
            Update(toast, message, tone, options);
            return toast;
        }

        Hosts.Shown();
        Announce(message, tone, options);
        Changed?.Invoke();
        return toast;
    }

    public void Update(ToastReference toast, string message, Tone tone, ToastOptions? options)
    {
        options ??= new();
        lock (_lock)
        {
            if (!_toasts.Contains(toast))
            {
                return;
            }

            toast.Set(message, tone, options, LifetimeOf(tone, options));
        }

        Announce(message, tone, options);
        Changed?.Invoke();
    }

    public void Close(ToastReference toast, ToastCloseReason reason)
    {
        bool removed;
        lock (_lock)
        {
            var before = _toasts;
            _toasts = _toasts.Remove(toast);
            removed = _toasts != before;
        }

        if (removed)
        {
            toast.Finish(reason);
            Changed?.Invoke();
        }
    }

    public void DismissAll()
    {
        ImmutableList<ToastReference> closing;
        lock (_lock)
        {
            (closing, _toasts) = (_toasts, []);
        }

        if (closing.Count == 0)
        {
            return;
        }

        closing.ForEach(toast => toast.Finish(ToastCloseReason.App));
        Changed?.Invoke();
    }

    private TimeSpan? LifetimeOf(Tone tone, ToastOptions options) => options switch
    {
        { StayOpen: true } => null,
        { Duration: { } duration } => duration,
        // An action mustn't time out before the user reaches it (WCAG 2.2.1).
        { Action: not null } => null,
        _ when tone is Tone.Danger => null,
        _ => Settings.Duration,
    };

    // The title comes first, as it shows, so "Upload failed. The file is too large." reads as one message. An action
    // is named after it, with the key that reaches it.
    private void Announce(string message, Tone tone, ToastOptions options)
    {
        var text = Sentences(options.Title?.Trim(), message);
        if (options.Action is { } action)
        {
            text = Sentences(text, string.Format(CultureInfo.CurrentCulture, localizer[TbStrings.ToastActionAvailable],
                action.Text));
        }

        if (tone is Tone.Danger or Tone.Warning)
        {
            announcer.Assertive(text);
        }
        else
        {
            announcer.Polite(text);
        }
    }

    private static string Sentences(string? first, string second) => string.IsNullOrEmpty(first) ? second
        : first[^1] is '.' or '!' or '?' ? $"{first} {second}" : $"{first}. {second}";
}
