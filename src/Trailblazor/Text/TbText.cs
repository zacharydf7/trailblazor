using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>Text in the type scale. Its look (<see cref="TextStyle"/>) and its element (<see cref="As"/>) are set
/// separately, so a heading level never has to change to change how it looks.</summary>
public sealed class TbText : TbComponentBase
{
    /// <summary>The typographic style. Defaults to <see cref="TextStyle.Body"/>.</summary>
    [Parameter]
    public TextStyle TextStyle { get; set; } = TextStyle.Body;

    /// <summary>The element to render. Defaults to <see cref="TextElement.P"/>.</summary>
    [Parameter]
    public TextElement As { get; set; } = TextElement.P;

    /// <summary>Colors the text by meaning. When null, the text inherits its color.</summary>
    [Parameter]
    public Tone? Tone { get; set; }

    /// <summary>The text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, As.ToString().ToLowerInvariant());
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "class", ClassNames.Join("tb-text", ClassNames.Modifier("tb-text", TextStyle),
            Tone is { } tone ? ClassNames.Modifier("tb-text", tone) : null, Class));
        builder.AddAttribute(3, "style", Style);
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }
}
