using System.Diagnostics.CodeAnalysis;

namespace Trailblazor;

/// <summary>Opens dialogs from code. Register it with <c>AddTrailblazor()</c> and put one <see cref="TbRoot"/> in the
/// layout to host the dialogs.</summary>
public interface IDialogService
{
    /// <summary>The open dialogs, oldest first; the last one is on top.</summary>
    IReadOnlyList<DialogReference> OpenDialogs { get; }

    // The overloads differ in their type parameter count, so no call is ambiguous.
#pragma warning disable RS0026
    /// <summary>Opens <typeparamref name="TDialog"/> and returns at once, with a reference that closes, renames or
    /// updates it, and whose <see cref="DialogReference{TResult}.Result"/> finishes when it closes. Leave out the title
    /// for a dialog whose content overrides <c>DefaultTitle</c>.</summary>
    /// <exception cref="ArgumentException">There's no title, and the content doesn't override <c>DefaultTitle</c>.
    /// </exception>
    DialogReference<TResult> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog, TResult>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult>;

    /// <summary>Opens <typeparamref name="TDialog"/> and waits until it closes.</summary>
    Task<DialogResult<TResult>> ShowAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog, TResult>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult>;

    /// <summary>Opens <typeparamref name="TDialog"/>, a dialog with no result, and returns at once, as
    /// <see cref="Show{TDialog, TResult}"/> does. Close it as confirmed with <c>CloseAsync(true)</c>, or cancel it with
    /// <see cref="DialogReference.CancelAsync"/>.</summary>
    DialogReference<bool> Show<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent;

    /// <summary>Opens <typeparamref name="TDialog"/>, a dialog with no result, waits until it closes, and returns
    /// whether it was confirmed.</summary>
    Task<bool> ShowAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog>(
        string? title = null, Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent;
#pragma warning restore RS0026

    /// <summary>Opens a dialog whose content type is chosen at run time, such as from a list, and returns at once, as
    /// <see cref="Show{TDialog, TResult}"/> does. <paramref name="dialog"/> must be a
    /// <see cref="TbDialogContent{TResult}"/>, and <paramref name="parameters"/> are its parameters by name.</summary>
    /// <exception cref="ArgumentException"><paramref name="dialog"/> isn't a <see cref="TbDialogContent{TResult}"/>
    /// that can be created, there's no title and it doesn't override <c>DefaultTitle</c>, or a name in
    /// <paramref name="parameters"/> isn't one of its parameters or its value doesn't fit the parameter's type.
    /// </exception>
    DialogReference<TResult> ShowDynamic<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type dialog, string? title = null,
        IReadOnlyDictionary<string, object?>? parameters = null, DialogOptions? options = null);

    /// <summary>Opens a dialog whose content type is chosen at run time and waits until it closes. See
    /// <see cref="ShowDynamic{TResult}"/>.</summary>
    Task<DialogResult<TResult>> ShowDynamicAsync<TResult>(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type dialog, string? title = null,
        IReadOnlyDictionary<string, object?>? parameters = null, DialogOptions? options = null);

    /// <summary>Asks a yes-or-no question and returns whether it was confirmed. The buttons say the app's text for
    /// <see cref="TbStrings.DialogConfirm"/> and <see cref="TbStrings.DialogCancel"/>, "OK" and "Cancel" in English,
    /// unless <paramref name="confirmText"/> and <paramref name="cancelText"/> say otherwise. With a
    /// <paramref name="telemetryName"/>, it counts as <see cref="DialogOptions.TelemetryName"/> describes.</summary>
    /// <exception cref="ArgumentException"><paramref name="title"/> is blank.</exception>
    Task<bool> ConfirmAsync(string title, string message, string? confirmText = null, string? cancelText = null,
        Tone tone = Tone.Primary, string? telemetryName = null);

    /// <summary>Tells the user something with one button, the app's text for <see cref="TbStrings.DialogConfirm"/>
    /// ("OK") unless <paramref name="okText"/> says otherwise, and the tone's icon. Finishes when it closes. With a
    /// <paramref name="telemetryName"/>, it counts as <see cref="DialogOptions.TelemetryName"/> describes.</summary>
    /// <exception cref="ArgumentException"><paramref name="title"/> is blank.</exception>
    Task AlertAsync(string title, string message, string? okText = null, Tone tone = Tone.Info,
        string? telemetryName = null);

    /// <summary>Cancels every open dialog, newest first, without asking them.</summary>
    Task CloseAllAsync();
}
