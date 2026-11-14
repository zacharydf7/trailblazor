using System.Text.RegularExpressions;

namespace Trailblazor.Testing.Playwright;

internal static class FieldLabel
{
    // A Required label ends in an asterisk, which the caller may also pass, spaced or not; a regex sees raw whitespace.
    public static Regex Matching(string label) =>
        new($@"^\s*{Regex.Escape(label.TrimEnd().TrimEnd('*').TrimEnd()).Replace(@"\ ", @"\s+")}(\s*\*)?\s*$");
}
