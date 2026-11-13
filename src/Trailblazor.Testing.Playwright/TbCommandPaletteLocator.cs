using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCommandPalette</c>, found by its label, or the only one in the scope.</summary>
public sealed class TbCommandPaletteLocator(ILocator scope, string? name)
{
    private ILocator Marker => name is null ? scope.Locator("[data-command-palette]")
        : scope.Locator($"[data-command-palette=\"{name.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"]");

    private ILocator Search => name is null ? Scope.Document(scope).Locator(".tb-command-palette__input")
        : Scope.Document(scope).GetByRole(AriaRole.Combobox, new() { Name = name, Exact = true });

    private ILocator Listbox => name is null ? Scope.Document(scope).Locator(".tb-command-palette__listbox")
        : Scope.Document(scope).GetByRole(AriaRole.Listbox, new() { Name = name, Exact = true });

    /// <summary>Presses the palette's shortcut, such as Ctrl+K, and waits until its search box has focus and its keys
    /// work. Throws for a palette with no shortcut; click the app's own button instead.</summary>
    public async Task OpenAsync()
    {
        if (await Marker.GetAttributeAsync("data-shortcut") is null)
        {
            throw new InvalidOperationException(
                "This command palette has no shortcut; open it with the app's own button.");
        }

        // The app can change the shortcut as the page turns interactive, so it's read again once a listener is ready,
        // and the helper waits for that shortcut's listener.
        await Assertions.Expect(Marker).ToHaveAttributeAsync("data-ready", new Regex("."));
        var shortcut = (await Marker.GetAttributeAsync("data-shortcut"))!;
        await Assertions.Expect(Marker).ToHaveAttributeAsync("data-ready", shortcut);
        await scope.Page.Keyboard.PressAsync(string.Join('+', shortcut.Split('+').Select(KeyOf)));
        await Assertions.Expect(Search).ToBeFocusedAsync();
        await Assertions.Expect(Search).ToHaveAttributeAsync("data-ready", "");
    }

    /// <summary>Types this text in the open palette's search box, as the user does, and waits until the results are
    /// for it.</summary>
    public async Task SearchAsync(string text)
    {
        await Search.FillAsync(text);
        await Assertions.Expect(Listbox).ToHaveAttributeAsync("data-query", text);
    }

    /// <summary>The names of the commands the open palette shows, in order: each one's text, or with an
    /// <c>ItemTemplate</c>, all it shows.</summary>
    public async Task<IReadOnlyList<string>> ResultsAsync() => await Listbox.EvaluateAsync<string[]>("""
        listbox => [...listbox.querySelectorAll('[data-part=option]')]
            .map(option => (option.querySelector(':scope > .tb-command-palette__text > [data-part=label]') ?? option)
                .textContent.trim())
        """);

    /// <summary>Clicks the command with this name in the open palette and waits until the app's <c>OnChoose</c> has
    /// run. Throws for a command it doesn't show or that is disabled.</summary>
    public async Task ChooseAsync(string command)
    {
        var place = (await ResultsAsync()).ToList().IndexOf(command);
        if (place < 0)
        {
            throw new InvalidOperationException($"The command palette shows no command named \"{command}\".");
        }

        var option = Listbox.Locator("[data-part=option]").Nth(place);
        if (await option.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The command \"{command}\" is disabled.");
        }

        await Commit.WaitAsync(Marker, async () =>
        {
            await option.ClickAsync();
            return true;
        });
    }

    // Mod is ⌘ on Apple devices and Ctrl elsewhere, which Playwright calls ControlOrMeta. The palette ignores the
    // names' case.
    private static string KeyOf(string key) => key.Trim().ToLowerInvariant() switch
    {
        "mod" => "ControlOrMeta",
        "ctrl" => "Control",
        "alt" => "Alt",
        "shift" => "Shift",
        "meta" => "Meta",
        var other => other.Length == 1 ? other.ToUpperInvariant() : key.Trim(),
    };
}
