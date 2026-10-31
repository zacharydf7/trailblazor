namespace Trailblazor;

/// <summary>Opens dialogs from code. Register it with <c>AddTrailblazor()</c> and put one <see cref="TbRoot"/> in the
/// layout to host the dialogs.</summary>
public interface IDialogService
{
    /// <summary>The open dialogs, oldest first; the last one is on top.</summary>
    IReadOnlyList<DialogReference> OpenDialogs { get; }

    /// <summary>Opens <typeparamref name="TDialog"/> and returns at once, with a reference that closes, renames or
    /// updates it, and whose <see cref="DialogReference{TResult}.Result"/> finishes when it closes.</summary>
    DialogReference<TResult> Show<TDialog, TResult>(string title, Action<DialogParameters<TDialog>>? parameters = null,
        DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult>;

    /// <summary>Opens <typeparamref name="TDialog"/> and waits until it closes.</summary>
    Task<DialogResult<TResult>> ShowAsync<TDialog, TResult>(string title,
        Action<DialogParameters<TDialog>>? parameters = null, DialogOptions? options = null)
        where TDialog : TbDialogContent<TResult>;

    /// <summary>Asks a yes-or-no question and returns whether it was confirmed. The buttons say the app's text for
    /// <see cref="TbStrings.DialogConfirm"/> and <see cref="TbStrings.DialogCancel"/>, "OK" and "Cancel" in English,
    /// unless <paramref name="confirmText"/> and <paramref name="cancelText"/> say otherwise.</summary>
    Task<bool> ConfirmAsync(string title, string message, string? confirmText = null, string? cancelText = null,
        Tone tone = Tone.Primary);

    /// <summary>Tells the user something with one button, the app's text for <see cref="TbStrings.DialogConfirm"/>
    /// ("OK") unless <paramref name="okText"/> says otherwise, and the tone's icon. Finishes when it closes.</summary>
    Task AlertAsync(string title, string message, string? okText = null, Tone tone = Tone.Info);

    /// <summary>Cancels every open dialog, newest first, without asking them.</summary>
    Task CloseAllAsync();
}
