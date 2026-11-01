using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbNumberField</c>, found by its label.</summary>
public sealed class TbNumberFieldLocator(IPage page, string label)
{
    /// <summary>Types this text into the field and leaves it, which commits it. A statically rendered field just gets
    /// the text.</summary>
    public async Task EnterAsync(string text)
    {
        var input = page.GetByLabel(label, new() { Exact = true }).And(page.Locator("input"));
        if (await input.GetAttributeAsync("data-static") is not null)
        {
            await input.FillAsync(text);
            return;
        }

        // A prerendered field is replaced once the page is interactive, and would lose the text.
        await Assertions.Expect(input).ToHaveAttributeAsync("data-ready", "");
        await input.FillAsync(text);
        await input.BlurAsync();
    }
}
