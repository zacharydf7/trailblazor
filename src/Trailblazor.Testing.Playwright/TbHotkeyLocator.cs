using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbHotkey</c>, found by its keys.</summary>
public sealed class TbHotkeyLocator(ILocator scope, string keys)
{
    /// <summary>Waits until the hotkey's script listens for its keys, then presses them on the page, with Mod pressed as
    /// ⌘ on Apple devices and Ctrl elsewhere. The hotkey fires only where its scope allows, so focus what it needs
    /// first, such as a field inside a <c>Parent</c> hotkey's element.</summary>
    public async Task PressAsync()
    {
        var quoted = keys.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        await Assertions.Expect(scope.Locator($"[data-part=hotkey][data-keys=\"{quoted}\"]").First)
            .ToHaveAttributeAsync("data-ready", keys);
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
        await scope.Page.Keyboard.PressAsync(string.Join('+', parts));
    }
}
