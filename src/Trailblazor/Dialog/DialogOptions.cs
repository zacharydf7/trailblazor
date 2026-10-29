namespace Trailblazor;

/// <summary>How a dialog opened through <see cref="IDialogService"/> behaves. Each call passes its own.</summary>
public sealed record DialogOptions
{
    /// <summary>Blocks the rest of the page and keeps focus inside the dialog. False leaves the page usable.
    /// </summary>
    public bool Modal { get; init; } = true;

    /// <summary>Lets Escape cancel the dialog.</summary>
    public bool DismissOnEscape { get; init; } = true;

    /// <summary>Lets a click outside a modal dialog cancel it.</summary>
    public bool DismissOnBackdrop { get; init; } = true;

    /// <summary>How wide the dialog grows. Defaults to <see cref="DialogSize.Medium"/>.</summary>
    public DialogSize Size { get; init; } = DialogSize.Medium;

    /// <summary>Shows the header with the title. Without it, the title is still the dialog's accessible name.
    /// </summary>
    public bool ShowHeader { get; init; } = true;

    /// <summary>Shows a close button in the header, which cancels the dialog.</summary>
    public bool CloseButton { get; init; } = true;
}
