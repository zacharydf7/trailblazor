using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>The base for a dialog's content when the dialog has no result, such as one that only informs. Open it with
/// <see cref="IDialogService.ShowAsync{TDialog}"/>, which returns whether it was confirmed. Close it with
/// <c>Dialog.ConfirmAsync()</c> to confirm or <c>Dialog.CancelAsync()</c> to cancel. Its markup becomes the dialog's
/// body; put the buttons in a <see cref="TbDialogActions"/>.</summary>
public abstract class TbDialogContent : TbDialogContent<bool>
{
    // A dialog with no result has no value to close with, so its reference offers ConfirmAsync in place of CloseAsync.
    /// <summary>The open dialog, so the content can close it, with <c>Dialog.ConfirmAsync()</c> or
    /// <c>Dialog.CancelAsync()</c>, and change it while it's open, as in <c>Dialog.Size = DialogSize.Large</c>.
    /// </summary>
    protected new DialogReference Dialog => base.Dialog;
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
                value.SetDefaultTitle(() => DefaultTitle);
            }
        }
    }

    // One member rather than one per action or setting, so the names an app gives its own handlers and markup, such
    // as CloseDialogAsync or Size.Large, stay free.
    /// <summary>The open dialog, so the content can close it, with <c>Dialog.CloseAsync(value)</c> or
    /// <c>Dialog.CancelAsync()</c>, and change it while it's open: set its <see cref="DialogReference.Size"/>,
    /// <see cref="DialogReference.Modal"/>, <see cref="DialogReference.Position"/>,
    /// <see cref="DialogReference.WindowState"/>, <see cref="DialogReference.ShowHeader"/> or
    /// <see cref="DialogReference.CloseButton"/>, as in <c>Dialog.Size = DialogSize.Large</c>.</summary>
    protected DialogReference<TResult> Dialog => _dialog as DialogReference<TResult>
        ?? throw new InvalidOperationException($"Open {GetType().Name} with {nameof(IDialogService)}.");

    /// <summary>The title the dialog takes when it's opened without one, which names it for screen readers and shows in
    /// its header. Override it in a dialog that always has the same title, so callers can leave the title out. Opening
    /// a dialog without a title throws unless its content overrides this, and the dialog throws as it renders if the
    /// override returns a blank title.</summary>
    protected virtual string DefaultTitle => "";

    // Read before the content exists, so a dialog opened without a title fails at once rather than opening unnamed.
    internal static bool HasDefaultTitle(Type dialog) =>
        dialog.GetProperty(nameof(DefaultTitle), BindingFlags.Instance | BindingFlags.NonPublic)?.DeclaringType
            != typeof(TbDialogContent<TResult>);

    /// <summary>Called when the user presses Escape, clicks the backdrop or presses the close button, and cancels the
    /// dialog unless overridden. Override it to send these through the dialog's own cancel, such as one that asks
    /// about unsaved changes first; the dialog stays open unless the override closes it. Closing from code doesn't call
    /// it.
    /// </summary>
    protected virtual Task OnDismissAsync(DialogDismissReason reason) => Dialog.CancelAsync();
}
