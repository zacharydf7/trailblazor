using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTagInput</c>, found by its label.</summary>
public sealed class TbTagInputLocator(ILocator scope, string label) : TbFieldLocator
{
    private protected override ILocator Control => Input;

    /// <summary>The input that tags are typed into.</summary>
    public ILocator Input => scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("input"));

    /// <summary>The tags, in order, for assertions such as
    /// <c>Expect(tags.Tags).ToHaveTextAsync(["Shade", "Water"])</c>.</summary>
    public ILocator Tags => Field.Locator("[data-part=tag]");

    private ILocator Field => Input.Locator("xpath=ancestor::*[@data-part='control'][1]");

    /// <summary>Once the field's keys work, types this text and presses Enter, which adds it as a tag, then waits until
    /// the app has added it or turned it down.</summary>
    public async Task AddAsync(string text)
    {
        // A prerendered field is replaced once the page is interactive, and would lose the text.
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(Input, async () =>
        {
            await Input.FillAsync(text);
            await Input.PressAsync("Enter");
            return !string.IsNullOrWhiteSpace(text);
        });
    }

    /// <summary>Removes this tag with its remove button and waits until the app has taken it out.</summary>
    public Task RemoveAsync(string tag) => Commit.WaitAsync(Input, async () =>
    {
        await Tags.Filter(new() { HasTextRegex = new Regex($@"^\s*{Regex.Escape(tag)}\s*$") })
            .Locator("[data-part=remove]").ClickAsync();
        return true;
    });
}
