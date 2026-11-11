using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Trailblazor;

internal sealed class DialogService(IOptions<TrailblazorOptions> trailblazorOptions, ITbLocalizer localizer,
    TimeProvider time, Telemetry telemetry, ILogger<DialogService>? logger = null) : IDialogService
{
    // An app can open dialogs off the dispatcher, such as from a hub handler, while the host renders them, so the list
    // is replaced rather than changed.
    private readonly Lock _lock = new();
    private ImmutableList<DialogReference> _open = [];

    public IReadOnlyList<DialogReference> OpenDialogs => _open;

    internal HostCheck Hosts { get; } = new("A dialog", time, logger);

    // A modal makes the rest of the page inert, so toasts and announcements go inside the topmost one.
    public DialogReference? TopmostModal => _open.LastOrDefault(dialog => dialog.IsBlocking && !dialog.Closing);

    // Raised when a dialog opens, closes, or starts or stops blocking the page, so the hosts re-render.
    public event Action? Changed;

    public DialogReference<TResult> Show<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog, TResult>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult>
    {
        RequireTitle<TResult>(typeof(TDialog), title);
        var values = new DialogParameters<TDialog>();
        parameters?.Invoke(values);
        return Open<TResult>(title, typeof(TDialog), values.Values, options ?? new(), isAlert: false);
    }

    public Task<DialogResult<TResult>> ShowAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog, TResult>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult> => Show<TDialog, TResult>(title, parameters, options).Result;

    public DialogReference<bool> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent => Show<TDialog, bool>(title, parameters, options);

    public async Task<bool> ShowAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent => (await Show<TDialog>(title, parameters, options).Result).Confirmed;

    public DialogReference<TResult> ShowDynamic<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type dialog, string? title = null,
        IReadOnlyDictionary<string, object?>? parameters = null, DialogOptions? options = null)
    {
        if (dialog.IsAbstract || !typeof(TbDialogContent<TResult>).IsAssignableFrom(dialog))
        {
            throw new ArgumentException($"{dialog.Name} isn't a TbDialogContent<{typeof(TResult).Name}>.",
                nameof(dialog));
        }

        RequireTitle<TResult>(dialog, title);

        // Blazor matches parameter names without regard to case, and finds a wrong name or value only as it renders.
        foreach (var (name, value) in parameters ?? ImmutableDictionary<string, object?>.Empty)
        {
            var property = dialog.GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property?.IsDefined(typeof(ParameterAttribute)) != true)
            {
                throw new ArgumentException($"{dialog.Name} has no parameter named {name}.", nameof(parameters));
            }

            var type = property.PropertyType;
            var fits = value is null ? !type.IsValueType || Nullable.GetUnderlyingType(type) is not null
                : type.IsInstanceOfType(value);
            if (!fits)
            {
                throw new ArgumentException($"{dialog.Name}'s {property.Name} takes a {type.Name}.",
                    nameof(parameters));
            }
        }

        var values = parameters is null ? [] : new Dictionary<string, object?>(parameters);
        return Open<TResult>(title, dialog, values, options ?? new(), isAlert: false);
    }

    public Task<DialogResult<TResult>> ShowDynamicAsync<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type dialog, string? title = null,
        IReadOnlyDictionary<string, object?>? parameters = null, DialogOptions? options = null) =>
        ShowDynamic<TResult>(dialog, title, parameters, options).Result;

    public async Task<bool> ConfirmAsync(string title, string message, string? confirmText = null,
        string? cancelText = null, Tone tone = Tone.Primary, string? telemetryName = null) =>
        (await OpenConfirm(title, message, confirmText, cancelText, tone, telemetryName).Result).Confirmed;

    public async Task AlertAsync(string title, string message, string? okText = null, Tone tone = Tone.Info,
        string? telemetryName = null) => await OpenAlert(title, message, okText, tone, telemetryName).Result;

    // The dialogs behind ConfirmAsync and AlertAsync, which FakeDialogService records and answers.
    internal DialogReference<bool> OpenConfirm(string title, string message, string? confirmText, string? cancelText,
        Tone tone, string? telemetryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var parameters = new Dictionary<string, object?>
        {
            [nameof(ConfirmDialog.Message)] = message,
            [nameof(ConfirmDialog.ConfirmText)] = confirmText ?? localizer[TbStrings.DialogConfirm],
            [nameof(ConfirmDialog.CancelText)] = cancelText ?? localizer[TbStrings.DialogCancel],
            [nameof(ConfirmDialog.Tone)] = tone,
        };
        // A click that misses the dialog shouldn't answer it.
        return Open<bool>(title, typeof(ConfirmDialog), parameters,
            new() { Size = DialogSize.Small, DismissOnBackdrop = false, TelemetryName = telemetryName }, isAlert: true);
    }

    internal DialogReference<bool> OpenAlert(string title, string message, string? okText, Tone tone,
        string? telemetryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        var parameters = new Dictionary<string, object?>
        {
            [nameof(ConfirmDialog.Message)] = message,
            [nameof(ConfirmDialog.ConfirmText)] = okText ?? localizer[TbStrings.DialogConfirm],
            [nameof(ConfirmDialog.Tone)] = tone,
            [nameof(ConfirmDialog.ShowIcon)] = true,
        };
        // A click that misses the dialog shouldn't answer it.
        return Open<bool>(title, typeof(ConfirmDialog), parameters,
            new() { Size = DialogSize.Small, DismissOnBackdrop = false, TelemetryName = telemetryName }, isAlert: true);
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

    // Read before the content exists, so a dialog opened without a title fails at once rather than opening unnamed.
    private static void RequireTitle<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicProperties)] Type dialog, string? title)
    {
        var defaultTitle = dialog.GetProperty(TbDialogContent<TResult>.DefaultTitleName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (string.IsNullOrWhiteSpace(title) && defaultTitle?.DeclaringType == typeof(TbDialogContent<TResult>))
        {
            throw new ArgumentException($"Give {dialog.Name} a title, or override its DefaultTitle.", nameof(title));
        }
    }

    private DialogReference<TResult> Open<TResult>(
        string? title, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type? component,
        IReadOnlyDictionary<string, object?> parameters, DialogOptions options, bool isAlert)
    {
        var settings = trailblazorOptions.Value.Dialogs;
        // The app's width would override a size or width the options chose, so it applies only when they set neither.
        var sized = options.Size is not null || options.Width is not null;
        options = options with
        {
            DismissOnEscape = options.DismissOnEscape ?? settings.DismissOnEscape,
            DismissOnBackdrop = options.DismissOnBackdrop ?? settings.DismissOnBackdrop,
            TopLayer = options.TopLayer ?? settings.TopLayer,
            Size = options.Size ?? settings.Size,
            Width = sized ? options.Width : settings.Width,
            CloseButton = options.CloseButton ?? settings.CloseButton,
        };
        var dialog = new DialogReference<TResult>(title, options, component, parameters, isAlert, CloseAsync)
        {
            OpenedAt = time.GetTimestamp(),
        };
        dialog.BlockingChanged += () => Changed?.Invoke();
        lock (_lock)
        {
            _open = _open.Add(dialog);
        }

        // A TbDialog in markup renders itself, so only the dialogs the host renders need one.
        if (component is not null)
        {
            Hosts.Shown();
        }

        if (options.TelemetryName is { } name)
        {
            telemetry.ControlShown(name, ComponentOf(dialog));
        }

        Changed?.Invoke();
        return dialog;
    }

    // A dialog is named by its label, and its component by its content's type, never by its title.
    private void CountClose(DialogReference dialog, bool confirmed)
    {
        if (dialog.Options.TelemetryName is not { } name)
        {
            return;
        }

        var outcome = confirmed ? "confirmed" : dialog.DismissedBy switch
        {
            DialogDismissReason.Escape => "escape",
            DialogDismissReason.Backdrop => "backdrop",
            DialogDismissReason.CloseButton => "close_button",
            _ => "cancelled",
        };
        telemetry.DialogClosed(name, ComponentOf(dialog), outcome, time.GetElapsedTime(dialog.OpenedAt));
    }

    internal static Type ComponentOf(DialogReference dialog) => dialog.ContentType ?? typeof(TbDialog);

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

        // The page takes input while the dialog fades out, so its toasts and announcements go back to the page now.
        Changed?.Invoke();
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
            CountClose(dialog, confirmed);
            dialog.Complete(confirmed, value);
        }
    }
}
