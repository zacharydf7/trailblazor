namespace Trailblazor;

/// <summary>How a dialog opened through <see cref="IDialogService"/> behaves. Each call passes its own.</summary>
public sealed record DialogOptions
{
    /// <summary>Blocks the rest of the page and keeps focus inside the dialog. False leaves the page usable.
    /// </summary>
    public bool Modal { get; init; } = true;

    /// <summary>Lets Escape cancel the dialog. Left unset, it follows <see cref="DialogSettings.DismissOnEscape"/>.
    /// </summary>
    public bool? DismissOnEscape { get; init; }

    /// <summary>Lets a click outside a modal dialog cancel it. Left unset, it follows
    /// <see cref="DialogSettings.DismissOnBackdrop"/>.</summary>
    public bool? DismissOnBackdrop { get; init; }

    /// <summary>How wide the dialog grows. Defaults to <see cref="DialogSize.Medium"/>.</summary>
    public DialogSize Size { get; init; } = DialogSize.Medium;

    /// <summary>Where the dialog sits in the window. Defaults to <see cref="DialogPosition.Center"/>.</summary>
    public DialogPosition Position { get; init; } = DialogPosition.Center;

    /// <summary>Shows the header with the title. Without it, the title is still the dialog's accessible name.
    /// </summary>
    public bool ShowHeader { get; init; } = true;

    /// <summary>Shows a close button in the header, which cancels the dialog.</summary>
    public bool CloseButton { get; init; } = true;
}
