using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbInPlaceEditor</c>, found by its <c>Label</c>.</summary>
public sealed class TbInPlaceEditorLocator(ILocator scope, string label)
{
    /// <summary>The editor's root, which has <c>data-editing</c> while the editor shows.</summary>
    public ILocator InPlaceEditor => scope.Locator(".tb-in-place-editor").Filter(new()
    {
        Has = scope.Page.Locator("[data-part=label]").GetByText(label, new() { Exact = true }),
    });

    /// <summary>Whether the editor shows, rather than the value.</summary>
    public async Task<bool> IsEditingAsync() => await InPlaceEditor.GetAttributeAsync("data-editing") is not null;

    /// <summary>The value's text while it shows, or the placeholder while it's empty.</summary>
    public async Task<string> TextAsync() =>
        (await InPlaceEditor.Locator("[data-part=value]").TextContentAsync())!.Trim();

    /// <summary>Presses the value once the page is interactive, and waits until the editor shows with focus in it, so
    /// the test can type straight away.</summary>
    public async Task EditAsync()
    {
        await Assertions.Expect(InPlaceEditor).ToHaveAttributeAsync("data-ready", "");
        await InPlaceEditor.Locator("[data-part=display]").ClickAsync();
        await Assertions.Expect(InPlaceEditor.Locator("[data-part=editor]:focus-within")).ToHaveCountAsync(1);
    }

    /// <summary>Presses the save button, and waits until the value shows again.</summary>
    public Task SaveAsync() => PressAsync("save");

    /// <summary>Presses the cancel button, and waits until the value shows again.</summary>
    public Task CancelAsync() => PressAsync("cancel");

    private async Task PressAsync(string part)
    {
        await InPlaceEditor.Locator($"[data-part={part}]").ClickAsync();
        await Assertions.Expect(InPlaceEditor).Not.ToHaveAttributeAsync("data-editing", "");
    }
}
