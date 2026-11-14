using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbNumberField</c>, found by its label.</summary>
public sealed class TbNumberFieldLocator(ILocator scope, string label)
{
    private ILocator Input => scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("input"));

    /// <summary>The text the field shows.</summary>
    public Task<string> TextAsync() => Input.InputValueAsync();

    /// <summary>Types this text into the field and leaves it, which commits it, and waits until the app has it. A
    /// statically rendered field just gets the text.</summary>
    public async Task EnterAsync(string text)
    {
        var input = Input;
        if (await input.GetAttributeAsync("data-static") is not null)
        {
            await input.FillAsync(text);
            return;
        }

        // A prerendered field is replaced once the page is interactive, and would lose the text.
        await Assertions.Expect(input).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(input, async () =>
        {
            var changes = await input.InputValueAsync() != text;
            await input.FillAsync(text);
            await input.BlurAsync();
            return changes;
        });
    }
}
