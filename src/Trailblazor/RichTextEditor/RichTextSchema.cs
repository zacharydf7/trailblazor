namespace Trailblazor;

/// <summary>What a <see cref="RichTextDocument"/> may keep when it reads HTML: its formats, its kinds of block, the
/// URL schemes its links and images may use, and whether it keeps mentions. A format it doesn't allow is dropped
/// while its text stays, a block it doesn't allow gives way to an allowed block around it or a plain paragraph, a
/// rule or image it doesn't allow is dropped, and a mention it doesn't allow is plain text.</summary>
public sealed record RichTextSchema
{
    /// <summary>Every format and every block but images and tables, links that use <c>http</c>, <c>https</c>,
    /// <c>mailto</c> or <c>tel</c>, and no mentions.</summary>
    public static RichTextSchema Default { get; } = new();

    /// <summary>The formats to keep. Without <see cref="RichTextFormats.Link"/>, every link becomes plain text.
    /// Defaults to all of them.</summary>
    public RichTextFormats Formats { get; init; } = RichTextHtmlReader.AllFormats;

    /// <summary>The kinds of block to keep. Defaults to all of them but <see cref="RichTextBlocks.Images"/> and
    /// <see cref="RichTextBlocks.Tables"/>.</summary>
    public RichTextBlocks Blocks { get; init; } = RichTextHtmlReader.AllBlocks;

    /// <summary>The URL schemes a link may use, such as <c>https</c>. A relative URL, which has no scheme, is always
    /// allowed. A link with any other scheme becomes plain text, and so does one to <c>javascript:</c> or
    /// <c>vbscript:</c> even when listed here, since following it would run script.</summary>
    public IReadOnlyList<string> LinkSchemes { get; init; } = ["http", "https", "mailto", "tel"];

    /// <summary>The URL schemes an image's source may use, when <see cref="Blocks"/> has
    /// <see cref="RichTextBlocks.Images"/>. A relative URL, which has no scheme, is always allowed. An image with any
    /// other scheme is dropped, and so is one with a <c>javascript:</c> or <c>vbscript:</c> source even when listed
    /// here. Defaults to <c>https</c> and <c>http</c>.</summary>
    public IReadOnlyList<string> ImageSchemes { get; init; } = ["https", "http"];

    /// <summary>Whether to keep mentions: a <c>span</c> with a <c>data-mention</c> attribute then reads as one run
    /// that is a <see cref="Mention"/>, and otherwise as its text. Defaults to false, since other editors write that
    /// attribute too, with ids of their own.</summary>
    public bool Mentions { get; init; }

    /// <summary>Whether this schema keeps the same formats, blocks, link schemes, image schemes and mentions as
    /// another, with the schemes listed in the same order; schemes compare without regard to case.</summary>
    /// <param name="other">The schema to compare with.</param>
    public bool Equals(RichTextSchema? other) => other is not null && Formats == other.Formats
        && Blocks == other.Blocks && LinkSchemes.SequenceEqual(other.LinkSchemes, StringComparer.OrdinalIgnoreCase)
        && ImageSchemes.SequenceEqual(other.ImageSchemes, StringComparer.OrdinalIgnoreCase)
        && Mentions == other.Mentions;

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Formats, Blocks, LinkSchemes.Count, ImageSchemes.Count, Mentions);

    internal bool AllowsLink(string url) => Allows(LinkSchemes, url);

    internal bool AllowsImage(string url) => Allows(ImageSchemes, url);

    private static bool Allows(IReadOnlyList<string> schemes, string url) => SchemeOf(url) switch
    {
        null => true,
        "" => false,
        var scheme => schemes.Contains(scheme, StringComparer.OrdinalIgnoreCase) && !RunsScript(scheme),
    };

    internal static bool RunsScript(string? scheme) =>
        scheme is not null && (scheme.Equals("javascript", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("vbscript", StringComparison.OrdinalIgnoreCase));

    // As a browser reads a URL: without the tabs and line breaks inside it or the spaces and controls around it.
    internal static string? CleanLink(string? url)
    {
        var cleaned = url?.Replace("\t", "").Replace("\n", "").Replace("\r", "")
            .Trim(Enumerable.Range(0, 33).Select(code => (char)code).ToArray());
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }

    // Null for a relative URL (no ":" before any "/", "?" or "#"); "" when what precedes the ":" isn't a valid scheme.
    internal static string? SchemeOf(string url)
    {
        var colon = url.IndexOf(':');
        var path = url.IndexOfAny(['/', '?', '#']);
        if (colon < 0 || (path >= 0 && path < colon))
        {
            return null;
        }

        var scheme = url[..colon];
        return scheme.Length > 0 && char.IsAsciiLetter(scheme[0])
            && scheme.All(character => char.IsAsciiLetterOrDigit(character) || character is '+' or '-' or '.')
            ? scheme
            : "";
    }
}
