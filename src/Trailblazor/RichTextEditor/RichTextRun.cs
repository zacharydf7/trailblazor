namespace Trailblazor;

/// <summary>A stretch of text in a <see cref="RichTextParagraph"/> that has one set of formats.</summary>
public sealed record RichTextRun
{
    /// <summary>Creates a run.</summary>
    /// <param name="text">The text. A <c>'\n'</c> in it is a line break, and so is <c>"\r\n"</c>. Tabs, form feeds,
    /// no-break spaces and any other <c>'\r'</c> become plain spaces.</param>
    /// <param name="formats">The formats the text has.</param>
    /// <exception cref="ArgumentException">The formats are both subscript and superscript.</exception>
    public RichTextRun(string text, RichTextFormats formats = RichTextFormats.None)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (formats.HasFlag(RichTextFormats.Subscript | RichTextFormats.Superscript))
        {
            throw new ArgumentException("Text can't be both subscript and superscript.", nameof(formats));
        }

        Text = text.Replace("\r\n", "\n").Replace('\r', ' ').Replace('\t', ' ').Replace('\f', ' ')
            .Replace('\u00A0', ' ');
        Formats = formats;
    }

    /// <summary>The text, with <c>'\n'</c> for each line break. Tabs, form feeds, carriage returns and no-break
    /// spaces in it are already plain spaces.</summary>
    public string Text { get; }

    /// <summary>The formats the text has.</summary>
    public RichTextFormats Formats { get; }
}
