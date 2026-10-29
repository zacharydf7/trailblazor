namespace Trailblazor;

/// <summary>A dialog that is open, as listed by <see cref="IDialogService.OpenDialogs"/>.</summary>
public sealed class DialogReference
{
    private readonly Func<DialogReference, bool, object?, Task> _close;

    internal DialogReference(string title, DialogOptions options, Type component,
        IReadOnlyDictionary<string, object?> parameters, bool isAlert, Func<DialogReference, bool, object?, Task> close)
    {
        Title = title;
        Options = options;
        Component = component;
        Parameters = parameters;
        IsAlert = isAlert;
        _close = close;
        ShowHeader = options.ShowHeader;
        CloseButton = options.CloseButton;
    }

    /// <summary>The dialog's title, which is also its accessible name.</summary>
    public string Title { get; }

    /// <summary>The options the dialog was opened with, with any left unset filled in from
    /// <see cref="DialogSettings"/>.</summary>
    public DialogOptions Options { get; }

    internal Type Component { get; }

    internal IReadOnlyDictionary<string, object?> Parameters { get; }

    // An alert dialog asks for a response, and its message, rendered with DescriptionId, describes it.
    internal bool IsAlert { get; }

    internal string DescriptionId { get; } = $"tb-dialog-description-{Guid.NewGuid():N}";

    // The dialog can change these while it's open, so they start from the options but live here.
    internal bool ShowHeader { get; private set; }

    internal bool CloseButton { get; private set; }

    internal DialogWindowState WindowState { get; private set; }

    internal event Action? Changed;

    // Choices from the Move and Resize menus. Each is a command, so choosing the current place again still undoes a
    // drag.
    internal event Action<DialogPosition>? MoveRequested;

    internal event Action<DialogSize>? ResizeRequested;

    internal void SetHeader(bool showHeader, bool closeButton)
    {
        ShowHeader = showHeader;
        CloseButton = closeButton;
        // The Restore button goes with the header, so a minimized dialog comes back rather than stay stuck.
        if (!showHeader && WindowState == DialogWindowState.Minimized)
        {
            WindowState = DialogWindowState.Normal;
        }

        Changed?.Invoke();
    }

    internal void SetWindowState(DialogWindowState windowState)
    {
        WindowState = windowState;
        Changed?.Invoke();
    }

    internal void MoveTo(DialogPosition position) => MoveRequested?.Invoke(position);

    internal void ResizeTo(DialogSize size) => ResizeRequested?.Invoke(size);

    // Set by the rendered dialog, so closing first closes the browser's dialog and focus goes back to the opener.
    internal Func<Task>? CloseInBrowserAsync { get; set; }

    internal Task CloseAsync(bool confirmed, object? value) => _close(this, confirmed, value);
}
