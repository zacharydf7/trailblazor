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
    public string Title => UpgradeCatalog.Titled(Date);
}

/// <summary>The changes to markup and behavior made on one date, such as a part or attribute that moved, each written
/// as a sentence with names in backticks.</summary>
public sealed record BehaviorChanges(string Date, IReadOnlyList<string> Changes)
{
    /// <summary>The id of the date's heading on the Upgrading page.</summary>
    public string Id => $"behavior-{Date}";

    /// <summary>The date as the page shows it, such as "November 14, 2026".</summary>
    public string Title => UpgradeCatalog.Titled(Date);

    /// <summary>The date with its section's name, such as "Markup and behavior, November 14, 2026", for links and
    /// search results, which would otherwise show the same date as a "Renamed and removed" heading.</summary>
    public string Section => $"Markup and behavior, {Title}";
}

// What CHANGELOG.md lists under "### Renamed and removed" and "### Markup and behavior", newest first: each
// "#### date" heading starts a date, each "- " line under it is a change, and the next "##" or "###" heading ends the
// section. A rename is written "- Before → Now".
public static class UpgradeCatalog
{
    // Read before the lists below, which static initializers build in the order they're written.
    private static readonly Dictionary<string, List<Dated>> Sections = Load();

    public static IReadOnlyList<ApiChanges> All { get; } = [.. Section("Renamed and removed").Select(date =>
        new ApiChanges(date.Date, [.. date.Lines.Select(Renamed)]))];

    public static IReadOnlyList<BehaviorChanges> Behavior { get; } =
        [.. Section("Markup and behavior").Select(date => new BehaviorChanges(date.Date, date.Lines))];

    internal static string Titled(string date) =>
        DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture)
            .ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    private static List<Dated> Section(string name) =>
        Sections.TryGetValue(name, out var dates) ? dates
            : throw new InvalidOperationException($"CHANGELOG.md has no \"### {name}\" section.");

    private static ApiChange Renamed(string line)
    {
        var parts = line.Split(" → ", 2);
        return parts.Length == 2 ? new(parts[0], parts[1]) : throw new InvalidOperationException(
            $"CHANGELOG.md's line \"- {line}\" has no \" → \" between its names.");
    }

    private static Dictionary<string, List<Dated>> Load()
    {
        using var stream = typeof(UpgradeCatalog).Assembly.GetManifestResourceStream("Changelog/CHANGELOG.md")
            ?? throw new InvalidOperationException("CHANGELOG.md isn't embedded.");
        var sections = new Dictionary<string, List<Dated>>();
        List<Dated>? dates = null;
        foreach (var line in new StreamReader(stream).ReadToEnd().Split('\n').Select(line => line.TrimEnd('\r')))
        {
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                sections.Add(line[4..], dates = []);
            }
            else if (line.StartsWith("#### ", StringComparison.Ordinal) && dates is not null)
            {
                dates.Add(new(line[5..], []));
            }
            else if (line.StartsWith('#'))
            {
                dates = null;
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) && dates is [.., var date])
            {
                // Markdown would read a bare < as the start of a tag, so the changelog writes \<.
                date.Lines.Add(line[2..].Replace(@"\<", "<", StringComparison.Ordinal));
            }
        }

        return sections;
    }

    private sealed record Dated(string Date, List<string> Lines);
}
