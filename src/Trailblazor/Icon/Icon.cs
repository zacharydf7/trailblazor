namespace Trailblazor;

/// <summary>An SVG icon: the markup inside the <c>&lt;svg&gt;</c> element and its view box. Icon packs expose these
/// as static values; an app can also make its own from raw SVG.</summary>
/// <param name="Svg">The SVG content, such as one or more <c>&lt;path&gt;</c> elements. It is rendered as markup, so
/// it must come from the app's code, never from user input.</param>
/// <param name="ViewBox">The SVG view box. Defaults to the 24 by 24 grid most icon sets use.</param>
public readonly record struct Icon(string Svg, string ViewBox = "0 0 24 24")
{
    /// <summary>Whether the icon turns around on a right-to-left page. Set it for an icon that points along the text,
    /// such as a forward arrow, a send icon or an indent icon. Most icons, such as a check mark, a clock or a play
    /// button, keep their direction, so this is off by default. Set it with <c>with</c>, as in
    /// <c>MaterialSymbols.Outlined.ArrowForward with { MirrorsInRightToLeft = true }</c>, and every component the icon
    /// is passed to draws it turned around.</summary>
    public bool MirrorsInRightToLeft { get; init; }
}
