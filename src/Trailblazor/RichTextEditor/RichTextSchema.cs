namespace Trailblazor;

/// <summary>What a <see cref="RichTextDocument"/> may keep when it reads HTML: its formats, its kinds of block and the
/// URL schemes its links may use. A format it doesn't allow is dropped while its text stays, a block it doesn't allow
/// gives way to an allowed block around it or a plain paragraph, and a rule it doesn't allow is dropped.</summary>
public sealed record RichTextSchema
{
    /// <summary>Every format and block, and links that use <c>http</c>, <c>https</c>, <c>mailto</c> or <c>tel</c>.
    /// </summary>
    public static RichTextSchema Default { get; } = new();

    /// <summary>The formats to keep. Without <see cref="RichTextFormats.Link"/>, every link becomes plain text.
    /// Defaults to all of them.</summary>
    public RichTextFormats Formats { get; init; } = RichTextHtmlReader.AllFormats;

    /// <summary>The kinds of block to keep. Defaults to all of them.</summary>
    public RichTextBlocks Blocks { get; init; } = RichTextHtmlReader.AllBlocks;

    /// <summary>The URL schemes a link may use, such as <c>https</c>. A relative URL, which has no scheme, is always
    /// allowed. A link with any other scheme becomes plain text, and so does one to <c>javascript:</c> or
    /// <c>vbscript:</c> even when listed here, since following it would run script.</summary>
    public IReadOnlyList<string> LinkSchemes { get; init; } = ["http", "https", "mailto", "tel"];

    /// <summary>Whether this schema keeps the same formats, blocks and link schemes as another, listed in the same
    /// order; schemes compare without regard to case.</summary>
    /// <param name="other">The schema to compare with.</param>
    public bool Equals(RichTextSchema? other) => other is not null && Formats == other.Formats
        && Blocks == other.Blocks && LinkSchemes.SequenceEqual(other.LinkSchemes, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Formats, Blocks, LinkSchemes.Count);

    internal bool AllowsLink(string url) => SchemeOf(url) switch
    {
        null => true,
        "" => false,
        var scheme => LinkSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase) && !RunsScript(scheme),
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
