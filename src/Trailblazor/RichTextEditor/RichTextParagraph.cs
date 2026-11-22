using System.Text;

namespace Trailblazor;

/// <summary>A paragraph of a <see cref="RichTextDocument"/>: what kind of block it is, and either its text as runs,
/// each with one set of formats, or the image it shows. A table cell is a paragraph too.</summary>
public sealed class RichTextParagraph
{
    /// <summary>Creates a paragraph from its runs. Empty runs are dropped, and neighbors with the same formats and
    /// link are joined, so the paragraph keeps the fewest runs that say the same thing. A mention stays a run of its
    /// own.</summary>
    /// <param name="runs">The runs, in order. A rule has none.</param>
    /// <param name="kind">What kind of block the paragraph is. An image needs
    /// <see cref="RichTextParagraph(RichTextImage)"/> instead, and a table cell
    /// <see cref="RichTextParagraph(IEnumerable{RichTextRun}, RichTextTableCell)"/>.</param>
    /// <param name="level">How far a list item is nested, from 0 for the outermost list. Other kinds have level 0.
    /// </param>
    /// <exception cref="ArgumentException">A rule is given runs with text, or the kind is
    /// <see cref="RichTextParagraphKind.Image"/> or <see cref="RichTextParagraphKind.TableCell"/>, which need an image
    /// or a cell.</exception>
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

        if (kind == RichTextParagraphKind.TableCell)
        {
            throw new ArgumentException("A table cell needs its place in the table.", nameof(kind));
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
            if (run.Mention is not null)
            {
                Flush();
                joined.Add(run);
                style = null;
                continue;
            }

            if (run.Formats != style?.Formats || run.Link != style?.Link || run.Color != style?.Color)
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
                joined.Add(new RichTextRun(text.ToString(), style.Formats, style.Link, style.Color, vetted: true));
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

    /// <summary>Creates a table cell, of kind <see cref="RichTextParagraphKind.TableCell"/>. Its runs are joined as in
    /// <see cref="RichTextParagraph(IEnumerable{RichTextRun}, RichTextParagraphKind, int)"/>.</summary>
    /// <param name="runs">The cell's runs, in order. An empty cell has none.</param>
    /// <param name="cell">Where the cell sits in its table.</param>
    public RichTextParagraph(IEnumerable<RichTextRun> runs, RichTextTableCell cell) : this(runs)
    {
        ArgumentNullException.ThrowIfNull(cell);
        Kind = RichTextParagraphKind.TableCell;
        Cell = cell;
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

    /// <summary>Where a table cell sits in its table; null for every other kind.</summary>
    public RichTextTableCell? Cell { get; }

    internal static bool IsListItem(RichTextParagraphKind kind) =>
        kind is RichTextParagraphKind.BulletedItem or RichTextParagraphKind.NumberedItem;
}
