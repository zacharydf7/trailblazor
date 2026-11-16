namespace Trailblazor.Docs.Examples;

// The body of one method in a recipe's source, as the docs show it: the testing comparison's cells are methods the
// tests run, so the code on the page is the code that ran.
public static class RecipeSnippet
{
    // The body of the method named member in the class named type, without its braces or the expression body's arrow,
    // and with the method's indentation taken off.
    public static string Of(string file, string type, string member)
    {
        var lines = ExampleSource.OfRecipeFile(file).Text.Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        // The member is looked for only within its class, up to the next class in the file.
        var start = lines.FindIndex(line => line.Contains($" class {type}", StringComparison.Ordinal));
        var next = start < 0 ? -1
            : lines.FindIndex(start + 1, line => line.Contains(" class ", StringComparison.Ordinal));
        var signature = start < 0 ? -1 : lines.FindIndex(start, (next < 0 ? lines.Count : next) - start,
            line => line.Contains($" {member}(", StringComparison.Ordinal));
        if (signature < 0)
        {
            throw new InvalidOperationException($"{file} has no {type}.{member}.");
        }

        var open = lines.FindIndex(signature,
            line => line.Trim() == "{" || line.Contains("=>", StringComparison.Ordinal));
        if (lines[open].Trim() == "{")
        {
            var indent = lines[open].Length - lines[open].TrimStart().Length;
            var close = lines.FindIndex(open + 1, line => line.TrimEnd() == new string(' ', indent) + "}");
            return string.Join('\n', Dedented(lines[(open + 1)..close]));
        }

        // An expression body runs from its arrow to the line that ends it. One that starts on the arrow's line keeps
        // its continuation lines indented under it.
        var last = lines.FindIndex(open, line => line.TrimEnd().EndsWith(';'));
        var first = lines[open][(lines[open].IndexOf("=>", StringComparison.Ordinal) + 2)..].Trim();
        var rest = Dedented(lines[(open + 1)..(last + 1)]);
        return string.Join('\n', first.Length == 0 ? rest : [first, .. rest.Select(line => $"    {line}")]);
    }

    private static List<string> Dedented(List<string> lines)
    {
        var margin = lines.Where(line => line.Trim().Length > 0).Select(line => line.Length - line.TrimStart().Length)
            .DefaultIfEmpty(0).Min();
        return [.. lines.Select(line => line.Length < margin ? line.Trim() : line[margin..])];
    }
}
