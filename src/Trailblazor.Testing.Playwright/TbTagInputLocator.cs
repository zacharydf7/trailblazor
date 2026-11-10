using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTagInput</c>, found by its label.</summary>
public sealed class TbTagInputLocator(ILocator scope, string label)
{
    /// <summary>The input that tags are typed into.</summary>
    public ILocator Input => scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("input"));

    /// <summary>The tags, in order, for assertions such as
    /// <c>Expect(tags.Tags).ToHaveTextAsync(["Shade", "Water"])</c>.</summary>
    public ILocator Tags => Field.Locator("[data-part=tag]");

    private ILocator Field => Input.Locator("xpath=ancestor::*[@data-part='control'][1]");

    /// <summary>Types this text and presses Enter, which adds it as a tag, once the field's keys work.</summary>
    public async Task AddAsync(string text)
    {
        // A prerendered field is replaced once the page is interactive, and would lose the text.
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-ready", "");
        await Input.FillAsync(text);
        await Input.PressAsync("Enter");
    }

    /// <summary>Removes this tag with its remove button.</summary>
    public Task RemoveAsync(string tag) => Tags
        .Filter(new() { HasTextRegex = new Regex($@"^\s*{Regex.Escape(tag)}\s*$") })
        .Locator("[data-part=remove]").ClickAsync();
}
