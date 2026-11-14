using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Trailblazor.Testing;

/// <summary>Records the dialogs a component opens, and answers its confirms and alerts when told to, for bUnit.
/// <c>AddTrailblazor()</c> registers it as the <see cref="IDialogService"/>; get it from <c>Services</c>. Dialogs open
/// as they otherwise would, so a rendered <see cref="TbRoot"/> shows them, and each stays open until the test, the
/// component or a click in it closes it.</summary>
public sealed class FakeDialogService : IDialogService
{
    private readonly DialogService _dialogs;
    private readonly Lock _lock = new();
    private ImmutableList<DialogReference> _opened = [];

    internal FakeDialogService(DialogService dialogs) => _dialogs = dialogs;

    /// <summary>Every dialog opened through <see cref="IDialogService"/>, oldest first, closed ones included; a
    /// <see cref="TbDialog"/> written in markup opens itself and isn't here. A confirm or alert has its message in
    /// <see cref="DialogReference.Parameters"/> as <c>Message</c>.</summary>
    public IReadOnlyList<DialogReference> Opened => _opened;

    /// <summary>Answers each confirm and alert as it opens, so a click that awaits one finishes: true confirms, false
    /// cancels, and null leaves it open, as when this isn't set. An alert closes on either answer.</summary>
    public Func<DialogReference, bool?>? Answer { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<DialogReference> OpenDialogs => _dialogs.OpenDialogs;

    /// <summary>The last <typeparamref name="TDialog"/> opened, to check its title and parameters and close it. A title
    /// from the content's <c>DefaultTitle</c> is there only once a rendered <see cref="TbRoot"/> shows the dialog.
    /// </summary>
    /// <exception cref="InvalidOperationException">No <typeparamref name="TDialog"/> was opened.</exception>
    public DialogReference<TResult> Last<TDialog, TResult>() where TDialog : TbDialogContent<TResult> =>
        _opened.OfType<DialogReference<TResult>>().LastOrDefault(dialog => dialog.ContentType == typeof(TDialog))
            ?? throw new InvalidOperationException($"No {typeof(TDialog).Name} was opened.");

    /// <summary>The last <typeparamref name="TDialog"/> opened, a dialog with no result, to check its title and
    /// parameters and close it: <c>CloseAsync(true)</c> confirms and <c>CancelAsync()</c> cancels. A title from the
    /// content's <c>DefaultTitle</c> is there only once a rendered <see cref="TbRoot"/> shows the dialog.</summary>
    /// <exception cref="InvalidOperationException">No <typeparamref name="TDialog"/> was opened.</exception>
    public DialogReference<bool> Last<TDialog>() where TDialog : TbDialogContent => Last<TDialog, bool>();

    /// <summary>The last confirm or alert opened, to check its title and message and answer it:
    /// <c>CloseAsync(true)</c> confirms and <c>CancelAsync()</c> cancels.</summary>
    /// <exception cref="InvalidOperationException">No confirm or alert was opened.</exception>
    public DialogReference<bool> LastConfirm() =>
        _opened.OfType<DialogReference<bool>>().LastOrDefault(dialog => dialog.ContentType == typeof(ConfirmDialog))
            ?? throw new InvalidOperationException("No confirm or alert was opened.");

    // The overloads differ in their type parameter count, so no call is ambiguous.
#pragma warning disable RS0026
    /// <inheritdoc />
    public DialogReference<TResult> Show<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog, TResult>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult> => Record(_dialogs.Show<TDialog, TResult>(title, parameters, options));

    /// <inheritdoc />
    public Task<DialogResult<TResult>> ShowAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog, TResult>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult> => Show<TDialog, TResult>(title, parameters, options).Result;

    /// <inheritdoc />
    public DialogReference<bool> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent => Show<TDialog, bool>(title, parameters, options);

    /// <inheritdoc />
    public async Task<bool> ShowAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent => (await Show<TDialog>(title, parameters, options).Result).Confirmed;
#pragma warning restore RS0026

    /// <inheritdoc />
    public DialogReference<TResult> ShowDynamic<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type dialog, string? title = null,
        IReadOnlyDictionary<string, object?>? parameters = null, DialogOptions? options = null) =>
        Record(_dialogs.ShowDynamic<TResult>(dialog, title, parameters, options));

    /// <inheritdoc />
    public Task<DialogResult<TResult>> ShowDynamicAsync<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type dialog, string? title = null,
        IReadOnlyDictionary<string, object?>? parameters = null, DialogOptions? options = null) =>
        ShowDynamic<TResult>(dialog, title, parameters, options).Result;

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, string? confirmText = null,
        string? cancelText = null, Tone tone = Tone.Primary, string? telemetryName = null,
        DialogOptions? options = null)
    {
        var dialog = Record(
            _dialogs.OpenConfirm(title, message, confirmText, cancelText, tone, telemetryName, options));
        await AnswerAsync(dialog);
        return (await dialog.Result).Confirmed;
    }

    /// <inheritdoc />
    public async Task AlertAsync(string title, string message, string? okText = null, Tone tone = Tone.Info,
        string? telemetryName = null, DialogOptions? options = null)
    {
        var dialog = Record(_dialogs.OpenAlert(title, message, okText, tone, telemetryName, options));
        await AnswerAsync(dialog);
        await dialog.Result;
    }

    /// <inheritdoc />
    public Task CloseAllAsync() => _dialogs.CloseAllAsync();

    // An app can open a dialog off the dispatcher, as the real service allows, while a test reads Opened.
    private DialogReference<TResult> Record<TResult>(DialogReference<TResult> dialog)
    {
        lock (_lock)
        {
            _opened = _opened.Add(dialog);
        }

        return dialog;
    }

    private Task AnswerAsync(DialogReference<bool> dialog) => Answer?.Invoke(dialog) switch
    {
        true => dialog.CloseAsync(true),
        false => dialog.CancelAsync(),
        null => Task.CompletedTask,
    };
}
