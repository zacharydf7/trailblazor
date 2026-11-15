using System.Globalization;

namespace Trailblazor.Docs.Examples;

/// <summary>A public name that changed or went away, and what to use in its place.</summary>
public sealed record ApiChange(string Old, string New);

/// <summary>The public names changed on one date, before the first release.</summary>
public sealed record ApiChanges(string Date, IReadOnlyList<ApiChange> Changes)
{
    /// <summary>The id of the date's heading on the Upgrading page.</summary>
    public string Id => $"changes-{Date}";

    /// <summary>The date as the page shows it, such as "November 14, 2026".</summary>
    public string Title => DateOnly.ParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture)
        .ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
}

// The renames and removals CHANGELOG.md lists, newest first: each "#### date" heading starts a date, each
// "- Before → Now" line under it is a change, and the next "##" or "###" heading ends the list.
public static class UpgradeCatalog
{
    private static readonly Lazy<IReadOnlyList<ApiChanges>> Changes = new(Load);

    public static IReadOnlyList<ApiChanges> All => Changes.Value;

    private static IReadOnlyList<ApiChanges> Load()
    {
        using var stream = typeof(UpgradeCatalog).Assembly.GetManifestResourceStream("Changelog/CHANGELOG.md")
            ?? throw new InvalidOperationException("CHANGELOG.md isn't embedded.");
        List<(string Date, List<ApiChange> Changes)> dates = [];
        foreach (var line in new StreamReader(stream).ReadToEnd().Split('\n').Select(line => line.TrimEnd('\r')))
        {
            if (line.StartsWith("#### ", StringComparison.Ordinal))
            {
                dates.Add((line[5..], []));
            }
            else if (line.StartsWith('#') && dates.Count > 0)
            {
                break;
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) && dates.Count > 0)
            {
                // Markdown would read a bare < as the start of a tag, so the changelog writes \<.
                var parts = line[2..].Replace(@"\<", "<", StringComparison.Ordinal).Split(" → ", 2);
                if (parts.Length < 2)
                {
                    throw new InvalidOperationException(
                        $"CHANGELOG.md's line \"{line}\" has no \" → \" between its names.");
                }

                dates[^1].Changes.Add(new(parts[0], parts[1]));
            }
        }

        return [.. dates.Select(date => new ApiChanges(date.Date, date.Changes))];
    }
}
