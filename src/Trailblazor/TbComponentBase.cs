using System.Globalization;
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

    /// <summary>Any other attributes, such as <c>id</c> or <c>aria-*</c>, added to the root element. Some components
    /// put the <c>id</c> on the part users act on instead, such as a field's input or a menu's trigger.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // The id given, for a component whose id names the part users act on; one bound to null or empty gives none.
    private protected string? GivenId =>
        Convert.ToString(AdditionalAttributes?.GetValueOrDefault("id"), CultureInfo.InvariantCulture) is
            { Length: > 0 } id ? id : null;

    // The other attributes, for the root of a component whose part takes the id. Names ignore case, as in HTML.
    private protected IReadOnlyDictionary<string, object>? AttributesWithoutId => GivenId is null
        ? AdditionalAttributes
        : AdditionalAttributes!
            .Where(attribute => !string.Equals(attribute.Key, "id", StringComparison.OrdinalIgnoreCase))
            .ToDictionary();
}
