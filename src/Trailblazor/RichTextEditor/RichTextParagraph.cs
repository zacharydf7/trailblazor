using System.Text;

namespace Trailblazor;

/// <summary>A paragraph of a <see cref="RichTextDocument"/>: what kind of block it is, and either its text as runs,
/// each with one set of formats, or the image it shows.</summary>
public sealed class RichTextParagraph
{
    /// <summary>Creates a paragraph from its runs. Empty runs are dropped, and neighbors with the same formats and
    /// link are joined, so the paragraph keeps the fewest runs that say the same thing.</summary>
    /// <param name="runs">The runs, in order. A rule has none.</param>
    /// <param name="kind">What kind of block the paragraph is. An image needs
    /// <see cref="RichTextParagraph(RichTextImage)"/> instead.</param>
    /// <param name="level">How far a list item is nested, from 0 for the outermost list. Other kinds have level 0.
    /// </param>
    /// <exception cref="ArgumentException">A rule is given runs with text, or the kind is
    /// <see cref="RichTextParagraphKind.Image"/>, which needs an image.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The level is negative, or not 0 for a paragraph that isn't a
    /// list item.</exception>
    public RichTextParagraph(IEnumerable<RichTextRun> runs,
        RichTextParagraphKind kind = RichTextParagraphKind.Paragraph, int level = 0)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentOutOfRangeException.ThrowIfNegative(level);
        if (kind == RichTextParagraphKind.Image)
        {
            throw new ArgumentException("An image paragraph needs an image.", nameof(kind));
        }

        if (level > 0 && !IsListItem(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Only a list item can be nested.");
        }

        var joined = new List<RichTextRun>();
        var text = new StringBuilder();
        RichTextRun? style = null;
        foreach (var run in runs.Where(run => run.Text.Length > 0))
        {
            if (run.Formats != style?.Formats || run.Link != style?.Link)
            {
                Flush();
                style = run;
            }

            text.Append(run.Text);
        }

        Flush();
        if (kind == RichTextParagraphKind.Rule && joined.Count > 0)
        {
            throw new ArgumentException("A rule has no text.", nameof(runs));
        }

        Runs = joined;
        Text = string.Concat(joined.Select(run => run.Text));
        Kind = kind;
        Level = level;

        void Flush()
        {
            if (style is not null)
            {
                joined.Add(new RichTextRun(text.ToString(), style.Formats, style.Link, vetted: true));
                text.Clear();
            }
        }
    }

    /// <summary>Creates an image paragraph, of kind <see cref="RichTextParagraphKind.Image"/> with no runs.</summary>
    /// <param name="image">The image it shows.</param>
    public RichTextParagraph(RichTextImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        Runs = [];
        Text = "";
        Kind = RichTextParagraphKind.Image;
        Image = image;
    }

    /// <summary>The runs, in order. An empty paragraph has none.</summary>
    public IReadOnlyList<RichTextRun> Runs { get; }

    /// <summary>The paragraph's text without formats. A <c>'\n'</c> in it is a line break.</summary>
    public string Text { get; }

    /// <summary>What kind of block the paragraph is.</summary>
    public RichTextParagraphKind Kind { get; }

    /// <summary>How far a list item is nested, from 0 for the outermost list; 0 for every other kind.</summary>
    public int Level { get; }

    /// <summary>The image an image paragraph shows; null for every other kind.</summary>
    public RichTextImage? Image { get; }

    internal static bool IsListItem(RichTextParagraphKind kind) =>
        kind is RichTextParagraphKind.BulletedItem or RichTextParagraphKind.NumberedItem;
}
