using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Shows buttons or a menu in the dialog's header, after the title and before its window buttons. Put it
/// anywhere in the dialog's content. Unlike <see cref="TbDialogTitle"/>'s content, it isn't part of the dialog's
/// accessible name, and a drag that starts on it doesn't move the dialog. Without the header, it shows nothing.
/// </summary>
public sealed class TbDialogHeaderActions : ComponentBase, IDisposable
{
    [CascadingParameter]
    private DialogReference? Dialog { get; set; }

    /// <summary>What the header shows after the title.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private DialogReference Reference => Dialog
        ?? throw new InvalidOperationException($"Put {nameof(TbDialogHeaderActions)} inside a dialog.");

    /// <inheritdoc />
    protected override void OnParametersSet() => Reference.Fill(Reference.HeaderActions, this, ChildContent);

    /// <inheritdoc />
    public void Dispose() => Dialog?.Empty(Dialog.HeaderActions, this);
}
