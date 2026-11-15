namespace Trailblazor;

/// <summary>The image a <see cref="RichTextParagraph"/> of kind <see cref="RichTextParagraphKind.Image"/> shows: its
/// source and its alt text.</summary>
public sealed record RichTextImage
{
    /// <summary>Creates an image.</summary>
    /// <param name="source">The image's URL. As in a browser, tabs and line breaks in it and spaces and controls around
    /// it are dropped.</param>
    /// <param name="alt">The text that stands for the image, <c>""</c> for a decorative image, or <c>null</c> when none
    /// was given. Each line break, tab or other control character in it becomes a plain space, and so does
    /// <c>"\r\n"</c>.</param>
    /// <exception cref="ArgumentException">The source is empty, or it's a <c>javascript:</c> or <c>vbscript:</c> URL,
    /// which would run script.</exception>
    public RichTextImage(string source, string? alt)
    {
        ArgumentNullException.ThrowIfNull(source);
        var cleaned = RichTextSchema.CleanLink(source)
            ?? throw new ArgumentException("An image needs a source.", nameof(source));
        if (RichTextSchema.RunsScript(RichTextSchema.SchemeOf(cleaned)))
        {
            throw new ArgumentException("An image source can't run script.", nameof(source));
        }

        Source = cleaned;
        Alt = alt is null ? null
            : new string(alt.Replace("\r\n", "\n").Select(character => char.IsControl(character) ? ' ' : character)
                .ToArray());
    }

    /// <summary>The image's URL.</summary>
    public string Source { get; }

    /// <summary>The text that stands for the image, without line breaks; <c>""</c> for a decorative image, and
    /// <c>null</c> when none was given.</summary>
    public string? Alt { get; }
}
