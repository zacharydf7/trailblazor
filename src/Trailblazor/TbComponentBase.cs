using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Styling passthrough every Trailblazor component puts on its root element.</summary>
public abstract class TbComponentBase : ComponentBase
{
    /// <summary>CSS classes added to the root element.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Inline styles added to the root element.</summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>Any other attributes, such as <c>id</c> or <c>aria-*</c>, added to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }
}
