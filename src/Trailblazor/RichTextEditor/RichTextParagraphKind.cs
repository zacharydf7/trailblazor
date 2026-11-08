namespace Trailblazor;

/// <summary>What a <see cref="RichTextParagraph"/> is: a plain paragraph, a heading, a quote, a list item or a rule.
/// </summary>
public enum RichTextParagraphKind
{
    /// <summary>A plain paragraph, written as <c>&lt;p&gt;</c>.</summary>
    Paragraph,

    /// <summary>A top-level heading, written as <c>&lt;h1&gt;</c>.</summary>
    Heading1,

    /// <summary>A second-level heading, written as <c>&lt;h2&gt;</c>.</summary>
    Heading2,

    /// <summary>A third-level heading, written as <c>&lt;h3&gt;</c>.</summary>
    Heading3,

    /// <summary>A paragraph of a block quote. Neighboring quote paragraphs share one <c>&lt;blockquote&gt;</c>.
    /// </summary>
    Quote,

    /// <summary>An item of a bulleted list, written in a <c>&lt;ul&gt;</c>.</summary>
    BulletedItem,

    /// <summary>An item of a numbered list, written in an <c>&lt;ol&gt;</c>.</summary>
    NumberedItem,

    /// <summary>A horizontal rule, written as <c>&lt;hr&gt;</c>. It has no text.</summary>
    Rule,
}
