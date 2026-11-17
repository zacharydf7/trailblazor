using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbSmartPaste</c>, found by its button's <c>Label</c>, or the only one in the scope.</summary>
public sealed class TbSmartPasteLocator(ILocator scope, string? label)
{
    private ILocator SmartPaste => label is null ? scope.Locator(".tb-smart-paste")
        : scope.Locator(".tb-smart-paste").Filter(new()
        {
            Has = scope.Page.GetByRole(AriaRole.Button, new() { Name = label, Exact = true }),
        });

    /// <summary>The button that fills the form.</summary>
    public ILocator Button => SmartPaste.Locator("[data-part=button]");

    /// <summary>What it last showed beside the button, such as "Filled 3 fields", or an empty string before the first
    /// paste.</summary>
    public async Task<string> StatusAsync() => (await SmartPaste.Locator("[data-part=status]").TextContentAsync())!
        .Trim();

    /// <summary>Presses the button, enters this text in the box that opens, and fills the form from it, waiting until
    /// the fields are filled. The box opens where the browser won't share the clipboard, so this first makes the page's
    /// <c>navigator.clipboard.readText</c> refuse, as a user can; Firefox would otherwise wait for a click on its own
    /// Paste prompt.</summary>
    public async Task PasteAsync(string text)
    {
        await Button.EvaluateAsync("""
            () => navigator.clipboard.readText = () => Promise.reject(new DOMException('Refused', 'NotAllowedError'))
            """);
        await Commit.ClickedAsync(Button);
        var box = scope.Page.Locator("dialog.tb-smart-paste__box[open]:not([data-leaving])");
        await Assertions.Expect(box).ToBeVisibleAsync();
        await box.Locator("[data-part=text]").FillAsync(text);
        await Commit.ClickedAsync(box.Locator("[data-part=fill]"));
        // The box closes as the model starts, so the wait ends once the button is no longer busy.
        await Assertions.Expect(Button).Not.ToHaveAttributeAsync("aria-busy", "true");
    }
}
