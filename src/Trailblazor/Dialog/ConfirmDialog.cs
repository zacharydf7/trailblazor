using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The dialog behind IDialogService.ConfirmAsync. C#, so it can stay internal.
internal sealed class ConfirmDialog : TbDialog<bool>
{
    [Parameter]
    public string Message { get; set; } = "";

    [Parameter]
    public string ConfirmText { get; set; } = "";

    [Parameter]
    public string CancelText { get; set; } = "";

    [Parameter]
    public Tone Tone { get; set; } = Tone.Primary;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbText>(0);
        builder.AddAttribute(1, "id", Reference.DescriptionId);
        builder.AddComponentParameter(2, nameof(TbText.ChildContent),
            (RenderFragment)(text => text.AddContent(0, Message)));
        builder.CloseComponent();
        builder.OpenComponent<TbDialogActions>(3);
        builder.AddComponentParameter(4, nameof(TbDialogActions.ChildContent), (RenderFragment)(actions =>
        {
            AddButton(actions, 0, CancelText, Variant.Outlined, Tone.Neutral, CancelAsync);
            AddButton(actions, 10, ConfirmText, Variant.Filled, Tone, () => CloseAsync(true));
        }));
        builder.CloseComponent();
    }

    private void AddButton(RenderTreeBuilder builder, int sequence, string text, Variant variant, Tone tone,
        Func<Task> onClick)
    {
        builder.OpenComponent<TbButton>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(TbButton.Variant), variant);
        builder.AddComponentParameter(sequence + 2, nameof(TbButton.Tone), tone);
        builder.AddComponentParameter(sequence + 3, nameof(TbButton.OnClick),
            EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, onClick));
        builder.AddComponentParameter(sequence + 4, nameof(TbButton.ChildContent),
            (RenderFragment)(content => content.AddContent(0, text)));
        builder.CloseComponent();
    }
}
