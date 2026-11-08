using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Trailblazor;

// Decodes character references as a browser's parser does, except that a name HTML 4 lacks stays as text.
internal static partial class HtmlCharacterReferences
{
    // Browsers read &#128; to &#159; as the Windows-1252 characters older pages meant by them.
    private const string Windows1252 = "\u20AC\u0081\u201A\u0192\u201E\u2026\u2020\u2021\u02C6\u2030\u0160\u2039\u0152"
        + "\u008D\u017D\u008F\u0090\u2018\u2019\u201C\u201D\u2022\u2013\u2014\u02DC\u2122\u0161\u203A\u0153\u009D"
        + "\u017E\u0178";

    // The names browsers still read without a closing semicolon, such as "&copy 2026".
    private static readonly HashSet<string> LegacyNames = new(("AElig AMP Aacute Acirc Agrave Aring Atilde Auml COPY "
        + "Ccedil ETH Eacute Ecirc Egrave Euml GT Iacute Icirc Igrave Iuml LT Ntilde Oacute Ocirc Ograve Oslash Otilde "
        + "Ouml QUOT REG THORN Uacute Ucirc Ugrave Uuml Yacute aacute acirc acute aelig agrave amp aring atilde auml "
        + "brvbar ccedil cedil cent copy curren deg divide eacute ecirc egrave eth euml frac12 frac14 frac34 gt iacute "
        + "icirc iexcl igrave iquest iuml laquo lt macr micro middot nbsp not ntilde oacute ocirc ograve ordf ordm "
        + "oslash otilde ouml para plusmn pound quot raquo reg sect shy sup1 sup2 sup3 szlig thorn times uacute ucirc "
        + "ugrave uml uuml yacute yen yuml").Split(' '), StringComparer.Ordinal);

    public static string Decode(string text) => text.Contains('&') ? Reference().Replace(text, Replace) : text;

    private static string Replace(Match match)
    {
        if (match.Groups["number"].Success)
        {
            return Character(match.Groups["number"].Value);
        }

        var name = match.Groups["name"].Value;
        if (match.Groups["semicolon"].Success)
        {
            return Named(name) ?? match.Value;
        }

        // Without a semicolon, the longest legacy name the reference starts with is read, and the rest stays text.
        for (var length = Math.Min(name.Length, 6); length >= 2; length--)
        {
            if (LegacyNames.Contains(name[..length]))
            {
                return Named(name[..length]) + name[length..];
            }
        }

        return match.Value;
    }

    // WebUtility knows the HTML 4 names, which spell the six uppercase legacy names in lowercase.
    private static string? Named(string name)
    {
        var known = name is "AMP" or "COPY" or "GT" or "LT" or "QUOT" or "REG" ? name.ToLowerInvariant() : name;
        var decoded = WebUtility.HtmlDecode($"&{known};");
        return decoded == $"&{known};" ? null : decoded;
    }

    private static string Character(string number)
    {
        var parsed = number[0] is 'x' or 'X'
            ? long.TryParse(number.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hex)
                ? hex
                : long.MaxValue
            : long.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                ? value
                : long.MaxValue;
        return parsed switch
        {
            0 or > 0x10FFFF or (>= 0xD800 and <= 0xDFFF) => "\uFFFD",
            >= 0x80 and <= 0x9F => Windows1252[(int)parsed - 0x80].ToString(),
            _ => char.ConvertFromUtf32((int)parsed),
        };
    }

    [GeneratedRegex("&(?:#(?<number>[xX][0-9a-fA-F]+|[0-9]+);?|(?<name>[A-Za-z][A-Za-z0-9]*)(?<semicolon>;)?)")]
    private static partial Regex Reference();
}
