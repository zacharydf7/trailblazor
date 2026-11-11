namespace Trailblazor;

/// <summary>How a dialog opened through <see cref="IDialogService"/> behaves. Each call passes its own.</summary>
public sealed record DialogOptions
{
    /// <summary>Blocks the rest of the page and keeps focus inside the dialog. False leaves the page usable. An open
    /// dialog can change it through <see cref="DialogReference.Modal"/>.</summary>
    public bool Modal { get; init; } = true;

    /// <summary>Lets Escape cancel the dialog. Left unset, it follows <see cref="DialogSettings.DismissOnEscape"/>.
    /// </summary>
    public bool? DismissOnEscape { get; init; }

    /// <summary>Lets a click outside a modal dialog cancel it. Left unset, it follows
    /// <see cref="DialogSettings.DismissOnBackdrop"/>.</summary>
    public bool? DismissOnBackdrop { get; init; }

    /// <summary>Opens the dialog in the browser's top layer, above everything on the page. False keeps it on the page,
    /// below popups that other libraries add to the page's body, such as an editor's menus or a date picker, so they
    /// can be used from inside it. Left unset, it follows <see cref="DialogSettings.TopLayer"/>.</summary>
    public bool? TopLayer { get; init; }

    /// <summary>How wide the dialog grows. Left unset, it follows <see cref="DialogSettings.Size"/>, which defaults to
    /// <see cref="DialogSize.Medium"/>.</summary>
    public DialogSize? Size { get; init; }

    /// <summary>The widest the dialog grows, as a CSS length such as <c>40rem</c>, for a width between the steps of
    /// <see cref="Size"/>. It takes the place of the size, except with <see cref="DialogSize.Fit"/>, where it caps the
    /// content's width. The dialog is still never wider than the window, and a size set later replaces it.</summary>
    public string? Width { get; init; }

    /// <summary>Where the dialog sits in the window. Defaults to <see cref="DialogPosition.Center"/>.</summary>
    public DialogPosition Position { get; init; } = DialogPosition.Center;

    /// <summary>What gets focus when the dialog opens. An element marked <c>autofocus</c> takes precedence. Defaults
    /// to <see cref="DialogFocus.FirstField"/>.</summary>
    public DialogFocus InitialFocus { get; init; } = DialogFocus.FirstField;

    /// <summary>Shows the header with the title. Without it, the title is still the dialog's accessible name.
    /// </summary>
    public bool ShowHeader { get; init; } = true;

    /// <summary>Shows a close button in the header, which cancels the dialog. Left unset, it follows
    /// <see cref="DialogSettings.CloseButton"/>.</summary>
    public bool? CloseButton { get; init; }

    /// <summary>Shows a button in the header that collapses the dialog to its header, in a tray along the bottom-end
    /// edge. A <see cref="Modal"/> dialog leaves the page usable while it is minimized, and blocks it again once
    /// restored.</summary>
    public bool Minimizable { get; init; }

    /// <summary>Shows a button in the header that makes the dialog fill the window.</summary>
    public bool Maximizable { get; init; }

    /// <summary>Lets the user move a dialog that isn't <see cref="Modal"/> by dragging its header, or with the arrow
    /// keys on the move button the header gains.</summary>
    public bool Draggable { get; init; }

    /// <summary>Lets the user resize a dialog that isn't <see cref="Modal"/> by dragging the grip in its bottom-end
    /// corner, or with the arrow keys on the grip.</summary>
    public bool Resizable { get; init; }

    /// <summary>Extra CSS classes for the dialog and its inner parts.</summary>
    public DialogClasses? Classes { get; init; }

    /// <summary>A label the dialog's use is counted under, such as <c>"delete-trail"</c>, through the
    /// <c>Trailblazor</c> meter: shown each time it opens, and counted in <c>trailblazor.dialog.closed</c> and
    /// <c>trailblazor.dialog.duration</c> with how it closed: <c>confirmed</c>, <c>cancelled</c>, <c>escape</c>,
    /// <c>backdrop</c> or <c>close_button</c>. Its title and result are never recorded. It names the dialog, never the
    /// user.</summary>
    public string? TelemetryName { get; init; }
}
