namespace Trailblazor;

/// <summary>A toast shown through <see cref="IToastService"/>.</summary>
public sealed class ToastReference
{
    private readonly ToastService _service;
    private readonly TaskCompletionSource<ToastCloseReason> _closed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal ToastReference(ToastService service, string message, Tone tone, ToastOptions options,
        TimeSpan? lifetime)
    {
        _service = service;
        Message = message;
        Tone = tone;
        Options = options;
        Lifetime = lifetime;
    }

    /// <summary>The message.</summary>
    public string Message { get; private set; }

    /// <summary>The message's meaning, which sets its color.</summary>
    public Tone Tone { get; private set; }

    /// <summary>The options it was shown or last updated with.</summary>
    public ToastOptions Options { get; private set; }

    /// <summary>True until the toast closes.</summary>
    public bool IsOpen => _service.Toasts.Contains(this);

    /// <summary>Finishes when the toast closes, saying why: its time ran out, the user closed it or pressed its
    /// action, or the app dismissed it.</summary>
    public Task<ToastCloseReason> Closed => _closed.Task;

    // How long the toast stays from when it shows; null keeps it until dismissed.
    internal TimeSpan? Lifetime { get; private set; }

    // Changes whenever the content does, so the toast restarts its timer.
    internal int Version { get; private set; }

    /// <summary>Replaces the message, tone and options, and restarts the timer, as if shown again. Does nothing
    /// once the toast has closed.</summary>
    public void Update(string message, Tone tone, ToastOptions? options = null) =>
        _service.Update(this, message, tone, options);

    /// <summary>Closes the toast, as <see cref="ToastCloseReason.App"/>. Does nothing once it has closed.
    /// </summary>
    public void Dismiss() => _service.Close(this, ToastCloseReason.App);

    internal void Finish(ToastCloseReason reason) => _closed.TrySetResult(reason);

    internal void Set(string message, Tone tone, ToastOptions options, TimeSpan? lifetime)
    {
        Message = message;
        Tone = tone;
        Options = options;
        Lifetime = lifetime;
        Version++;
    }
}
