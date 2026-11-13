namespace Trailblazor;

// Scores how well a query matches a text, as editors' command pickers do: the query's letters must appear in the text
// in order, and letters in a run or at the start of a word score higher. Case and accents don't count.
internal static class FuzzyMatch
{
    private const int Letter = 1;
    private const int Run = 8;
    private const int WordStart = 8;
    private const int Gap = 1;
    private const int None = int.MinValue;

    // The score and the matched letters' places in the text, or null when the query's letters don't all appear in
    // order. Spaces in the query are left out, so "new tab" matches "New Tab" and "newtab".
    public static Match? Find(string text, string query)
    {
        var wanted = TextFold.Fold(string.Concat(query.Where(letter => !char.IsWhiteSpace(letter))));
        var (letters, places) = FoldWithPlaces(text);
        if (wanted.Length == 0 || wanted.Length > letters.Count)
        {
            return null;
        }

        // score[q, t] is the best score with the query's letter q matched at the folded letter t; from[q, t] is where
        // the letter before it was matched.
        var score = new int[wanted.Length, letters.Count];
        var from = new int[wanted.Length, letters.Count];
        for (var q = 0; q < wanted.Length; q++)
        {
            var bestEarlier = None;
            var bestEarlierAt = -1;
            for (var t = 0; t < letters.Count; t++)
            {
                if (q > 0 && t >= 2 && score[q - 1, t - 2] > bestEarlier)
                {
                    (bestEarlier, bestEarlierAt) = (score[q - 1, t - 2], t - 2);
                }

                score[q, t] = None;
                if (letters[t] != wanted[q])
                {
                    continue;
                }

                var gain = Letter + (StartsWord(text, places[t]) ? WordStart : 0);
                if (q == 0)
                {
                    (score[q, t], from[q, t]) = (gain - (t == 0 ? 0 : Gap), -1);
                    continue;
                }

                var inRun = t >= 1 && score[q - 1, t - 1] != None ? score[q - 1, t - 1] + gain + Run : None;
                var afterGap = bestEarlier != None ? bestEarlier + gain - Gap : None;
                (score[q, t], from[q, t]) = inRun >= afterGap ? (inRun, t - 1) : (afterGap, bestEarlierAt);
            }
        }

        var last = wanted.Length - 1;
        var end = -1;
        for (var t = 0; t < letters.Count; t++)
        {
            if (score[last, t] != None && (end < 0 || score[last, t] > score[last, end]))
            {
                end = t;
            }
        }

        if (end < 0)
        {
            return null;
        }

        var matched = new SortedSet<int>();
        for (int q = last, t = end; q >= 0; t = from[q, t], q--)
        {
            matched.Add(places[t]);
        }

        return new(score[last, end], [.. matched]);
    }

    // Folds each letter on its own, so every folded letter knows the place in the text it came from.
    private static (List<char> Letters, List<int> Places) FoldWithPlaces(string text)
    {
        var letters = new List<char>(text.Length);
        var places = new List<int>(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            foreach (var letter in TextFold.Fold(text[index].ToString()))
            {
                letters.Add(letter);
                places.Add(index);
            }
        }

        return (letters, places);
    }

    // A word starts the text, follows a space or punctuation, or is a capital after a small letter, as in "dataGrid".
    private static bool StartsWord(string text, int place) => place == 0
        || !char.IsLetterOrDigit(text[place - 1])
        || (char.IsUpper(text[place]) && char.IsLower(text[place - 1]));

    public readonly record struct Match(int Score, int[] Places);
}
