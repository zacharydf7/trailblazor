using System.Globalization;
using System.Text;

namespace Trailblazor;

/// <summary>A small picture of a person or a team: their photo, their initials, or an icon.</summary>
public partial class TbAvatar
{
    // The first letter of the first word and of the last, as text elements, so an accented or joined letter stays
    // whole: "Ada Lovelace" is "AL", "Ada" is "A".
    internal static string InitialsOf(string? name)
    {
        var words = (name ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(FirstLetter).OfType<string>().ToList();
        var initials = words.Count switch
        {
            0 => "",
            1 => words[0],
            _ => words[0] + words[^1],
        };
        return initials.ToUpper(CultureInfo.CurrentCulture);
    }

    // A CSS url() of the address, escaped so that no address can end the string and add rules of its own.
    internal static string CssUrl(string address)
    {
        var url = new StringBuilder("url(\"");
        foreach (var character in address)
        {
            if (character is '"' or '\\')
            {
                url.Append('\\').Append(character);
            }
            else if (char.IsControl(character))
            {
                url.Append('\\').Append(((int)character).ToString("x", CultureInfo.InvariantCulture)).Append(' ');
            }
            else
            {
                url.Append(character);
            }
        }

        return url.Append("\")").ToString();
    }

    private static string? FirstLetter(string word)
    {
        var elements = StringInfo.GetTextElementEnumerator(word);
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            if (char.IsLetterOrDigit(element, 0))
            {
                return element;
            }
        }

        return null;
    }
}
