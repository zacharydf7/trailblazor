using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>A panel with the app's surface color, an optional border and an optional shadow, for grouping content.
/// </summary>
public sealed class TbSurface : TbComponentBase
{
    /// <summary>The shadow under the surface. Defaults to <see cref="Trailblazor.Elevation.Low"/>.</summary>
    [Parameter]
    public Elevation Elevation { get; set; } = Elevation.Low;

    /// <summary>Draws a border around the surface.</summary>
    [Parameter]
    public bool Outlined { get; set; }

    /// <summary>The element to render, such as <see cref="LayoutElement.Article"/> for a card that stands on its own
    /// or <see cref="LayoutElement.Section"/> for a part of the page. Defaults to <see cref="LayoutElement.Div"/>.
    /// </summary>
    [Parameter]
    public LayoutElement As { get; set; }

    /// <summary>The surface's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, LayoutElements.Tag(As));
        builder.AddAttribute(1, "role", LayoutElements.Role(As));
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "style", Style);
        builder.AddAttribute(4, "class", ClassNames.Join("tb-surface", ClassNames.Modifier("tb-surface", Elevation),
            Outlined ? "tb-surface--outlined" : null, Class));
        builder.AddContent(5, ChildContent);
        builder.CloseElement();
    }
}
