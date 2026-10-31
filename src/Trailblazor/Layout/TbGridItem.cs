using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>One item in a <see cref="TbGrid"/>, taking as much of its row as it asks for at each width: some of the
/// grid's 12 columns, the room left in the row, or what its content needs.</summary>
public sealed class TbGridItem : TbComponentBase
{
    private const int Columns = 12;
    private static readonly string[] Breakpoints = ["", "Sm", "Md", "Lg", "Xl"];

    // The span and offset at each breakpoint, from the narrowest up.
    private GridSpan[] _spans = [];
    private int[] _offsets = [];

    /// <summary>How much of its row the item takes at every width: a number of columns, such as <c>6</c>, or
    /// <see cref="GridSpan.Grow"/> or <see cref="GridSpan.Auto"/>. Defaults to 12, the whole row.</summary>
    [Parameter]
    public GridSpan Span { get; set; } = Columns;

    /// <summary>The span from the small breakpoint (640px) up. Defaults to the next smaller span.</summary>
    [Parameter]
    public GridSpan? SpanSm { get; set; }

    /// <summary>The span from the medium breakpoint (768px) up. Defaults to the next smaller span.</summary>
    [Parameter]
    public GridSpan? SpanMd { get; set; }

    /// <summary>The span from the large breakpoint (1024px) up. Defaults to the next smaller span.</summary>
    [Parameter]
    public GridSpan? SpanLg { get; set; }

    /// <summary>The span from the extra-large breakpoint (1280px) up. Defaults to the next smaller span.</summary>
    [Parameter]
    public GridSpan? SpanXl { get; set; }

    /// <summary>How many columns (0 to 11) to leave empty before the item, at every width. An item that begins a row
    /// starts in column <c>Offset + 1</c>. Defaults to 0.</summary>
    [Parameter]
    public int Offset { get; set; }

    /// <summary>The offset from the small breakpoint (640px) up. Defaults to the next smaller offset.</summary>
    [Parameter]
    public int? OffsetSm { get; set; }

    /// <summary>The offset from the medium breakpoint (768px) up. Defaults to the next smaller offset.</summary>
    [Parameter]
    public int? OffsetMd { get; set; }

    /// <summary>The offset from the large breakpoint (1024px) up. Defaults to the next smaller offset.</summary>
    [Parameter]
    public int? OffsetLg { get; set; }

    /// <summary>The offset from the extra-large breakpoint (1280px) up. Defaults to the next smaller offset.</summary>
    [Parameter]
    public int? OffsetXl { get; set; }

    /// <summary>The element to render, such as <see cref="LayoutElement.Li"/> in a grid rendered as a list.
    /// Defaults to <see cref="LayoutElement.Div"/>.</summary>
    [Parameter]
    public LayoutElement As { get; set; }

    /// <summary>The item's content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Each breakpoint takes the next smaller one's value unless it has its own.
        var sm = SpanSm ?? Span;
        var md = SpanMd ?? sm;
        var lg = SpanLg ?? md;
        _spans = [Span, sm, md, lg, SpanXl ?? lg];
        var offsetSm = OffsetSm ?? Offset;
        var offsetMd = OffsetMd ?? offsetSm;
        var offsetLg = OffsetLg ?? offsetMd;
        _offsets = [Offset, offsetSm, offsetMd, offsetLg, OffsetXl ?? offsetLg];
        for (var step = 0; step < Breakpoints.Length; step++)
        {
            var (span, offset) = (_spans[step], _offsets[step]);
            if (span.IsColumns && span.Columns is < 1 or > Columns)
            {
                throw new ArgumentOutOfRangeException(nameof(Span) + Breakpoints[step], span,
                    "Spans run from 1 to 12.");
            }

            if (offset is < 0 or >= Columns)
            {
                throw new ArgumentOutOfRangeException(nameof(Offset) + Breakpoints[step], offset,
                    "Offsets run from 0 to 11.");
            }

            if (span.IsColumns && offset + span.Columns > Columns)
            {
                throw new ArgumentOutOfRangeException(nameof(Offset) + Breakpoints[step], offset,
                    "The item runs past column 12 at some width.");
            }
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, LayoutElements.Tag(As));
        builder.AddAttribute(1, "role", LayoutElements.Role(As));
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "class", ClassNames.Join("tb-grid-item", Class));
        builder.AddAttribute(4, "style", Styles.Join(LayoutStyle(), Style));
        builder.AddContent(5, ChildContent);
        builder.CloseElement();
    }

    // Every item sets every breakpoint's values, so a nested item never inherits its parent's.
    private string LayoutStyle()
    {
        var declarations = new List<string>();
        for (var step = 0; step < Breakpoints.Length; step++)
        {
            var suffix = step == 0 ? "" : $"-{Breakpoints[step].ToLowerInvariant()}";
            declarations.Add($"--tb-grid-flex{suffix}: {Flex(_spans[step])}");
            declarations.Add($"--tb-grid-offset{suffix}: {_offsets[step]}");
        }

        return string.Join("; ", declarations);
    }

    // A span of columns covers its columns and the gaps between them.
    private static string Flex(GridSpan span) => span == GridSpan.Grow ? "1 1 0%" : span == GridSpan.Auto
        ? "0 1 auto" : $"0 0 calc({span.Columns} * var(--tb-grid-column) + {span.Columns - 1} * var(--tb-grid-gap))";
}
