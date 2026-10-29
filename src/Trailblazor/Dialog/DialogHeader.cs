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

    protected override void OnInitialized() => Dialog.HeaderChanged += OnHeaderChanged;

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
        if (Dialog.CloseButton)
        {
            builder.OpenComponent<TbIconButton>(17);
            builder.AddComponentParameter(18, nameof(TbIconButton.Icon), BuiltInIcons.Close);
            builder.AddComponentParameter(19, nameof(TbIconButton.AriaLabel), "Close");
            builder.AddComponentParameter(20, nameof(TbIconButton.Size), Size.Small);
            builder.AddComponentParameter(21, nameof(TbIconButton.OnClick),
                EventCallback.Factory.Create<MouseEventArgs>(this, () => Dialog.CloseAsync(false, null)));
            builder.AddAttribute(22, "data-part", "close");
            builder.CloseComponent();
        }

        builder.CloseElement();
    }

    private void OnHeaderChanged() => InvokeAsync(StateHasChanged);

    public void Dispose() => Dialog.HeaderChanged -= OnHeaderChanged;
}
