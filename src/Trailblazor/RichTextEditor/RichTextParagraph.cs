using System.Text;

namespace Trailblazor;

/// <summary>A paragraph of a <see cref="RichTextDocument"/>: its text as runs, each with one set of formats.</summary>
public sealed class RichTextParagraph
{
    /// <summary>Creates a paragraph from its runs. Empty runs are dropped, and neighbors with the same formats are
    /// joined, so the paragraph keeps the fewest runs that say the same thing.</summary>
    /// <param name="runs">The runs, in order.</param>
    public RichTextParagraph(IEnumerable<RichTextRun> runs)
    {
        ArgumentNullException.ThrowIfNull(runs);
        var joined = new List<RichTextRun>();
        var text = new StringBuilder();
        RichTextFormats? formats = null;
        foreach (var run in runs.Where(run => run.Text.Length > 0))
        {
            if (run.Formats != formats)
            {
                Flush();
                formats = run.Formats;
            }

            text.Append(run.Text);
        }

        Flush();
        Runs = joined;
        Text = string.Concat(joined.Select(run => run.Text));

        void Flush()
        {
            if (formats is { } current)
            {
                joined.Add(new RichTextRun(text.ToString(), current));
                text.Clear();
            }
        }
    }

    /// <summary>The runs, in order. An empty paragraph has none.</summary>
    public IReadOnlyList<RichTextRun> Runs { get; }

    /// <summary>The paragraph's text without formats. A <c>'\n'</c> in it is a line break.</summary>
    public string Text { get; }
}
