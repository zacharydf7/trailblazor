using Microsoft.AspNetCore.Components;

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

    /// <summary>A heading above the message, such as "Upload failed". It is read out with the message.</summary>
    public string? Title { get; init; }

    /// <summary>The icon before the text. Defaults to the tone's: a check for success, a triangle for warning, a
    /// circled cross for danger and a circled "i" for info; primary and neutral have none.</summary>
    public Icon? Icon { get; init; }

    /// <summary>False shows no icon, not even the tone's.</summary>
    public bool ShowIcon { get; init; } = true;

    /// <summary>What the toast shows in place of its message, such as a link or a component. The message is still
    /// what screen readers hear, so keep it saying the same thing.</summary>
    public RenderFragment? Content { get; init; }
}
