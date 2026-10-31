using System.Text;

namespace Trailblazor;

// A pattern such as "(000) 000-0000": 0 takes a digit, a a letter and * either, \ makes the next character literal,
// and every other character is shown as it is. text-mask.js fits typed text the same way.
internal sealed class TextMask
{
    // Each part is a slot, named by its token, or a literal, with no token.
    private readonly List<(char Literal, char? Token)> _parts = [];

    public TextMask(string pattern)
    {
        Pattern = pattern;
        for (var index = 0; index < pattern.Length; index++)
        {
            _parts.Add(pattern[index] switch
            {
                '\\' when index + 1 < pattern.Length => (pattern[++index], null),
                '0' or 'a' or '*' => ('_', pattern[index]),
                var literal => (literal, null),
            });
        }

        Slots = _parts.Count(part => part.Token is not null);
        if (Slots == 0)
        {
            throw new ArgumentException($"The mask \"{pattern}\" has no 0, a or * for text to go in.", nameof(pattern));
        }

        DigitsOnly = _parts.All(part => part.Token is null or '0');
    }

    public string Pattern { get; }

    public int Slots { get; }

    public bool DigitsOnly { get; }

    // The mask with each slot shown as an underscore, as a hint of what to type.
    public string Hint => string.Concat(_parts.Select(part => part.Literal));

    // Each slot takes the next character that fits it, and literals show once a slot after them is filled, or the
    // last slot is. Text passes over as much of a run of literals as it starts with, so "+447700" skips the "+44" but
    // a typed 4 still fills the slot after a literal "+44 ".
    public (string Text, string Slotted) Fit(string text, bool hasLiterals)
    {
        var shown = new StringBuilder();
        var slotted = new StringBuilder();
        var pending = new StringBuilder();
        var at = 0;
        for (var index = 0; index < _parts.Count; index++)
        {
            if (_parts[index].Token is not { } slot)
            {
                var run = string.Concat(_parts.Skip(index).TakeWhile(part => part.Token is null)
                    .Select(part => part.Literal));
                pending.Append(run);
                index += run.Length - 1;
                for (var matched = 0; hasLiterals && matched < run.Length && at < text.Length
                    && text[at] == run[matched]; matched++)
                {
                    at++;
                }

                continue;
            }

            while (at < text.Length && !Fits(slot, text[at]))
            {
                at++;
            }

            if (at == text.Length)
            {
                break;
            }

            shown.Append(pending).Append(text[at]);
            slotted.Append(text[at++]);
            pending.Clear();
        }

        if (slotted.Length == Slots)
        {
            shown.Append(pending);
        }

        return (shown.ToString(), slotted.ToString());
    }

    private static bool Fits(char token, char character) => token switch
    {
        '0' => char.IsAsciiDigit(character),
        'a' => char.IsLetter(character),
        _ => char.IsLetterOrDigit(character),
    };
}
