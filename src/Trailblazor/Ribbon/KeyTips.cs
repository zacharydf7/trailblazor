namespace Trailblazor;

// A key tip is the one key that reaches a ribbon control after Alt. The ribbon's script reads it from data-key-tip.
internal static class KeyTips
{
    public static void Check(string? keyTip, string owner)
    {
        if (keyTip is not null && (keyTip.Length != 1 || !char.IsLetterOrDigit(keyTip[0])))
        {
            throw new InvalidOperationException($"{owner}'s KeyTip must be one letter or digit.");
        }
    }

    public static IReadOnlyDictionary<string, object>? Attributes(string? keyTip) =>
        keyTip is null ? null : new Dictionary<string, object> { ["data-key-tip"] = keyTip };
}
