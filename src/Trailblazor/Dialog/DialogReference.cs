namespace Trailblazor;

/// <summary>A dialog that is open, as listed by <see cref="IDialogService.OpenDialogs"/>.</summary>
public abstract class DialogReference
{
    private readonly Func<DialogReference, bool, object?, Task> _close;
    private string _title;
    private bool _dismissing;

    private protected DialogReference(string title, DialogOptions options, Type? component,
        IReadOnlyDictionary<string, object?> parameters, bool isAlert, Func<DialogReference, bool, object?, Task> close)
    {
        _title = title;
        Options = options;
        Component = component;
        Parameters = parameters;
        IsAlert = isAlert;
        _close = close;
        ShowHeader = options.ShowHeader;
        CloseButton = options.CloseButton;
    }

    /// <summary>The dialog's title, which is also its accessible name. Setting it renames the open dialog.</summary>
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            Changed?.Invoke();
        }
    }

    /// <summary>The options the dialog was opened with, with any left unset filled in from
    /// <see cref="DialogSettings"/>.</summary>
    public DialogOptions Options { get; }

    // Null for a TbDialog written in markup, which renders its own content where it is.
    internal Type? Component { get; }

    internal IReadOnlyDictionary<string, object?> Parameters { get; }

    // An alert dialog asks for a response, and its message, rendered with DescriptionId, describes it.
    internal bool IsAlert { get; }

    // Set while a TbDialogDescription is in the dialog, so the dialog is described by it.
    internal bool Described { get; private set; }

    internal string DescriptionId { get; } = $"tb-dialog-description-{Guid.NewGuid():N}";

    // The dialog can change these while it's open, so they start from the options but live here.
    internal bool ShowHeader { get; private set; }

    internal bool CloseButton { get; private set; }

    internal DialogWindowState WindowState { get; private set; }

    // Asked before the user dismisses the dialog; set by its content.
    internal Func<DialogDismissReason, Task<bool>>? CanDismiss { get; set; }

    internal event Action? Changed;

    internal event Action<IReadOnlyDictionary<string, object?>>? ParametersChanged;

    // Choices from the Move and Resize menus. Each is a command, so choosing the current place again still undoes a
    // drag.
    internal event Action<DialogPosition>? MoveRequested;

    internal event Action<DialogSize>? ResizeRequested;

    /// <summary>Closes the dialog as cancelled, without asking it first. Does nothing once it has closed.</summary>
    public Task CancelAsync() => CloseAsync(false, null);

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

    internal void SetDescribed(bool described)
    {
        Described = described;
        Changed?.Invoke();
    }


    private protected void UpdateParameters(IReadOnlyDictionary<string, object?> parameters) =>
        ParametersChanged?.Invoke(parameters);

    internal void MoveTo(DialogPosition position) => MoveRequested?.Invoke(position);

    internal void ResizeTo(DialogSize size) => ResizeRequested?.Invoke(size);

    // Set by the rendered dialog, so closing first closes the browser's dialog and focus goes back to the opener.
    internal Func<Task>? CloseInBrowserAsync { get; set; }

    internal Task CloseAsync(bool confirmed, object? value) => _close(this, confirmed, value);

    // Set once closing starts, so a second close while the dialog fades out does nothing.
    internal bool Closing { get; set; }

    // A second request while the content is still deciding, such as a double click on the backdrop, is ignored.
    internal async Task DismissAsync(DialogDismissReason reason)
    {
        if (_dismissing)
        {
            return;
        }

        _dismissing = true;
        try
        {
            if (CanDismiss is null || await CanDismiss(reason))
            {
                await CancelAsync();
            }
        }
        finally
        {
            _dismissing = false;
        }
    }

    internal abstract void Complete(bool confirmed, object? value);
}

/// <summary>A dialog that is open and closes with a <typeparamref name="TResult"/>, as
/// <see cref="IDialogService.Show{TDialog, TResult}"/> returns it.</summary>
public sealed class DialogReference<TResult> : DialogReference
{
    private readonly TaskCompletionSource<DialogResult<TResult>> _result = new();

    internal DialogReference(string title, DialogOptions options, Type? component,
        IReadOnlyDictionary<string, object?> parameters, bool isAlert, Func<DialogReference, bool, object?, Task> close)
        : base(title, options, component, parameters, isAlert, close)
    {
    }

    /// <summary>Finishes when the dialog closes, with its value or as cancelled.</summary>
    public Task<DialogResult<TResult>> Result => _result.Task;

    /// <summary>Closes the dialog as confirmed, with <paramref name="value"/> as its result. Does nothing once it has
    /// closed.</summary>
    public Task CloseAsync(TResult value) => CloseAsync(true, value);

    /// <summary>Sets some of the open dialog's parameters, and its content renders again with them.
    /// <typeparamref name="TDialog"/> is the dialog it was opened with.</summary>
    public void Update<TDialog>(Action<DialogParameters<TDialog>> parameters) where TDialog : TbDialogContent<TResult>
    {
        if (Component != typeof(TDialog))
        {
            throw new InvalidOperationException(
                $"This dialog shows {Component?.Name ?? "markup"}, not {typeof(TDialog).Name}.");
        }

        var values = new DialogParameters<TDialog>();
        parameters(values);
        UpdateParameters(values.Values);
    }

    internal override void Complete(bool confirmed, object? value) => _result.SetResult(
        confirmed ? DialogResult<TResult>.Ok((TResult)value!) : DialogResult<TResult>.Cancel());
}
