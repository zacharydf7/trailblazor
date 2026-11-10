using Microsoft.AspNetCore.Components;

namespace Trailblazor;

// The info button beside a label, shared by TbLabel and the fields.
internal static class LabelInfo
{
    public static RenderFragment Render(string buttonName, string? text, RenderFragment? content) => builder =>
    {
        builder.OpenComponent<TbPopover>(0);
        builder.AddComponentParameter(1, nameof(TbPopover.AriaLabel), buttonName);
        builder.AddComponentParameter(2, nameof(TbPopover.Icon), BuiltInIcons.Info);
        builder.AddComponentParameter(3, nameof(TbPopover.Variant), Variant.Text);
        builder.AddComponentParameter(4, nameof(TbPopover.Size), Size.Small);
        builder.AddComponentParameter(5, nameof(TbPopover.OpenOnHover), true);
        builder.AddComponentParameter(6, nameof(TbPopover.Placement), Placement.Top);
        builder.AddAttribute(7, "data-part", "info");
        builder.AddAttribute(8, "class", "tb-label__info");
        builder.AddComponentParameter(9, nameof(TbPopover.ChildContent), (RenderFragment)(panel =>
        {
            panel.OpenElement(0, "div");
            panel.AddAttribute(1, "class",
                content is null ? "tb-label__info-text tb-label__info-text--lines" : "tb-label__info-text");
            if (content is null)
            {
                panel.AddContent(2, text);
            }
            else
            {
                panel.AddContent(3, content);
            }

            panel.CloseElement();
        }));
        builder.CloseComponent();
    };
}
