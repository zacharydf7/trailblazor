using System.Text.RegularExpressions;

namespace Trailblazor.Docs.Examples;

public enum CodeLanguage
{
    Plain,
    Razor,
    CSharp,
    Css,
    Json,
    Shell,
}

// Splits code into runs of text, each with the kind of token it is, or none for plain text. Each language is one regex
// whose named groups are the kinds; the first group that matches at a position wins, so comments and strings come first.
public static partial class CodeHighlighter
{
    public static IEnumerable<(string Text, string? Kind)> Tokens(string code, CodeLanguage language)
    {
        // A component's @code block is C#, whose generics and lambdas would otherwise read as tags.
        if (language == CodeLanguage.Razor && CodeDirective().Match(code) is { Success: true } directive)
        {
            return [.. Tokens(code[..directive.Index], CodeLanguage.Razor), (directive.Value, "keyword"),
                .. Tokens(code[(directive.Index + directive.Length)..], CodeLanguage.CSharp)];
        }

        return Match(code, language);
    }

    private static IEnumerable<(string Text, string? Kind)> Match(string code, CodeLanguage language)
    {
        if (PatternOf(language) is not { } pattern)
        {
            yield return (code, null);
            yield break;
        }

        var at = 0;
        foreach (Match match in pattern.Matches(code))
        {
            if (match.Index > at)
            {
                yield return (code[at..match.Index], null);
            }

            yield return (match.Value, Kinds.First(kind => match.Groups[kind].Success));
            at = match.Index + match.Length;
        }

        if (at < code.Length)
        {
            yield return (code[at..], null);
        }
    }

    // The label's file extension, before any comma, names the language. A .cs label, or one without an extension, is
    // Razor when the code starts with markup or a directive, since a label like "App.razor and Program.cs" covers both.
    public static CodeLanguage LanguageOf(string label, string code) =>
        Path.GetExtension(label.Split(',')[0]).ToLowerInvariant() switch
        {
            ".razor" or ".html" or ".csproj" => CodeLanguage.Razor,
            ".css" => CodeLanguage.Css,
            ".json" => CodeLanguage.Json,
            ".sh" => CodeLanguage.Shell,
            _ => code.TrimStart() is ['<' or '@', ..] ? CodeLanguage.Razor : CodeLanguage.CSharp,
        };

    [GeneratedRegex(@"@(?:code|functions)\b")]
    private static partial Regex CodeDirective();

    private static readonly string[] Kinds = ["comment", "string", "tag", "attribute", "keyword", "number"];

    private static Regex? PatternOf(CodeLanguage language) => language switch
    {
        CodeLanguage.Razor => Razor(),
        CodeLanguage.CSharp => CSharp(),
        CodeLanguage.Css => Css(),
        CodeLanguage.Json => Json(),
        CodeLanguage.Shell => Shell(),
        _ => null,
    };

    private const string Keywords = @"\b(?:abstract|as|async|await|base|bool|break|case|catch|class|const|continue|"
        + "decimal|default|do|double|else|enum|false|finally|for|foreach|get|if|in|init|int|interface|internal|is|"
        + "long|namespace|new|null|object|out|override|params|partial|private|protected|public|readonly|record|"
        + "ref|required|return|sealed|set|static|string|struct|switch|this|throw|true|try|typeof|using|var|void|"
        + @"when|while|with|yield)\b";

    private const string Number = @"(?<number>\b\d+(?:\.\d+)?\b)";

    [GeneratedRegex(@"(?<comment>@\*[\s\S]*?\*@|<!--[\s\S]*?-->|//[^\n]*)"
        + @"|(?<string>""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)')"
        + @"|(?<tag></[A-Za-z][\w.:-]*|(?<![\w.])<[A-Za-z][\w.:-]*|(?<!=|\w<[\w.,\s]*)/?>)"
        + @"|(?<attribute>@?[A-Za-z][\w:.-]*(?==))"
        + @"|(?<keyword>@[A-Za-z_]\w*)")]
    private static partial Regex Razor();

    [GeneratedRegex(@"(?<comment>//[^\n]*|/\*[\s\S]*?\*/)"
        + @"|(?<string>""""""[\s\S]*?""""""|@""(?:[^""]|"""")*""|\$?""(?:[^""\\\n]|\\.)*""|'(?:[^'\\\n]|\\.)')"
        + "|(?<keyword>" + Keywords + ")"
        + "|" + Number)]
    private static partial Regex CSharp();

    [GeneratedRegex(@"(?<comment>/\*[\s\S]*?\*/)"
        + @"|(?<string>""[^""\n]*""|'[^'\n]*')"
        + @"|(?<attribute>(?<=(?:[{;]|\*/)\s*)-{0,2}[A-Za-z][\w-]*(?=\s*:))"
        + @"|(?<keyword>@[\w-]+|!important)"
        + @"|(?<number>#[0-9A-Fa-f]{3,8}\b|\b\d+(?:\.\d+)?(?:px|rem|em|%|s|ms|deg|ch|vw|vh)?\b)")]
    private static partial Regex Css();

    [GeneratedRegex(@"(?<attribute>""(?:[^""\\\n]|\\.)*""(?=\s*:))"
        + @"|(?<string>""(?:[^""\\\n]|\\.)*"")"
        + @"|(?<keyword>\b(?:true|false|null)\b)"
        + @"|(?<number>-?\b\d+(?:\.\d+)?\b)")]
    private static partial Regex Json();

    [GeneratedRegex(@"(?<comment>(?<=^|\s)#[^\n]*)"
        + @"|(?<string>""[^""\n]*""|'[^'\n]*')"
        + @"|(?<attribute>(?<=\s)--?[\w-]+)"
        + @"|(?<keyword>(?<=^|\n)[\w.-]+)", RegexOptions.Multiline)]
    private static partial Regex Shell();
}
