using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>Lays out its content in a column or a row, with even gaps between the items.</summary>
public sealed class TbStack : TbComponentBase
{
    /// <summary>Lays the items out in a row instead of a column, at every width.</summary>
    [Parameter]
    public bool Row { get; set; }

    /// <summary>Lays the items out in a column below this breakpoint and in a row from it, such as
    /// <see cref="Breakpoint.Md"/> for a row from 768px. <see cref="Row"/> makes it a row at every width.</summary>
    [Parameter]
    public Breakpoint? RowFrom { get; set; }

    /// <summary>The space between items, as a step of the spacing scale (0, 1, 2, 3, 4, 5, 6, 8, 10, 12 or 16).
    /// Defaults to 3.</summary>
    [Parameter]
    public int Gap { get; set; } = 3;

    /// <summary>Where items sit across the stack. Defaults to <see cref="Trailblazor.Align.Stretch"/>.</summary>
    [Parameter]
    public Align Align { get; set; } = Align.Stretch;

    /// <summary>How items are spread along the stack. Defaults to <see cref="Trailblazor.Justify.Start"/>.</summary>
    [Parameter]
    public Justify Justify { get; set; } = Justify.Start;

    /// <summary>Lets items wrap onto more lines when they don't fit.</summary>
    [Parameter]
    public bool Wrap { get; set; }

    /// <summary>The element to render, such as <see cref="LayoutElement.Ul"/> for a list of items or
    /// <see cref="LayoutElement.Nav"/> for a row of links. Defaults to <see cref="LayoutElement.Div"/>.</summary>
    [Parameter]
    public LayoutElement As { get; set; }

    /// <summary>The stack's items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, LayoutElements.Tag(As));
        builder.AddAttribute(1, "role", LayoutElements.Role(As));
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "style", Styles.Join($"--tb-stack-gap: {Spacing.Token(Gap, nameof(Gap))}", Style));
        builder.AddAttribute(4, "class", ClassNames.Join("tb-stack", Row ? "tb-stack--row" : null,
            RowFrom is { } from ? ClassNames.Modifier("tb-stack", "row-from", from) : null,
            Wrap ? "tb-stack--wrap" : null, ClassNames.Modifier("tb-stack", "align", Align),
            ClassNames.Modifier("tb-stack", "justify", Justify), Class));
        builder.AddContent(5, ChildContent);
        builder.CloseElement();
    }
}
