using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>Shows a dialog's content outside a dialog, such as in a dock panel, a drawer or on the page, so content
/// written as a <see cref="TbDialogContent{TResult}"/> works there unchanged: its <c>Dialog.CloseAsync(value)</c> and
/// <c>Dialog.CancelAsync()</c> end it with a result, which <see cref="Result"/> and <see cref="OnClosed"/> give. The
/// host has no window, so what the content sets on <c>Dialog</c> for one, such as its size or position, does nothing
/// here, and a <see cref="TbDialogTitle"/> or <see cref="TbDialogHeader"/> in the content isn't shown. Its
/// <c>Class</c>, <c>Style</c> and other attributes go on the element around the content. Once the content closes,
/// the host renders nothing; give it a new <c>@key</c> to show the content again.</summary>
/// <typeparam name="TDialog">The content to show.</typeparam>
/// <typeparam name="TResult">What the content closes with.</typeparam>
public sealed class TbDialogContentHost<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TDialog, TResult> : TbComponentBase, IDisposable
    where TDialog : TbDialogContent<TResult>
{
    private readonly Dictionary<string, object?> _values = [];
    private readonly DialogReference<TResult> _dialog;
    private string? _titleGiven;
    private string? _titleReported;

    /// <summary>Creates the host.</summary>
    public TbDialogContentHost()
    {
        _dialog = new(null, new(), typeof(TDialog), _values, isAlert: false, CloseAsync);
        _dialog.Changed += OnDialogChanged;
    }

    /// <summary>The content's title, as on its <c>Dialog.Title</c>. Left unset, the content's
    /// <c>DefaultTitle</c> is its title. Bind it with <c>@bind-Title</c> to show the title somewhere, such as on a
    /// dock panel's tab.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Raised with the content's title once it is known, and when the content changes it, but not for a
    /// title given through <see cref="Title"/>.</summary>
    [Parameter]
    public EventCallback<string?> TitleChanged { get; set; }

    /// <summary>Sets the content's parameters as it is first shown, as the <c>parameters</c> of
    /// <see cref="IDialogService.Show{TDialog, TResult}"/> do. Later renders don't set them again, so what the content
    /// changes stays changed; call <see cref="Update"/> to set some anew.</summary>
    [Parameter]
    public Action<DialogParameters<TDialog>>? Parameters { get; set; }

    /// <summary>Raised when the content closes, with its value or as canceled.</summary>
    [Parameter]
    public EventCallback<DialogResult<TResult>> OnClosed { get; set; }

    /// <summary>Finishes when the content closes, with its value or as canceled. A host that is removed while its
    /// content is still open finishes as canceled, without raising <see cref="OnClosed"/>.</summary>
    public Task<DialogResult<TResult>> Result => _dialog.Result;

    /// <summary>Sets some of the content's parameters, and it renders again with them.</summary>
    public void Update(Action<DialogParameters<TDialog>> parameters) => _dialog.Update(parameters);

    /// <summary>Asks the content to close, as Escape or a dialog's close button would: its <c>OnDismissAsync</c>
    /// decides, and cancels unless it is overridden. Use it for whatever stands in for those here, such as closing
    /// the panel that holds the content.</summary>
    public Task DismissAsync(DialogDismissReason reason = DialogDismissReason.CloseButton) =>
        _dialog.DismissAsync(reason);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var parameters = new DialogParameters<TDialog>();
        Parameters?.Invoke(parameters);
        foreach (var (name, value) in parameters.Values)
        {
            _values[name] = value;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Only a title the parent changed is taken, so the title the content set itself isn't undone by a render.
        if (Title != _titleGiven)
        {
            _titleGiven = Title;
            if (Title is not null && Title != _dialog.Title)
            {
                _titleReported = Title;
                _dialog.Title = Title;
            }
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_dialog.IsClosing)
        {
            return;
        }

        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "data-part", "dialog-content");
        builder.AddAttribute(3, "class", ClassNames.Join("tb-dialog-content", Class));
        builder.AddAttribute(4, "style", Style);
        builder.OpenComponent<CascadingValue<DialogReference>>(5);
        builder.AddComponentParameter(6, "Value", _dialog);
        builder.AddComponentParameter(7, "IsFixed", true);
        builder.AddComponentParameter(8, "ChildContent", (RenderFragment)(content =>
        {
            content.OpenComponent<DialogContent>(0);
            content.AddComponentParameter(1, nameof(DialogContent.Dialog), _dialog);
            content.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The content's own title is known only once the content has rendered.
        if (!_dialog.IsClosing && _dialog.Title is { Length: > 0 } title && title != _titleReported)
        {
            _titleReported = title;
            await TitleChanged.InvokeAsync(title);
        }
    }

    private void OnDialogChanged() => InvokeAsync(StateHasChanged);

    private async Task CloseAsync(DialogReference dialog, bool confirmed, object? value)
    {
        if (dialog.IsClosing)
        {
            return;
        }

        dialog.IsClosing = true;
        dialog.Complete(confirmed, value);
        // Content may close from any thread, as it may in a dialog.
        await InvokeAsync(async () =>
        {
            StateHasChanged();
            await OnClosed.InvokeAsync(await _dialog.Result);
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _dialog.Changed -= OnDialogChanged;
        // A host that goes away with its content still open, as when its panel closes, ends as canceled, so code
        // that awaits the result goes on. There is no parent left to tell.
        if (!_dialog.IsClosing)
        {
            _dialog.IsClosing = true;
            _dialog.Complete(false, null);
        }
    }
}
