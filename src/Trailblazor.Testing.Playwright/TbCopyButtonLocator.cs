using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCopyButton</c>, found by its words, its <c>Labels.Copy</c>, such as "Copy", or the only one in the
/// scope.</summary>
public sealed class TbCopyButtonLocator(ILocator scope, string? label)
{
    /// <summary>The button.</summary>
    public ILocator Element => label is null ? scope.Locator(".tb-copy-button")
        : scope.Locator(".tb-copy-button").And(scope.Locator($"[data-label='{label.Replace("'", "\\'")}']"));

    /// <summary>How its last copy went, while it still shows it: <c>copied</c> or <c>failed</c>; <c>idle</c> before a
    /// copy and once the moment passes.</summary>
    public async Task<string> StateAsync() => (await Element.GetAttributeAsync("data-state"))!;

    /// <summary>Presses the button and returns once it shows how this copy went. Chromium copies only in a context
    /// granted <c>clipboard-read</c> and <c>clipboard-write</c>, which can then read it with
    /// <c>navigator.clipboard.readText()</c>; in Firefox, paste into a field to read it.</summary>
    public async Task CopyAsync()
    {
        var copies = int.Parse((await Element.GetAttributeAsync("data-copies"))!, CultureInfo.InvariantCulture);
        await Element.ClickAsync();
        await Assertions.Expect(Element).Not.ToHaveAttributeAsync("data-copies",
            copies.ToString(CultureInfo.InvariantCulture));
    }
}
