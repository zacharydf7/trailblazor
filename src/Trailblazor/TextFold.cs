using System.Globalization;
using System.Text;

namespace Trailblazor;

// Folds text for list filters the same way option-filter.js does in the browser: lowercase in the current culture
// first, so Turkish "İ" becomes "i", then without accents, so "jose" finds "José".
internal static class TextFold
{
    // Greek's final sigma becomes σ, the lowercase of Σ, so a word typed in capitals still matches.
    public static string Fold(string text) => string.Concat(text.ToLower(CultureInfo.CurrentCulture)
        .Replace('\u03C2', '\u03C3').Normalize(NormalizationForm.FormD).Where(letter => !IsAccent(letter)));

    public static bool Contains(string text, string query) =>
        Fold(text).Contains(Fold(query), StringComparison.Ordinal);

    // The combining diacritical marks, and Hebrew's and Arabic's vowel points and cantillation marks, which people
    // usually leave out when typing. Other scripts' marks, such as Devanagari vowel signs, are letters.
    private static bool IsAccent(char letter) => letter is >= '\u0300' and <= '\u036F' or >= '\u0591' and <= '\u05BD'
        or '\u05BF' or >= '\u05C1' and <= '\u05C2' or >= '\u05C4' and <= '\u05C5' or '\u05C7'
        or >= '\u064B' and <= '\u065F' or '\u0670' or >= '\u1AB0' and <= '\u1AFF' or >= '\u1DC0' and <= '\u1DFF'
        or >= '\u20D0' and <= '\u20FF' or >= '\uFE20' and <= '\uFE2F';
}
