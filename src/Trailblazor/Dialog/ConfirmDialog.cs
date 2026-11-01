using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// The dialog behind IDialogService.ConfirmAsync and, with no cancel button, AlertAsync. C#, so it can stay internal.
internal sealed class ConfirmDialog : TbDialogContent<bool>
{
    [Parameter]
    public string Message { get; set; } = "";

    [Parameter]
    public string ConfirmText { get; set; } = "";

    [Parameter]
    public string? CancelText { get; set; }

    [Parameter]
    public Tone Tone { get; set; } = Tone.Primary;

    [Parameter]
    public bool ShowIcon { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "tb-dialog__message");
        if (ShowIcon && BuiltInIcons.ForTone(Tone) is { } icon)
        {
            builder.OpenComponent<TbIcon>(2);
            builder.AddComponentParameter(3, nameof(TbIcon.Icon), icon);
            builder.AddComponentParameter(4, nameof(TbIcon.Tone), Tone);
            builder.AddAttribute(5, "data-part", "icon");
            builder.CloseComponent();
        }

        builder.OpenComponent<TbText>(6);
        builder.AddAttribute(7, "id", Reference.DescriptionId);
        builder.AddComponentParameter(8, nameof(TbText.ChildContent),
            (RenderFragment)(text => text.AddContent(0, Message)));
        builder.CloseComponent();
        builder.CloseElement();
        builder.OpenComponent<TbDialogActions>(9);
        builder.AddComponentParameter(10, nameof(TbDialogActions.ChildContent), (RenderFragment)(actions =>
        {
            if (CancelText is not null)
            {
                AddButton(actions, 0, CancelText, Variant.Outlined, Tone.Neutral, CancelDialogAsync);
            }

            AddButton(actions, 10, ConfirmText, Variant.Filled, Tone, () => CloseDialogAsync(true));
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
