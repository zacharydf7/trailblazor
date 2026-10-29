using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor;

// A dialog's header, apart from the frame so the dialog can show or hide it while open. C#, so it can stay internal.
internal sealed class DialogHeader : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public DialogReference Dialog { get; set; } = default!;

    [Parameter, EditorRequired]
    public string TitleId { get; set; } = "";

    protected override void OnInitialized() => Dialog.Changed += OnChanged;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Without a header the title still names the dialog, as plain text so it adds no heading.
        if (!Dialog.ShowHeader)
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "id", TitleId);
            builder.AddAttribute(2, "class", "tb-visually-hidden");
            builder.AddContent(3, Dialog.Title);
            builder.CloseElement();
            return;
        }

        builder.OpenElement(10, "header");
        builder.AddAttribute(11, "data-part", "header");
        builder.AddAttribute(12, "class", "tb-dialog__header");
        builder.OpenElement(13, "h2");
        builder.AddAttribute(14, "id", TitleId);
        builder.AddAttribute(15, "class", "tb-dialog__title");
        builder.AddContent(16, Dialog.Title);
        builder.CloseElement();
        var state = Dialog.WindowState;
        if (Dialog.Options.Draggable && !Dialog.Options.Modal && state == DialogWindowState.Normal)
        {
            DialogMenus.AddMove(builder, 17, Dialog);
        }

        if (Dialog.Options.Minimizable && !Dialog.Options.Modal)
        {
            var minimized = state == DialogWindowState.Minimized;
            AddWindowButton(builder, 27, "minimize", minimized ? "Restore" : "Minimize",
                minimized ? BuiltInIcons.Restore : BuiltInIcons.Minus,
                minimized ? DialogWindowState.Normal : DialogWindowState.Minimized);
        }

        if (Dialog.Options.Maximizable)
        {
            var maximized = state == DialogWindowState.Maximized;
            AddWindowButton(builder, 33, "maximize", maximized ? "Restore" : "Maximize",
                maximized ? BuiltInIcons.Restore : BuiltInIcons.Maximize,
                maximized ? DialogWindowState.Normal : DialogWindowState.Maximized);
        }

        if (Dialog.CloseButton)
        {
            builder.OpenComponent<TbIconButton>(39);
            builder.AddComponentParameter(40, nameof(TbIconButton.Icon), BuiltInIcons.Close);
            builder.AddComponentParameter(41, nameof(TbIconButton.AriaLabel), "Close");
            builder.AddComponentParameter(42, nameof(TbIconButton.Size), Size.Small);
            builder.AddComponentParameter(43, nameof(TbIconButton.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(this, () => Dialog.CloseAsync(false, null)));
            builder.AddAttribute(44, "data-part", "close");
            builder.CloseComponent();
        }

        builder.CloseElement();
    }

    // Each button keeps its place as its label changes, so focus stays on it after a click.
    private void AddWindowButton(RenderTreeBuilder builder, int sequence, string part, string label, Icon icon,
        DialogWindowState next)
    {
        builder.OpenComponent<TbIconButton>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbIconButton.Icon), icon);
        builder.AddComponentParameter(sequence + 2, nameof(TbIconButton.AriaLabel), label);
        builder.AddComponentParameter(sequence + 3, nameof(TbIconButton.Size), Size.Small);
        builder.AddComponentParameter(sequence + 4, nameof(TbIconButton.OnClick),
            EventCallback.Factory.Create<MouseEventArgs>(this, () => Dialog.SetWindowState(next)));
        builder.AddAttribute(sequence + 5, "data-part", part);
        builder.CloseComponent();
    }

    private void OnChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Dialog.Changed -= OnChanged;
}
