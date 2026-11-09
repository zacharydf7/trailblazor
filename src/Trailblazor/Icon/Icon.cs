using System.Globalization;
using System.Xml;
using System.Xml.Linq;

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

    // The outer element's painting settings, which a stroke icon set relies on to draw outlines.
    private static readonly string[] Painting = ["fill", "fill-rule", "fill-opacity", "clip-rule", "stroke",
        "stroke-width", "stroke-linecap", "stroke-linejoin", "stroke-miterlimit", "stroke-opacity", "stroke-dasharray",
        "stroke-dashoffset"];

    private static readonly XNamespace SvgNamespace = "http://www.w3.org/2000/svg";
    private static readonly XNamespace XLink = "http://www.w3.org/1999/xlink";

    /// <summary>Makes an icon from a whole <c>&lt;svg&gt;</c> element, as icon sets publish them. The view box is the
    /// element's <c>viewBox</c>, or <c>0 0 width height</c> when it has none, or the 24 by 24 grid when it has neither.
    /// The element's fill and stroke settings are kept, so a stroke icon, such as one drawn with
    /// <c>fill="none" stroke="currentColor"</c>, draws as outlines. Like <see cref="Svg"/>, the markup must come from
    /// the app's code, never from user input.</summary>
    /// <exception cref="ArgumentException"><paramref name="svg"/> isn't a single well-formed <c>&lt;svg&gt;</c>
    /// element.</exception>
    public static Icon FromSvg(string svg)
    {
        XElement root;
        try
        {
            root = XElement.Parse(svg);
        }
        catch (XmlException exception)
        {
            throw new ArgumentException("The icon isn't well-formed SVG.", nameof(svg), exception);
        }

        if (root.Name.LocalName != "svg")
        {
            throw new ArgumentException($"The icon is a <{root.Name.LocalName}>, not an <svg>.", nameof(svg));
        }

        // An editor's own markup, such as Inkscape's, means nothing to a browser.
        root.Descendants().Where(element => !IsSvg(element.Name.Namespace)).ToList().Remove();
        foreach (var element in root.DescendantsAndSelf())
        {
            // SVG 2 reads a plain href, so a link needs no xlink namespace.
            if (element.Attribute(XLink + "href") is { } link)
            {
                element.SetAttributeValue("href", link.Value);
            }

            // Inline SVG gets its namespace from the page; foreign attributes would serialize with made-up prefixes.
            element.Attributes().Where(attribute => attribute.IsNamespaceDeclaration
                || !IsSvg(attribute.Name.Namespace) && attribute.Name.Namespace != XNamespace.Xml).Remove();
            element.Name = element.Name.LocalName;
        }

        var content = string.Concat(root.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting)));
        var painting = Painting.Select(name => root.Attribute(name)).OfType<XAttribute>().ToList();
        if (painting.Count > 0)
        {
            content = $"<g {string.Join(' ', painting.Select(attribute => attribute.ToString()))}>{content}</g>";
        }

        return new Icon(content, root.Attribute("viewBox")?.Value ?? SizeBox(root));
    }

    private static bool IsSvg(XNamespace space) => space == XNamespace.None || space == SvgNamespace;

    private static string SizeBox(XElement root) =>
        Length(root.Attribute("width")) is { } width && Length(root.Attribute("height")) is { } height
            ? string.Create(CultureInfo.InvariantCulture, $"0 0 {width} {height}")
            : "0 0 24 24";

    // A size in pixels, written with or without its unit.
    private static double? Length(XAttribute? size) =>
        double.TryParse(size?.Value.Replace("px", "", StringComparison.Ordinal), NumberStyles.Float,
            CultureInfo.InvariantCulture, out var length) ? length : null;
}
