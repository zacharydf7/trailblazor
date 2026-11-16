using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbHotkey</c>, found by its keys.</summary>
public sealed class TbHotkeyLocator(ILocator scope, string keys)
{
    /// <summary>Presses the hotkey's keys on the page once its script listens for them, with Mod pressed as ⌘ on Apple
    /// devices and Ctrl elsewhere, then waits until the app has run the <c>OnPress</c> of the hotkey that fired, or
    /// until a dialog opens. The hotkey fires only where its scope allows, so focus what it needs first, such as a
    /// field inside a <c>Parent</c> hotkey's element. If no hotkey fires, the press returns at once.</summary>
    public async Task PressAsync()
    {
        var quoted = keys.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        var marker = scope.Locator($"[data-part=hotkey][data-keys=\"{quoted}\"]").First;
        await Assertions.Expect(marker).ToHaveAttributeAsync("data-ready", keys);
        // Playwright names the platform's own modifier ControlOrMeta.
        var parts = keys.Split('+', StringSplitOptions.TrimEntries).Select(part => part.ToUpperInvariant() switch
        {
            "MOD" => "ControlOrMeta",
            "CTRL" => "Control",
            "ALT" => "Alt",
            "SHIFT" => "Shift",
            "META" => "Meta",
            "SPACE" => "Space",
            "PLUS" => "+",
            _ => part,
        });
        // Several hotkeys can share keys and only one fires, so the wait follows the marker the script fires on.
        await Commit.FiredAsync(marker, "tb-hotkey", "data-commits",
            () => scope.Page.Keyboard.PressAsync(string.Join('+', parts)));
    }
}
