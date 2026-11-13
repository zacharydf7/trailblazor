namespace Trailblazor;

// Reads a keyboard shortcut such as "Mod+K": any of Mod, Ctrl, Alt, Shift and Meta, then one key.
internal static class KeyShortcut
{
    private static readonly HashSet<string> Modifiers =
        new(["Mod", "Ctrl", "Alt", "Shift", "Meta"], StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string shortcut) => shortcut.Split('+') is [.. var modifiers, var key]
        && key.Trim().Length > 0 && modifiers.All(modifier => Modifiers.Contains(modifier.Trim()));

    // A shortcut of one character, Space or Plus, with Shift at most, fires on a key people type. A named key such as
    // Escape or F2 doesn't.
    public static bool IsCharacterKey(string shortcut) => shortcut.Split('+') is [.. var modifiers, var key]
        && (key.Trim().Length == 1 || key.Trim().Equals("Space", StringComparison.OrdinalIgnoreCase)
            || key.Trim().Equals("Plus", StringComparison.OrdinalIgnoreCase))
        && modifiers.All(modifier => string.Equals(modifier.Trim(), "Shift", StringComparison.OrdinalIgnoreCase));
}
