using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTextField</c>, found by its label.</summary>
public sealed class TbTextFieldLocator(ILocator scope, string label)
{
    private ILocator Input => scope.GetByLabel(FieldLabel.Matching(label))
        .And(scope.Locator(".tb-text-field input[data-part=input], .tb-text-field textarea[data-part=input]"));

    /// <summary>The text the field shows.</summary>
    public Task<string> TextAsync() => Input.InputValueAsync();

    /// <summary>Types this text into the field and leaves it, which commits it, and waits until the app has it. A
    /// statically rendered field just gets the text. A read-only or disabled field throws.</summary>
    public async Task EnterAsync(string text)
    {
        var input = Input;
        if (!await input.IsEditableAsync())
        {
            throw new InvalidOperationException("This field is read-only or disabled, so text can't be typed into it.");
        }

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
