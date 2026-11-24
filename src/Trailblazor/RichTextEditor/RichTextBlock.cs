namespace Trailblazor;

/// <summary>A kind of block of the app's own for a <see cref="TbRichTextEditor"/>, such as a callout, with its toolbar
/// button. A paragraph of the kind is written as <c>&lt;p data-tb-block="name"&gt;</c>, which the app's style sheet
/// gives a look. It holds text, as a plain paragraph does, and no other blocks.</summary>
public sealed class RichTextBlock
{
    // The script keeps the editor's own kinds of block under these names, beside the app's.
    private static readonly string[] Taken =
    [
        "paragraph", "heading1", "heading2", "heading3", "quote", "bullet", "number", "rule", "table", "image", "cell",
    ];

    /// <summary>Creates a kind of block.</summary>
    /// <param name="name">The kind's name, which is written in <c>data-tb-block</c>: lowercase letters, digits and
    /// hyphens, starting with a letter, and at most 32 characters. It can't be the name of one of the editor's own
    /// kinds, such as <c>quote</c>.</param>
    /// <exception cref="ArgumentException">The name isn't one a block can have.</exception>
    public RichTextBlock(string name) => Name = IsName(name) ? name
        : throw new ArgumentException($"\"{name}\" can't name a block: a name is lowercase letters, digits and "
            + "hyphens, starts with a letter, has at most 32 characters, and isn't one of the editor's own kinds of "
            + "block.", nameof(name));

    /// <summary>The kind's name, which a <see cref="RichTextParagraph"/> of the kind has as its
    /// <see cref="RichTextParagraph.Block"/>.</summary>
    public string Name { get; }

    /// <summary>The name of the kind's toolbar button and of its slash command. Defaults to <see cref="Name"/>.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>The icon of the kind's toolbar button. Defaults to the first letter of <see cref="Label"/>, or of
    /// <see cref="Name"/> without one.</summary>
    public Icon? Icon { get; init; }

    /// <summary>The keys that turn a paragraph into the kind and back, written as a <see cref="RichTextMark"/>'s
    /// <see cref="RichTextMark.Shortcut"/> is.</summary>
    public string? Shortcut { get; init; }

    internal static bool IsName(string? name) => RichTextMark.IsPlain(name) && !Taken.Contains(name);
}
