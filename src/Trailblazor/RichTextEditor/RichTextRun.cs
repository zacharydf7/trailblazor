namespace Trailblazor;

/// <summary>A stretch of text in a <see cref="RichTextParagraph"/> that has one set of formats and at most one link.
/// A run can instead be a mention: one unit of text with formats but no link.</summary>
public sealed record RichTextRun
{
    /// <summary>Creates a run.</summary>
    /// <param name="text">The text. A <c>'\n'</c> in it is a line break, and so is <c>"\r\n"</c>. Tabs, form feeds,
    /// no-break spaces and any other <c>'\r'</c> become plain spaces.</param>
    /// <param name="formats">The formats the text has. <see cref="RichTextFormats.Link"/> comes from
    /// <paramref name="link"/>, so it can't be given here without one.</param>
    /// <param name="link">The URL the text links to, or null for none. As in a browser, tabs and line breaks in it and
    /// spaces and controls around it are dropped, and a link left empty is none.</param>
    /// <param name="color">The text's color, one of the design system's tones, or null for the color of the text
    /// around it. <see cref="RichTextFormats.Color"/> comes from it, so it can't be given in
    /// <paramref name="formats"/> without one.</param>
    /// <param name="marks">The names of the app's own marks the text has, each a <see cref="RichTextMark"/>'s
    /// name, or null for none.</param>
    /// <exception cref="ArgumentException">The formats are both subscript and superscript, or they have
    /// <see cref="RichTextFormats.Link"/> without a link or <see cref="RichTextFormats.Color"/> without a color,
    /// or the link is to <c>javascript:</c> or <c>vbscript:</c>, which would run script when followed, or a
    /// mark's name isn't one a <see cref="RichTextMark"/> can have.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The color isn't one of the <see cref="Tone"/> values.
    /// </exception>
    public RichTextRun(string text, RichTextFormats formats = RichTextFormats.None, string? link = null,
        Tone? color = null, IEnumerable<string>? marks = null)
        : this(text, formats, link, color, vetted: false, RichTextMark.Join(marks))
    {
    }

    /// <summary>Creates a run that is a mention: one unit of text that carries the app's id for the person or thing
    /// mentioned.</summary>
    /// <param name="mention">The mention. The run's text is its trigger followed by its item's text, each cleaned as
    /// a run's text is, with line breaks as spaces too. The item's description isn't kept, and an empty id is none.
    /// </param>
    public RichTextRun(Mention mention) : this(mention, RichTextFormats.None)
    {
    }

    /// <summary>Creates a run that is a mention with formats.</summary>
    /// <param name="mention">The mention, as in <see cref="RichTextRun(Mention)"/>.</param>
    /// <param name="formats">The formats the text has. A mention can't be a link.</param>
    /// <exception cref="ArgumentException">The formats are both subscript and superscript, or they have
    /// <see cref="RichTextFormats.Link"/> or <see cref="RichTextFormats.Color"/>.</exception>
    public RichTextRun(Mention mention, RichTextFormats formats) : this(mention, formats, color: null)
    {
    }

    /// <summary>Creates a run that is a mention with formats and a text color.</summary>
    /// <param name="mention">The mention, as in <see cref="RichTextRun(Mention)"/>.</param>
    /// <param name="formats">The formats the text has. A mention can't be a link.</param>
    /// <param name="color">The text's color, or null for the color of the text around it.</param>
    /// <exception cref="ArgumentException">The formats are both subscript and superscript, or they have
    /// <see cref="RichTextFormats.Link"/>, or <see cref="RichTextFormats.Color"/> without a color.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The color isn't one of the <see cref="Tone"/> values.
    /// </exception>
    public RichTextRun(Mention mention, RichTextFormats formats, Tone? color) : this(mention, formats, color, [])
    {
    }

    /// <summary>Creates a run that is a mention with formats, a text color and marks of the app's own.</summary>
    /// <param name="mention">The mention, as in <see cref="RichTextRun(Mention)"/>.</param>
    /// <param name="formats">The formats the text has. A mention can't be a link.</param>
    /// <param name="color">The text's color, or null for the color of the text around it.</param>
    /// <param name="marks">The names of the app's own marks the text has, each a <see cref="RichTextMark"/>'s
    /// name.</param>
    /// <exception cref="ArgumentException">The formats are both subscript and superscript, or they have
    /// <see cref="RichTextFormats.Link"/>, or <see cref="RichTextFormats.Color"/> without a color, or a mark's
    /// name isn't one a <see cref="RichTextMark"/> can have.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The color isn't one of the <see cref="Tone"/> values.
    /// </exception>
    public RichTextRun(Mention mention, RichTextFormats formats, Tone? color, IEnumerable<string> marks)
        : this(Cleaned(mention).Trigger + Cleaned(mention).Item.Text, formats, link: null, color, vetted: true,
            RichTextMark.Join(marks))
    {
        Mention = Cleaned(mention);
    }

    // The reader and paragraphs pass links already cleaned and checked, and marks already joined, so a long link
    // isn't scanned again per run.
    internal RichTextRun(string text, RichTextFormats formats, string? link, Tone? color, bool vetted,
        string marks = "")
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

        if (formats.HasFlag(RichTextFormats.Color) && color is null)
        {
            throw new ArgumentException("A run with the Color format needs a color.", nameof(formats));
        }

        if (color is { } tone && !Enum.IsDefined(tone))
        {
            throw new ArgumentOutOfRangeException(nameof(color), color, "The color must be one of the Tone values.");
        }

        Text = text.Replace("\r\n", "\n").Replace('\r', ' ').Replace('\t', ' ').Replace('\f', ' ')
            .Replace('\u00A0', ' ');
        Formats = formats | (link is null ? 0 : RichTextFormats.Link) | (color is null ? 0 : RichTextFormats.Color);
        Link = link;
        Color = color;
        MarkNames = marks;
    }

    /// <summary>The text, with <c>'\n'</c> for each line break. Tabs, form feeds, carriage returns and no-break
    /// spaces in it are already plain spaces.</summary>
    public string Text { get; }

    /// <summary>The formats the text has, with <see cref="RichTextFormats.Link"/> when it has a link and
    /// <see cref="RichTextFormats.Color"/> when it has a color.</summary>
    public RichTextFormats Formats { get; }

    /// <summary>The URL the text links to, or null for none.</summary>
    public string? Link { get; }

    /// <summary>The text's color, one of the design system's tones, or null for the color of the text around it.
    /// In the HTML it is a <c>&lt;span data-tb-color&gt;</c> with the tone's name in lowercase, which the
    /// library's style sheet colors.</summary>
    public Tone? Color { get; }

    /// <summary>The mention the run is, or null for ordinary text. A mention's <see cref="Text"/> is its trigger
    /// followed by its item's text.</summary>
    public Mention? Mention { get; }

    /// <summary>The names of the app's own marks the text has, each a <see cref="RichTextMark"/>'s name, sorted by
    /// name.</summary>
    public IReadOnlyList<string> Marks => MarkNames.Length == 0 ? [] : MarkNames.Split(' ');

    // The marks as one string, so that two runs with the same marks are equal.
    internal string MarkNames { get; }

    private static readonly Dictionary<string, Tone> Colors =
        Enum.GetValues<Tone>().ToDictionary(ColorName, StringComparer.Ordinal);

    // A color is written by its tone's name in lowercase, as in data-tb-color="danger", and read back only so.
    internal static string ColorName(Tone color) => color.ToString().ToLowerInvariant();

    internal static Tone? ColorOf(string? name) =>
        name is not null && Colors.TryGetValue(name, out var color) ? color : null;

    private static Mention Cleaned(Mention mention)
    {
        ArgumentNullException.ThrowIfNull(mention);
        return new(Clean(mention.Trigger), new MentionItem(Clean(mention.Item.Text))
        {
            Id = string.IsNullOrEmpty(mention.Item.Id) ? null : mention.Item.Id,
        });
    }

    private static string Clean(string text) => text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ')
        .Replace('\f', ' ').Replace('\u00A0', ' ');
}
