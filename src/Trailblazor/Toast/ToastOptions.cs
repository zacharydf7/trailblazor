namespace Trailblazor;

/// <summary>How one toast behaves. Each call passes its own.</summary>
public sealed record ToastOptions
{
    /// <summary>How long the toast stays. Defaults to the app-wide duration, or until dismissed for an error.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Keeps the toast until it's dismissed or updated, for messages such as "Uploading…".</summary>
    public bool StayOpen { get; init; }

    /// <summary>Identifies the message. Showing a toast with the key of one that's still open updates that
    /// toast instead of adding another.</summary>
    public string? Key { get; init; }

    /// <summary>A button on the toast, such as "Undo".</summary>
    public ToastAction? Action { get; init; }
}
