using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Trailblazor;

internal sealed class DialogService(IOptions<TrailblazorOptions> trailblazorOptions, ITbLocalizer localizer,
    TimeProvider time, ILogger<DialogService>? logger = null) : IDialogService
{
    // An app can open dialogs off the dispatcher, such as from a hub handler, while the host renders them, so the list
    // is replaced rather than changed.
    private readonly Lock _lock = new();
    private ImmutableList<DialogReference> _open = [];

    public IReadOnlyList<DialogReference> OpenDialogs => _open;

    internal HostCheck Hosts { get; } = new("A dialog", time, logger);

    // A modal makes the rest of the page inert, so toasts and announcements go inside the topmost one.
    public DialogReference? TopmostModal => _open.LastOrDefault(dialog => dialog.Options.Modal);

    // Raised when a dialog opens or closes, so the host in TbRoot re-renders.
    public event Action? Changed;

    public DialogReference<TResult> Show<TDialog, TResult>(string title,
        Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult>
    {
        var values = new DialogParameters<TDialog>();
        parameters?.Invoke(values);
        return Open<TResult>(title, typeof(TDialog), values.Values, options ?? new(), isAlert: false);
    }

    public Task<DialogResult<TResult>> ShowAsync<TDialog, TResult>(string title,
        Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult> => Show<TDialog, TResult>(title, parameters, options).Result;

    public async Task<bool> ConfirmAsync(string title, string message, string? confirmText = null,
        string? cancelText = null, Tone tone = Tone.Primary)
    {
        var parameters = new Dictionary<string, object?>
        {
            [nameof(ConfirmDialog.Message)] = message,
            [nameof(ConfirmDialog.ConfirmText)] = confirmText ?? localizer[TbStrings.DialogConfirm],
            [nameof(ConfirmDialog.CancelText)] = cancelText ?? localizer[TbStrings.DialogCancel],
            [nameof(ConfirmDialog.Tone)] = tone,
        };
        var dialog = Open<bool>(title, typeof(ConfirmDialog), parameters, new() { Size = DialogSize.Small },
            isAlert: true);
        return (await dialog.Result).Confirmed;
    }

    public async Task AlertAsync(string title, string message, string? okText = null, Tone tone = Tone.Info)
    {
        var parameters = new Dictionary<string, object?>
        {
            [nameof(ConfirmDialog.Message)] = message,
            [nameof(ConfirmDialog.ConfirmText)] = okText ?? localizer[TbStrings.DialogConfirm],
            [nameof(ConfirmDialog.Tone)] = tone,
            [nameof(ConfirmDialog.ShowIcon)] = true,
        };
        await Open<bool>(title, typeof(ConfirmDialog), parameters, new() { Size = DialogSize.Small }, isAlert: true)
            .Result;
    }

    public async Task CloseAllAsync()
    {
        foreach (var dialog in Enumerable.Reverse(_open))
        {
            await dialog.CancelAsync();
        }
    }

    // A TbDialog in markup renders its own frame where it is, so the host leaves it out.
    internal DialogReference<bool> OpenInPlace(string title, DialogOptions options) =>
        Open<bool>(title, null, new Dictionary<string, object?>(), options, isAlert: false);

    private DialogReference<TResult> Open<TResult>(string title, Type? component,
        IReadOnlyDictionary<string, object?> parameters, DialogOptions options, bool isAlert)
    {
        var settings = trailblazorOptions.Value.Dialogs;
        options = options with
        {
            DismissOnEscape = options.DismissOnEscape ?? settings.DismissOnEscape,
            DismissOnBackdrop = options.DismissOnBackdrop ?? settings.DismissOnBackdrop,
        };
        var dialog = new DialogReference<TResult>(title, options, component, parameters, isAlert, CloseAsync)
        {
            OpenedAt = time.GetTimestamp(),
        };
        lock (_lock)
        {
            _open = _open.Add(dialog);
        }

        // A TbDialog in markup renders itself, so only the dialogs the host renders need one.
        if (component is not null)
        {
            Hosts.Shown();
        }

        Changed?.Invoke();
        return dialog;
    }

    // SPIKE (#101): a dialog is named by its content's type, never its title, which can hold the user's data.
    private void Record(DialogReference dialog, bool confirmed)
    {
        var name = dialog.Component?.Name ?? "markup";
        var outcome = confirmed ? "confirmed" : dialog.DismissedBy switch
        {
            DialogDismissReason.Escape => "escape",
            DialogDismissReason.Backdrop => "backdrop",
            DialogDismissReason.CloseButton => "close_button",
            _ => "cancelled",
        };
        KeyValuePair<string, object?>[] tags = [new("tb.dialog.name", name), new("tb.dialog.outcome", outcome)];
        TrailblazorTelemetry.DialogsClosed.Add(1, tags);
        TrailblazorTelemetry.DialogDuration.Record(time.GetElapsedTime(dialog.OpenedAt).TotalSeconds, tags);
    }

    private async Task CloseAsync(DialogReference dialog, bool confirmed, object? value)
    {
        lock (_lock)
        {
            if (!_open.Contains(dialog) || dialog.Closing)
            {
                return;
            }

            dialog.Closing = true;
        }

        try
        {
            if (dialog.CloseInBrowserAsync is { } closeInBrowser)
            {
                await closeInBrowser();
            }
        }
        finally
        {
            lock (_lock)
            {
                _open = _open.Remove(dialog);
            }

            Changed?.Invoke();
            Record(dialog, confirmed);
            dialog.Complete(confirmed, value);
        }
    }
}
