using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>The base for a dialog's content, opened with <see cref="IDialogService.ShowAsync{TDialog, TResult}"/>.
/// Its markup becomes the dialog's body; put the buttons in a <see cref="TbDialogActions"/>.</summary>
public abstract class TbDialog<TResult> : ComponentBase
{
    private DialogReference? _dialog;

    // The dialog asks its content before the user dismisses it, so the question is wired up as the content arrives.
    [CascadingParameter]
    private DialogReference? Dialog
    {
        get => _dialog;
        set
        {
            _dialog = value;
            if (value is not null)
            {
                value.CanDismiss = CanDismissAsync;
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

    /// <summary>Closes the dialog as confirmed, with <paramref name="value"/> as its result.</summary>
    protected Task CloseAsync(TResult value) => Reference.CloseAsync(true, value);

    /// <summary>Closes the dialog as cancelled.</summary>
    protected Task CancelAsync() => Reference.CloseAsync(false, null);

    /// <summary>Asked when the user presses Escape, clicks the backdrop or presses the close button. Return false to
    /// keep the dialog open, for example to ask about unsaved changes first. Closing from code doesn't ask.</summary>
    protected virtual Task<bool> CanDismissAsync(DialogDismissReason reason) => Task.FromResult(true);

    private protected DialogReference Reference => Dialog
        ?? throw new InvalidOperationException($"Open {GetType().Name} with {nameof(IDialogService)}.");
}

/// <summary>A dialog written in markup, opened and closed with <c>@bind-Open</c>, for a simple dialog that needs no
/// class of its own. Its content renders where it is, so it sees the page's cascading values.</summary>
public sealed class TbDialog : TbComponentBase, IAsyncDisposable
{
    private DialogReference<bool>? _dialog;
    private DialogReference<bool>? _closingForParent;
    private bool _disposed;

    [Inject]
    private DialogService Dialogs { get; set; } = default!;

    /// <summary>Whether the dialog is open. Bind it with <c>@bind-Open</c>: it turns false when the user dismisses
    /// the dialog.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised with false when the user dismisses the dialog.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>The dialog's title, which is also its accessible name.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>How the dialog behaves, as for a dialog opened from code. They're read each time it opens.</summary>
    [Parameter]
    public DialogOptions? Options { get; set; }

    /// <summary>Asked when the user presses Escape, clicks the backdrop or presses the close button. Return false to
    /// keep the dialog open, for example to ask about unsaved changes first. Setting <see cref="Open"/> to false
    /// doesn't ask.</summary>
    [Parameter]
    public Func<DialogDismissReason, Task<bool>>? CanDismiss { get; set; }

    /// <summary>The dialog's body. Put its buttons in a <see cref="TbDialogActions"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Open && _dialog is null)
        {
            OpenDialog();
        }
        else if (!Open && _dialog is not null && _closingForParent != _dialog)
        {
            _closingForParent = _dialog;
            await _dialog.CancelAsync();
        }
        else if (_dialog is not null && _dialog.Title != Title)
        {
            _dialog.Title = Title;
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_dialog is null)
        {
            return;
        }

        builder.OpenComponent<DialogFrame>(0);
        builder.SetKey(_dialog);
        builder.AddComponentParameter(1, nameof(DialogFrame.Dialog), _dialog);
        builder.AddComponentParameter(2, nameof(DialogFrame.ChildContent), ChildContent);
        builder.AddComponentParameter(3, nameof(DialogFrame.Class), Class);
        builder.AddComponentParameter(4, nameof(DialogFrame.Style), Style);
        builder.AddComponentParameter(5, nameof(DialogFrame.AdditionalAttributes), AdditionalAttributes);
        builder.CloseComponent();
    }

    private void OpenDialog()
    {
        var dialog = Dialogs.OpenInPlace(Title, Options ?? new());
        dialog.CanDismiss = reason => CanDismiss is { } ask ? ask(reason) : Task.FromResult(true);
        _dialog = dialog;
        _ = WatchAsync(dialog);
    }

    // Runs outside any event handler, so a failing OpenChanged goes to the renderer instead of being lost. A close
    // the parent asked for isn't reported back, and if the parent opened the dialog again meanwhile, it opens anew.
    private async Task WatchAsync(DialogReference<bool> dialog)
    {
        try
        {
            await dialog.Result;
            _dialog = null;
            if (_disposed)
            {
                return;
            }

            var forParent = _closingForParent == dialog;
            if (forParent && Open)
            {
                OpenDialog();
            }

            StateHasChanged();
            if (!forParent && Open)
            {
                await OpenChanged.InvokeAsync(false);
            }
        }
        catch (Exception exception)
        {
            await DispatchExceptionAsync(exception);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_dialog is not null)
        {
            await _dialog.CancelAsync();
        }
    }
}
