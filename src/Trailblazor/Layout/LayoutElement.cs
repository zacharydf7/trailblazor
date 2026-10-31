namespace Trailblazor;

/// <summary>The HTML element a layout component renders, which gives the region its meaning for assistive technology,
/// such as a list of cards or the page's navigation.</summary>
public enum LayoutElement
{
    /// <summary>A <c>div</c>, which adds no meaning.</summary>
    Div,

    /// <summary>A <c>section</c>, a part of the page; screen readers list it as a region when it is named.</summary>
    Section,

    /// <summary>An <c>article</c>, content that stands on its own, such as a post or a card.</summary>
    Article,

    /// <summary>A <c>main</c>, the page's main content; use one per page.</summary>
    Main,

    /// <summary>A <c>nav</c>, a group of navigation links.</summary>
    Nav,

    /// <summary>An <c>aside</c>, content related to the page but apart from it.</summary>
    Aside,

    /// <summary>A <c>header</c>, the start of the page or of a section.</summary>
    Header,

    /// <summary>A <c>footer</c>, the end of the page or of a section.</summary>
    Footer,

    /// <summary>A <c>ul</c>, a list whose items are <c>li</c> elements or components rendered as
    /// <see cref="Li"/>.</summary>
    Ul,

    /// <summary>An <c>ol</c>, a numbered list, whose items are <c>li</c> elements or components rendered as
    /// <see cref="Li"/>.</summary>
    Ol,

    /// <summary>An <c>li</c>, one item of a <see cref="Ul"/> or <see cref="Ol"/>.</summary>
    Li,
}

internal static class LayoutElements
{
    public static string Tag(LayoutElement element) => element.ToString().ToLowerInvariant();

    // The layout CSS removes a list's bullets, and Safari then stops reading it as a list unless its role says so.
    // It is set before the consumer's attributes, so a role they give takes its place.
    public static string? Role(LayoutElement element) => element is LayoutElement.Ul or LayoutElement.Ol ? "list" : null;
}
