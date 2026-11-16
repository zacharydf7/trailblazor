namespace Trailblazor;

// Reads a keyboard shortcut such as "Mod+K": any of Mod, Ctrl, Alt, Shift and Meta, then one key.
internal static class KeyShortcut
{
    // Each modifier by the exact name aria-keyshortcuts takes; Mod becomes Control or Meta there.
    private static readonly Dictionary<string, string> Modifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Mod"] = "Mod", ["Ctrl"] = "Control", ["Alt"] = "Alt", ["Shift"] = "Shift", ["Meta"] = "Meta",
    };

    public static bool IsValid(string shortcut) => shortcut.Split('+') is [.. var modifiers, var key]
        && key.Trim().Length > 0 && modifiers.All(modifier => Modifiers.ContainsKey(modifier.Trim()));

    // A shortcut of one character, Space or Plus, with Shift at most, fires on a key people type. A named key such as
    // Escape or F2 doesn't.
    public static bool IsCharacterKey(string shortcut) => shortcut.Split('+') is [.. var modifiers, var key]
        && (key.Trim().Length == 1 || key.Trim().Equals("Space", StringComparison.OrdinalIgnoreCase)
            || key.Trim().Equals("Plus", StringComparison.OrdinalIgnoreCase))
        && modifiers.All(modifier => string.Equals(modifier.Trim(), "Shift", StringComparison.OrdinalIgnoreCase));

    // For aria-keyshortcuts: Mod is ⌘ on Apple devices and Ctrl elsewhere, so "Mod+K" gives "Control+K Meta+K".
    public static string Aria(string shortcut)
    {
        var parts = shortcut.Split('+', StringSplitOptions.TrimEntries);
        var modifiers = parts[..^1].Select(part => Modifiers[part]).ToList();
        string With(string mod) =>
            string.Join('+', modifiers.Select(modifier => modifier == "Mod" ? mod : modifier).Append(parts[^1]));
        return modifiers.Contains("Mod") ? $"{With("Control")} {With("Meta")}" : With("");
    }
}
