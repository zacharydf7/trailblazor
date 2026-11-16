using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>Lays out fields and buttons in a row with every field's box on one line, whatever labels, helper text or
/// errors the fields show. Labels share the line above the boxes, and buttons and other content sit beside the boxes.
/// </summary>
public sealed class TbFieldRow : TbComponentBase
{
    /// <summary>The space between items, as a step of the spacing scale (0, 1, 2, 3, 4, 5, 6, 8, 10, 12 or 16).
    /// Defaults to 3.</summary>
    [Parameter]
    public int Gap { get; set; } = 3;

    /// <summary>The widths of the items in order, as CSS <c>grid-template-columns</c>, such as <c>"1fr auto"</c> for
    /// a field that takes the free space beside a button. By default each item is as wide as its content.</summary>
    [Parameter]
    public string? Columns { get; set; }

    /// <summary>The fields, buttons and other content of the row. Fields line up when they're the row's own children,
    /// not wrapped in another element.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "style", Styles.Join($"--tb-field-row-gap: {Spacing.Token(Gap, nameof(Gap))}",
            Columns is null ? null : $"--tb-field-row-columns: {Columns}", Style));
        builder.AddAttribute(3, "class", ClassNames.Join("tb-field-row", Class));
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }
}
