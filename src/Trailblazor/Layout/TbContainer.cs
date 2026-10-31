using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>Centers its content and limits how wide it grows.</summary>
public sealed class TbContainer : TbComponentBase
{
    /// <summary>The widest the container grows. Defaults to <see cref="ContainerWidth.Large"/>.</summary>
    [Parameter]
    public ContainerWidth MaxWidth { get; set; } = ContainerWidth.Large;

    /// <summary>The element to render, such as <see cref="LayoutElement.Main"/> for the page's main content.
    /// Defaults to <see cref="LayoutElement.Div"/>.</summary>
    [Parameter]
    public LayoutElement As { get; set; }

    /// <summary>The container's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, LayoutElements.Tag(As));
        builder.AddAttribute(1, "role", LayoutElements.Role(As));
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "style", Style);
        builder.AddAttribute(4, "class",
            ClassNames.Join("tb-container", ClassNames.Modifier("tb-container", MaxWidth), Class));
        builder.AddContent(5, ChildContent);
        builder.CloseElement();
    }
}
