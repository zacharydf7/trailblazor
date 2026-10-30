using Microsoft.Extensions.Options;

namespace Trailblazor;

internal sealed class DialogService(IOptions<TrailblazorOptions> trailblazorOptions, ITbLocalizer localizer)
    : IDialogService
{
    private readonly List<DialogReference> _open = [];
    private readonly Dictionary<DialogReference, Action<bool, object?>> _completions = [];

    public IReadOnlyList<DialogReference> OpenDialogs => _open;

    // A modal makes the rest of the page inert, so toasts and announcements go inside the topmost one.
    public DialogReference? TopmostModal => _open.LastOrDefault(dialog => dialog.Options.Modal);

    // Raised when a dialog opens or closes, so the host in TbRoot re-renders.
    public event Action? Changed;

    public Task<DialogResult<TResult>> ShowAsync<TDialog, TResult>(string title,
        Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialog<TResult>
    {
        var values = new DialogParameters<TDialog>();
        parameters?.Invoke(values);
        return Open<TResult>(title, typeof(TDialog), values.Values, options ?? new(), isAlert: false);
    }

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
        var result = await Open<bool>(title, typeof(ConfirmDialog), parameters, new() { Size = DialogSize.Small },
            isAlert: true);
        return result.Confirmed;
    }

    private Task<DialogResult<TResult>> Open<TResult>(string title, Type component,
        IReadOnlyDictionary<string, object?> parameters, DialogOptions options, bool isAlert)
    {
        var settings = trailblazorOptions.Value.Dialogs;
        options = options with
        {
            DismissOnEscape = options.DismissOnEscape ?? settings.DismissOnEscape,
            DismissOnBackdrop = options.DismissOnBackdrop ?? settings.DismissOnBackdrop,
        };
        var completion = new TaskCompletionSource<DialogResult<TResult>>();
        var dialog = new DialogReference(title, options, component, parameters, isAlert, CloseAsync);
        _completions[dialog] = (confirmed, value) => completion.SetResult(
            confirmed ? DialogResult<TResult>.Ok((TResult)value!) : DialogResult<TResult>.Cancel());
        _open.Add(dialog);
        Changed?.Invoke();
        return completion.Task;
    }

    private async Task CloseAsync(DialogReference dialog, bool confirmed, object? value)
    {
        if (!_completions.Remove(dialog, out var complete))
        {
            return;
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
            _open.Remove(dialog);
            Changed?.Invoke();
            complete(confirmed, value);
        }
    }
}
