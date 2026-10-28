namespace Trailblazor;

/// <summary>An SVG icon: the markup inside the <c>&lt;svg&gt;</c> element and its view box. Icon packs expose these
/// as static values; an app can also make its own from raw SVG.</summary>
/// <param name="Svg">The SVG content, such as one or more <c>&lt;path&gt;</c> elements. It is rendered as markup, so
/// it must come from the app's code, never from user input.</param>
/// <param name="ViewBox">The SVG view box. Defaults to the 24 by 24 grid most icon sets use.</param>
public readonly record struct Icon(string Svg, string ViewBox = "0 0 24 24");
