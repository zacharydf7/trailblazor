using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A dialog that is open, as listed by <see cref="IDialogService.OpenDialogs"/>.</summary>
public abstract class DialogReference
{
    private readonly Func<DialogReference, bool, object?, Task> _close;
    private string? _title;
    private Func<string>? _defaultTitle;
    private bool _modal;
    private DialogSize _size;
    private DialogPosition _position;
    private DialogWindowState _windowState;
    private bool _dismissing;
    private bool _showHeader;
    private bool _closeButton;
    private TbDialogHeaderActions? _headerActionsOwner;

    private protected DialogReference(string? title, DialogOptions options,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? component,
        IReadOnlyDictionary<string, object?> parameters, bool isAlert, Func<DialogReference, bool, object?, Task> close)
    {
        _title = title;
        Options = options;
        ContentType = component;
        Parameters = parameters;
        IsAlert = isAlert;
        _close = close;
        _showHeader = options.ShowHeader;
        _closeButton = options.CloseButton ?? true;
        _modal = options.Modal;
        _size = options.Size ?? DialogSize.Medium;
        _position = options.Position;
        WindowState = options.WindowState;
    }

    /// <summary>The dialog's title, which is also its accessible name: the one it was opened with, or else its
    /// content's <c>DefaultTitle</c>. A title from <c>DefaultTitle</c> is blank until the content has rendered. Setting
    /// it renames the open dialog. A <see cref="TbDialogTitle"/> in the dialog shows richer content in its place.
    /// </summary>
    public string Title
    {
        get => !string.IsNullOrWhiteSpace(_title) ? _title : _defaultTitle?.Invoke() ?? _title ?? "";
        set
        {
            _title = value;
            Changed?.Invoke();
        }
    }

    /// <summary>The options the dialog was opened with, with any left unset filled in from
    /// <see cref="DialogSettings"/>.</summary>
    public DialogOptions Options { get; }

    /// <summary>Whether the dialog blocks the rest of the page. It starts as <see cref="DialogOptions.Modal"/>, and
    /// setting it switches the open dialog, keeping focus where it was, or moving it into the dialog as it turns
    /// modal. A modal dialog that is minimized leaves the page usable until it is restored.</summary>
    public bool Modal
    {
        get => _modal;
        set => Change(() => _modal = value);
    }

    /// <summary>How wide the dialog is. It starts as <see cref="DialogOptions.Size"/>, and setting it resizes the open
    /// dialog, replacing any size the user dragged it to.</summary>
    public DialogSize Size
    {
        get => _size;
        set
        {
            _size = value;
            ResizeRequested?.Invoke(value);
        }
    }

    /// <summary>Where the dialog sits. It starts as <see cref="DialogOptions.Position"/>, and setting it moves the open
    /// dialog, replacing any place the user dragged it to.</summary>
    public DialogPosition Position
    {
        get => _position;
        set
        {
            _position = value;
            MoveRequested?.Invoke(value);
        }
    }

    /// <summary>Whether the dialog shows at its own size, minimized or maximized, as its header's buttons set it. It
    /// starts as <see cref="DialogOptions.WindowState"/>. Minimizing needs <see cref="DialogOptions.Minimizable"/> and
    /// the header, whose button restores it.</summary>
    public DialogWindowState WindowState
    {
        get => _windowState;
        set
        {
            if (value == DialogWindowState.Minimized && !(Options.Minimizable && ShowHeader))
            {
                throw new InvalidOperationException(
                    $"Only a dialog with {nameof(DialogOptions.Minimizable)} and its header can be minimized.");
            }

            Change(() => _windowState = value);
        }
    }

    /// <summary>Whether the header with the title shows. It starts as <see cref="DialogOptions.ShowHeader"/>, and
    /// setting it changes the open dialog.</summary>
    public bool ShowHeader
    {
        get => _showHeader;
        set => SetHeader(value, _closeButton);
    }

    /// <summary>Whether the header's close button shows. It starts as <see cref="DialogOptions.CloseButton"/>, and
    /// setting it changes the open dialog.</summary>
    public bool CloseButton
    {
        get => _closeButton;
        set => SetHeader(_showHeader, value);
    }

    // A minimized modal leaves the page usable, so it counts as modeless until it is restored.
    internal bool IsBlocking => _modal && _windowState != DialogWindowState.Minimized;

    /// <summary>The content the dialog shows, the <c>TDialog</c> it was opened with, or null for a
    /// <see cref="TbDialog"/> written in markup, which renders its own content where it is.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type? ContentType { get; }

    /// <summary>The content's parameters by name, as the dialog was opened with them and as
    /// <see cref="DialogReference{TResult}.Update{TDialog}"/> has set them since. A confirm or alert has its
    /// <c>Message</c> here.</summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; private set; }

    // An alert dialog asks for a response, and its message, rendered with DescriptionId, describes it.
    internal bool IsAlert { get; }

    // Set while a TbDialogDescription is in the dialog, so the dialog is described by it.
    internal bool Described { get; private set; }

    internal string DescriptionId { get; } = $"tb-dialog-description-{Guid.NewGuid():N}";

    // Called when the user dismisses the dialog, in place of canceling it; set by its content.
    internal Func<DialogDismissReason, Task>? Dismiss { get; set; }

    // Set while a TbDialogTitle is in the dialog, so the header shows it in place of the title's text.
    internal RenderFragment? TitleContent { get; private set; }

    // Set while a TbDialogHeaderActions is in the dialog, so the header shows it after the title.
    internal RenderFragment? HeaderActions { get; private set; }

    /// <summary>Raised with the new state each time <see cref="WindowState"/> changes.</summary>
    public event Action<DialogWindowState>? WindowStateChanged;

    internal event Action? Changed;

    // Only the header listens, since a frame-wide change would draw a markup dialog's TbDialogTitle again, endlessly.
    internal event Action? HeaderContentChanged;

    // Raised when the dialog starts or stops blocking the page, so toasts and announcements move with it.
    internal event Action? BlockingChanged;

    internal event Action<IReadOnlyDictionary<string, object?>>? ParametersChanged;

    // Each place or size set is a command, so setting the current one again still undoes a drag.
    internal event Action<DialogPosition>? MoveRequested;

    internal event Action<DialogSize>? ResizeRequested;

    /// <summary>Closes the dialog as canceled, without asking it first. Does nothing once it has closed.</summary>
    public Task CancelAsync() => CloseAsync(false, null);

    /// <summary>Closes the dialog as confirmed, for a dialog with no result, such as one that only informs. Does
    /// nothing once it has closed. Throws for a dialog whose result isn't a <see cref="bool"/>, which closes with
    /// <see cref="DialogReference{TResult}.CloseAsync"/> instead.</summary>
    public Task ConfirmAsync() => this is DialogReference<bool> ? CloseAsync(true, true)
        : throw new InvalidOperationException(
            "ConfirmAsync is only for a dialog with no result; close this one with CloseAsync(value).");

    private void SetHeader(bool showHeader, bool closeButton)
    {
        _showHeader = showHeader;
        _closeButton = closeButton;
        // The Restore button goes with the header, so a minimized dialog comes back rather than stay stuck.
        Change(() =>
        {
            if (!showHeader && _windowState == DialogWindowState.Minimized)
            {
                _windowState = DialogWindowState.Normal;
            }
        });
    }

    private void Change(Action change)
    {
        var wasBlocking = IsBlocking;
        var windowState = _windowState;
        change();
        Changed?.Invoke();
        if (IsBlocking != wasBlocking)
        {
            BlockingChanged?.Invoke();
        }

        if (_windowState != windowState)
        {
            WindowStateChanged?.Invoke(_windowState);
        }
    }

    // False until the content has rendered, since DefaultTitle may still fill in a blank title.
    internal bool Unnamed => _defaultTitle is not null && string.IsNullOrWhiteSpace(Title);

    internal void SetDefaultTitle(Func<string> title)
    {
        _defaultTitle = title;
        if (string.IsNullOrWhiteSpace(_title))
        {
            HeaderContentChanged?.Invoke();
        }
    }

    internal void SetTitleContent(RenderFragment? content)
    {
        TitleContent = content;
        HeaderContentChanged?.Invoke();
    }

    internal void SetHeaderActions(TbDialogHeaderActions owner, RenderFragment? content)
    {
        _headerActionsOwner = owner;
        HeaderActions = content;
        HeaderContentChanged?.Invoke();
    }

    // An instance swapped in sets its actions before the one it replaces is disposed, so only the latest clears them.
    internal void ClearHeaderActions(TbDialogHeaderActions owner)
    {
        if (_headerActionsOwner == owner)
        {
            _headerActionsOwner = null;
            HeaderActions = null;
            HeaderContentChanged?.Invoke();
        }
    }

    internal void SetDescribed(bool described)
    {
        Described = described;
        Changed?.Invoke();
    }

    private protected void UpdateParameters(IReadOnlyDictionary<string, object?> parameters)
    {
        var merged = new Dictionary<string, object?>(Parameters);
        foreach (var (name, value) in parameters)
        {
            merged[name] = value;
        }

        Parameters = merged;
        ParametersChanged?.Invoke(parameters);
    }

    // Set by the rendered dialog, so closing first closes the browser's dialog and focus goes back to the opener.
    internal Func<Task>? CloseInBrowserAsync { get; set; }

    internal Task CloseAsync(bool confirmed, object? value) => _close(this, confirmed, value);

    /// <summary>Whether the dialog has started to close. A closing dialog stays in
    /// <see cref="IDialogService.OpenDialogs"/> while it fades out, until its result is set, so leave out the closing
    /// ones to find the dialogs still open.</summary>
    public bool IsClosing { get; internal set; }

    // A second request while the content is still deciding, such as a double click on the backdrop, is ignored.
    internal async Task DismissAsync(DialogDismissReason reason)
    {
        if (_dismissing)
        {
            return;
        }

        _dismissing = true;
        DismissedBy = reason;
        try
        {
            await (Dismiss is null ? CancelAsync() : Dismiss(reason));
        }
        finally
        {
            _dismissing = false;
            // A dialog the content kept open isn't counted as dismissed if it closes some other way later.
            if (!IsClosing)
            {
                DismissedBy = null;
            }
        }
    }

    internal abstract void Complete(bool confirmed, object? value);

    // How the user dismissed it, if they did, and when it opened, for its telemetry.
    internal DialogDismissReason? DismissedBy { get; private set; }

    internal long OpenedAt { get; init; }
}

/// <summary>A dialog that is open and closes with a <typeparamref name="TResult"/>, as
/// <see cref="IDialogService.Show{TDialog, TResult}"/> returns it.</summary>
public sealed class DialogReference<TResult> : DialogReference
{
    private readonly TaskCompletionSource<DialogResult<TResult>> _result = new();

    internal DialogReference(string? title, DialogOptions options,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? component,
        IReadOnlyDictionary<string, object?> parameters, bool isAlert, Func<DialogReference, bool, object?, Task> close)
        : base(title, options, component, parameters, isAlert, close)
    {
    }

    /// <summary>Finishes when the dialog closes, with its value or as canceled.</summary>
    public Task<DialogResult<TResult>> Result => _result.Task;

    /// <summary>Closes the dialog as confirmed, with <paramref name="value"/> as its result. Does nothing once it has
    /// closed.</summary>
    public Task CloseAsync(TResult value) => CloseAsync(true, value);

    /// <summary>Sets some of the open dialog's parameters, and its content renders again with them.
    /// <typeparamref name="TDialog"/> is the dialog it was opened with.</summary>
    public void Update<TDialog>(Action<DialogParameters<TDialog>> parameters) where TDialog : TbDialogContent<TResult>
    {
        if (ContentType != typeof(TDialog))
        {
            throw new InvalidOperationException(
                $"This dialog shows {ContentType?.Name ?? "markup"}, not {typeof(TDialog).Name}.");
        }

        var values = new DialogParameters<TDialog>();
        parameters(values);
        UpdateParameters(values.Values);
    }

    internal override void Complete(bool confirmed, object? value) => _result.SetResult(
        confirmed ? DialogResult<TResult>.Ok((TResult)value!) : DialogResult<TResult>.Cancel());
}
