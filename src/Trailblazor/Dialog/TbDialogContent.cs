using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>The base for a dialog's content, opened with <see cref="IDialogService.ShowAsync{TDialog, TResult}"/>.
/// Its markup becomes the dialog's body; put the buttons in a <see cref="TbDialogActions"/>.</summary>
public abstract class TbDialogContent<TResult> : ComponentBase
{
    private DialogReference? _dialog;

    // The user's dismissals go to the content, so the hook is wired up as the content arrives.
    [CascadingParameter]
    private DialogReference? Dialog
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

    /// <summary>Whether the header with the title shows. It starts as <see cref="DialogOptions.ShowHeader"/>, and
    /// setting it changes the open dialog.</summary>
    protected bool ShowHeader
    {
        get => Reference.ShowHeader;
        set => Reference.SetHeader(value, Reference.CloseButton);
    }

    /// <summary>Whether the header's close button shows. It starts as <see cref="DialogOptions.CloseButton"/>, and
    /// setting it changes the open dialog.</summary>
    protected bool CloseButton
    {
        get => Reference.CloseButton;
        set => Reference.SetHeader(Reference.ShowHeader, value);
    }

    /// <summary>Whether the dialog blocks the rest of the page; setting it switches the open dialog. See
    /// <see cref="DialogReference.Modal"/>.</summary>
    protected bool Modal
    {
        get => Reference.Modal;
        set => Reference.Modal = value;
    }

    /// <summary>How wide the dialog is; setting it resizes the open dialog. See <see cref="DialogReference.Size"/>.
    /// </summary>
    protected DialogSize Size
    {
        get => Reference.Size;
        set => Reference.Size = value;
    }

    /// <summary>Where the dialog sits; setting it moves the open dialog. See <see cref="DialogReference.Position"/>.
    /// </summary>
    protected DialogPosition Position
    {
        get => Reference.Position;
        set => Reference.Position = value;
    }

    /// <summary>Whether the dialog is minimized or maximized; setting it changes the open dialog. See
    /// <see cref="DialogReference.WindowState"/>.</summary>
    protected DialogWindowState WindowState
    {
        get => Reference.WindowState;
        set => Reference.WindowState = value;
    }

    /// <summary>Closes the dialog as confirmed, with <paramref name="value"/> as its result.</summary>
    protected Task CloseDialogAsync(TResult value) => Reference.CloseAsync(true, value);

    /// <summary>Closes the dialog as canceled.</summary>
    protected Task CancelDialogAsync() => Reference.CloseAsync(false, null);

    /// <summary>Called when the user presses Escape, clicks the backdrop or presses the close button, and cancels the
    /// dialog unless overridden. Override it to send these through the dialog's own cancel, such as one that asks
    /// about unsaved changes first; the dialog stays open unless the override closes it. Closing from code doesn't call
    /// it.
    /// </summary>
    protected virtual Task OnDismissAsync(DialogDismissReason reason) => CancelDialogAsync();

    private protected DialogReference Reference => Dialog
        ?? throw new InvalidOperationException($"Open {GetType().Name} with {nameof(IDialogService)}.");
}
