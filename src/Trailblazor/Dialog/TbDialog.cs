using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>The base for a dialog's content, opened with <see cref="IDialogService.ShowAsync{TDialog, TResult}"/>.
/// Its markup becomes the dialog's body; put the buttons in a <see cref="TbDialogActions"/>.</summary>
public abstract class TbDialog<TResult> : ComponentBase
{
    [CascadingParameter]
    private DialogReference? Dialog { get; set; }

    /// <summary>Closes the dialog as confirmed, with <paramref name="value"/> as its result.</summary>
    protected Task CloseAsync(TResult value) => Reference.CloseAsync(true, value);

    /// <summary>Closes the dialog as cancelled.</summary>
    protected Task CancelAsync() => Reference.CloseAsync(false, null);

    private protected DialogReference Reference => Dialog
        ?? throw new InvalidOperationException($"Open {GetType().Name} with {nameof(IDialogService)}.");
}
