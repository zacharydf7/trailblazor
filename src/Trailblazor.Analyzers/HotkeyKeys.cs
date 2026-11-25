namespace Trailblazor.Analyzers;

// Reads a hotkey's keys as TbHotkey does: any of Mod, Ctrl, Alt, Shift and Meta, then one key, joined by plus signs.
internal static class HotkeyKeys
{
    private static readonly string[] Modifiers = ["Mod", "Ctrl", "Alt", "Shift", "Meta"];

    public static bool IsReadable(string keys)
    {
        var parts = keys.Split('+');
        return parts[parts.Length - 1].Trim().Length > 0 && parts.Take(parts.Length - 1)
            .All(part => Modifiers.Contains(part.Trim(), StringComparer.OrdinalIgnoreCase));
    }

    // One character, Space or Plus, with Shift at most: a key people type. A named key such as Escape or F2 isn't.
    public static bool IsCharacterKey(string keys)
    {
        var parts = keys.Split('+');
        var key = parts[parts.Length - 1].Trim();
        return (key.Length == 1 || key.Equals("Space", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Plus", StringComparison.OrdinalIgnoreCase))
            && parts.Take(parts.Length - 1)
                .All(part => part.Trim().Equals("Shift", StringComparison.OrdinalIgnoreCase));
    }
}
