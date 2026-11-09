using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>The base for a dialog's content when the dialog has no result, such as one that only informs. Open it with
/// <see cref="IDialogService.ShowAsync{TDialog}"/>, which returns whether it was confirmed. Its markup becomes the
/// dialog's body; put the buttons in a <see cref="TbDialogActions"/>.</summary>
public abstract class TbDialogContent : TbDialogContent<bool>
{
    /// <summary>Closes the dialog as confirmed.</summary>
    protected Task CloseDialogAsync() => base.CloseDialogAsync(true);

    // Hidden because CloseDialogAsync(false) would still close the dialog as confirmed.
    /// <summary>Not for a dialog with no result; use <see cref="CloseDialogAsync()"/> or
    /// <see cref="TbDialogContent{TResult}.CancelDialogAsync"/>.</summary>
    [Obsolete("A dialog with no result closes with CloseDialogAsync() or CancelDialogAsync().", error: true)]
    protected new Task CloseDialogAsync(bool value) => base.CloseDialogAsync(value);
}

/// <summary>The base for a dialog's content that closes with a <typeparamref name="TResult"/>, opened with
/// <see cref="IDialogService.ShowAsync{TDialog, TResult}"/>. Its markup becomes the dialog's body; put the buttons in a
/// <see cref="TbDialogActions"/>.</summary>
public abstract class TbDialogContent<TResult> : ComponentBase
{
    private DialogReference? _dialog;

    // The user's dismissals go to the content, so the hook is wired up as the content arrives.
    [CascadingParameter]
    private DialogReference? CascadedDialog
    {
        get => _dialog;
        set
        {
            _dialog = value;
            if (value is not null)
            {
                value.Dismiss = OnDismissAsync;
            }
        }
    }

    // One member rather than one per setting, so names such as Size stay free for markup like Size.Large.
    /// <summary>The open dialog, so the content can change it while it's open: set its
    /// <see cref="DialogReference.Size"/>, <see cref="DialogReference.Modal"/>, <see cref="DialogReference.Position"/>,
    /// <see cref="DialogReference.WindowState"/>, <see cref="DialogReference.ShowHeader"/> or
    /// <see cref="DialogReference.CloseButton"/>, as in <c>Dialog.Size = DialogSize.Large</c>.</summary>
    protected DialogReference Dialog => _dialog
        ?? throw new InvalidOperationException($"Open {GetType().Name} with {nameof(IDialogService)}.");

    /// <summary>Closes the dialog as confirmed, with <paramref name="value"/> as its result.</summary>
    protected Task CloseDialogAsync(TResult value) => Dialog.CloseAsync(true, value);

    /// <summary>Closes the dialog as canceled.</summary>
    protected Task CancelDialogAsync() => Dialog.CloseAsync(false, null);

    /// <summary>Called when the user presses Escape, clicks the backdrop or presses the close button, and cancels the
    /// dialog unless overridden. Override it to send these through the dialog's own cancel, such as one that asks
    /// about unsaved changes first; the dialog stays open unless the override closes it. Closing from code doesn't call
    /// it.
    /// </summary>
    protected virtual Task OnDismissAsync(DialogDismissReason reason) => CancelDialogAsync();
}
