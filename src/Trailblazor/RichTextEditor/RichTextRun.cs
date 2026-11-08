namespace Trailblazor;

/// <summary>A stretch of text in a <see cref="RichTextParagraph"/> that has one set of formats and at most one link.
/// </summary>
public sealed record RichTextRun
{
    /// <summary>Creates a run.</summary>
    /// <param name="text">The text. A <c>'\n'</c> in it is a line break, and so is <c>"\r\n"</c>. Tabs, form feeds,
    /// no-break spaces and any other <c>'\r'</c> become plain spaces.</param>
    /// <param name="formats">The formats the text has. <see cref="RichTextFormats.Link"/> comes from
    /// <paramref name="link"/>, so it can't be given here without one.</param>
    /// <param name="link">The URL the text links to, or null for none. As in a browser, tabs and line breaks in it and
    /// spaces and controls around it are dropped, and a link left empty is none.</param>
    /// <exception cref="ArgumentException">The formats are both subscript and superscript, or they have
    /// <see cref="RichTextFormats.Link"/> without a link, or the link is to <c>javascript:</c> or
    /// <c>vbscript:</c>, which would run script when followed.</exception>
    public RichTextRun(string text, RichTextFormats formats = RichTextFormats.None, string? link = null)
        : this(text, formats, link, vetted: false)
    {
    }

    // The reader and paragraphs pass links already cleaned and checked, so a long link isn't scanned again per run.
    internal RichTextRun(string text, RichTextFormats formats, string? link, bool vetted)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (formats.HasFlag(RichTextFormats.Subscript | RichTextFormats.Superscript))
        {
            throw new ArgumentException("Text can't be both subscript and superscript.", nameof(formats));
        }

        if (!vetted)
        {
            link = RichTextSchema.CleanLink(link);
            if (link is not null && RichTextSchema.RunsScript(RichTextSchema.SchemeOf(link)))
            {
                throw new ArgumentException("A link can't run script.", nameof(link));
            }
        }

        if (formats.HasFlag(RichTextFormats.Link) && link is null)
        {
            throw new ArgumentException("A run with the Link format needs a link.", nameof(formats));
        }

        Text = text.Replace("\r\n", "\n").Replace('\r', ' ').Replace('\t', ' ').Replace('\f', ' ')
            .Replace('\u00A0', ' ');
        Formats = link is null ? formats : formats | RichTextFormats.Link;
        Link = link;
    }

    /// <summary>The text, with <c>'\n'</c> for each line break. Tabs, form feeds, carriage returns and no-break
    /// spaces in it are already plain spaces.</summary>
    public string Text { get; }

    /// <summary>The formats the text has, with <see cref="RichTextFormats.Link"/> when it has a link.</summary>
    public RichTextFormats Formats { get; }

    /// <summary>The URL the text links to, or null for none.</summary>
    public string? Link { get; }
}
