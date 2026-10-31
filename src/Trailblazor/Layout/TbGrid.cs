using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>A 12-column grid that lays out <see cref="TbGridItem"/>s in rows, starting a new row when the next item
/// doesn't fit.</summary>
public sealed class TbGrid : TbComponentBase
{
    /// <summary>The space between rows and columns, as a step of the spacing scale (0, 1, 2, 3, 4, 5, 6, 8, 10, 12
    /// or 16). Defaults to 4.</summary>
    [Parameter]
    public int Gap { get; set; } = 4;

    /// <summary>The element to render, such as <see cref="LayoutElement.Ul"/> for a list of cards whose items are
    /// rendered as <see cref="LayoutElement.Li"/>. Defaults to <see cref="LayoutElement.Div"/>.</summary>
    [Parameter]
    public LayoutElement As { get; set; }

    /// <summary>The grid's <see cref="TbGridItem"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, LayoutElements.Tag(As));
        builder.AddAttribute(1, "role", LayoutElements.Role(As));
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "class", ClassNames.Join("tb-grid", Class));
        builder.AddAttribute(4, "style", Styles.Join($"--tb-grid-gap: {Spacing.Token(Gap, nameof(Gap))}", Style));
        builder.AddContent(5, ChildContent);
        builder.CloseElement();
    }
}
