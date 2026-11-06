using System.Text.RegularExpressions;

namespace Trailblazor.Testing.Playwright;

internal static class FieldLabel
{
    // A Required field's label text ends in an asterisk, and unlike a string, a regex sees the markup's raw whitespace.
    public static Regex Matching(string label) =>
        new($@"^\s*{Regex.Escape(label).Replace(@"\ ", @"\s+")}(\s*\*)?\s*$");
}
