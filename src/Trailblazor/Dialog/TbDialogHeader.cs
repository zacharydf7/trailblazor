using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Replaces what the dialog's header shows with the app's own content, such as a title bar with its own
/// buttons. Put it anywhere in the dialog's content. It renders in the header, so it still shows while the dialog is
/// minimized, and dragging it moves a draggable dialog, whose Move button stays for the keyboard. The header's own
/// title, window buttons and close button give way to it, so the app renders the controls it needs, such as one that
/// sets <see cref="DialogReference.WindowState"/> back to normal. The title still names the dialog. When
/// <see cref="DialogOptions.ShowHeader"/> is false, it shows nothing.</summary>
public sealed class TbDialogHeader : ComponentBase, IDisposable
{
    [CascadingParameter]
    private DialogReference? Dialog { get; set; }

    /// <summary>What the header shows.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private DialogReference Reference => Dialog
        ?? throw new InvalidOperationException($"Put {nameof(TbDialogHeader)} inside a dialog.");

    /// <inheritdoc />
    protected override void OnParametersSet() => Reference.Fill(Reference.CustomHeader, this, ChildContent);

    /// <inheritdoc />
    public void Dispose() => Dialog?.Empty(Dialog.CustomHeader, this);
}
