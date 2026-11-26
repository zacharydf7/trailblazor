using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Puts the page in its theme, palette and density before it paints: the app's defaults from
/// <see cref="TrailblazorOptions.Theme"/>, or the user's remembered choice. Place it in <c>&lt;head&gt;</c>, next to
/// <see cref="TbStyles"/>.</summary>
public partial class TbThemeHead
{
    /// <summary>The nonce the page's Content Security Policy allows, put on the theme's script and on the style a
    /// seeded palette adds, so the policy needn't allow inline <c>&lt;style&gt;</c> elements. See the Content Security
    /// Policy guide.</summary>
    [Parameter]
    public string? Nonce { get; set; }
}
