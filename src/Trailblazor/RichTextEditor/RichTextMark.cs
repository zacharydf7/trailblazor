using System.Text.RegularExpressions;

namespace Trailblazor;

/// <summary>A character format of the app's own for a <see cref="TbRichTextEditor"/>, such as a keyboard key or a
/// spoiler, with its toolbar button. Its name says how it is written in the value: <c>cite</c>, <c>dfn</c>,
/// <c>kbd</c>, <c>q</c>, <c>samp</c> and <c>var</c> are written as those elements, and any other name as
/// <c>&lt;span data-tb-mark="name"&gt;</c>, which the app's style sheet gives a look. Text has a mark or it doesn't:
/// a mark carries no value.</summary>
public sealed partial class RichTextMark
{
    // The names written as an element of their own. A browser's parser reopens none of them in the next paragraph,
    // so each reads back as it was written.
    private static readonly string[] Elements = ["cite", "dfn", "kbd", "q", "samp", "var"];

    // The script keeps the editor's own formats under these names, beside the app's marks.
    private static readonly string[] Taken =
    [
        "bold", "italic", "underline", "strikethrough", "subscript", "superscript", "code", "highlight", "color",
        "link",
    ];

    /// <summary>Creates a mark.</summary>
    /// <param name="name">The mark's name, which is also how it is written: lowercase letters, digits and hyphens,
    /// starting with a letter, and at most 32 characters. It can't be the name of one of the editor's own formats,
    /// such as <c>bold</c>.</param>
    /// <exception cref="ArgumentException">The name isn't one a mark can have.</exception>
    public RichTextMark(string name) => Name = IsName(name) ? name
        : throw new ArgumentException($"\"{name}\" can't name a mark: a name is lowercase letters, digits and hyphens, "
            + "starts with a letter, has at most 32 characters, and isn't one of the editor's own formats.",
            nameof(name));

    /// <summary>The mark's name, which a <see cref="RichTextRun"/> with the mark lists in its
    /// <see cref="RichTextRun.Marks"/>.</summary>
    public string Name { get; }

    /// <summary>The name of the mark's toolbar button. Defaults to <see cref="Name"/>.</summary>
    public string? Label { get; init; }

    /// <summary>The icon of the mark's toolbar button. Defaults to the first letter of <see cref="Label"/>, or of
    /// <see cref="Name"/> without one.</summary>
    public Icon? Icon { get; init; }

    /// <summary>The keys that turn the mark on or off, written as <c>aria-keyshortcuts</c> are: a key with
    /// <c>Control</c>, <c>Meta</c>, <c>Alt</c> or <c>Shift</c> before it and <c>+</c> between, and a space between two
    /// ways to press it, such as <c>"Control+Shift+K Meta+Shift+K"</c>. Pick keys the editor and the browser don't
    /// use.</summary>
    public string? Shortcut { get; init; }

    /// <summary>Whether the mark also has a button on the toolbar over the selection, with
    /// <see cref="TbRichTextEditor.ShowFloatingToolbar"/>. Defaults to true.</summary>
    public bool Floating { get; init; } = true;

    internal static bool IsName(string? name) => IsPlain(name) && !Taken.Contains(name);

    // The shape of a name that goes into the HTML as a tag or an attribute's value, for a mark or a block.
    internal static bool IsPlain([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? name) =>
        name is not null && NamePattern().IsMatch(name);

    internal static bool IsElement(string name) => Elements.Contains(name);

    // What a mark's text is put in: the text after "<" that opens it, and the element's name, which closes it.
    internal static (string Open, string Close) TagOf(string name) =>
        IsElement(name) ? (name, name) : ($"span data-tb-mark=\"{name}\"", "span");

    // A run's marks as it keeps them: each name once, sorted by name, with a space between.
    internal static string Join(IEnumerable<string>? marks)
    {
        var names = (marks ?? []).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        return names.TrueForAll(IsName) ? string.Join(' ', names)
            : throw new ArgumentException("Each mark must be a name a RichTextMark can have.", nameof(marks));
    }

    [GeneratedRegex("^[a-z][a-z0-9-]{0,31}$")]
    private static partial Regex NamePattern();
}
