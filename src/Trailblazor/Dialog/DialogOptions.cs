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

    /// <summary>What gets focus when the dialog opens. An element marked <c>autofocus</c> takes precedence. Defaults
    /// to <see cref="DialogFocus.FirstField"/>.</summary>
    public DialogFocus InitialFocus { get; init; } = DialogFocus.FirstField;

    /// <summary>Shows the header with the title. Without it, the title is still the dialog's accessible name.
    /// </summary>
    public bool ShowHeader { get; init; } = true;

    /// <summary>Shows a close button in the header, which cancels the dialog.</summary>
    public bool CloseButton { get; init; } = true;

    /// <summary>Shows a button in the header that collapses the dialog to its header, in a tray along the bottom-end
    /// edge. Only a dialog that isn't <see cref="Modal"/> can be minimized, since a modal would still block the
    /// page.</summary>
    public bool Minimizable { get; init; }

    /// <summary>Shows a button in the header that makes the dialog fill the window.</summary>
    public bool Maximizable { get; init; }

    /// <summary>Lets the user move a dialog that isn't <see cref="Modal"/> by dragging its header, or with the arrow
    /// keys on the move button the header gains.</summary>
    public bool Draggable { get; init; }

    /// <summary>Lets the user resize a dialog that isn't <see cref="Modal"/> by dragging the grip in its bottom-end
    /// corner, or with the arrow keys on the grip.</summary>
    public bool Resizable { get; init; }
}
