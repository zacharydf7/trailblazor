using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbNumberField</c>, found by its label.</summary>
public sealed class TbNumberFieldLocator(ILocator scope, string label) : TbFieldLocator
{
    private protected override ILocator Control => Input;

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

    /// <summary>Clicks the field's increase button this many times, or its decrease button for a negative count, and
    /// waits until the app has each step. Stepping stops at <c>Min</c> or <c>Max</c>, where the button turns off. A
    /// read-only, disabled or statically rendered field throws, and so does one whose step buttons are hidden, by
    /// <c>HideStepButtons</c> or, once its script has measured it, by being too narrow for them. Type into those with
    /// <see cref="EnterAsync"/>.</summary>
    public async Task StepAsync(int count)
    {
        ArgumentOutOfRangeException.ThrowIfZero(count);
        var input = Input;
        if (await input.GetAttributeAsync("data-static") is not null || !await input.IsEditableAsync())
        {
            throw new InvalidOperationException("This field is read-only, disabled or static, so it can't be stepped.");
        }

        await Assertions.Expect(input).ToHaveAttributeAsync("data-ready", "");
        var button = input.Locator("xpath=ancestor::*[@data-part='control'][1]")
            .Locator(count < 0 ? "[data-part=decrement]" : "[data-part=increment]");
        if (!await button.IsVisibleAsync())
        {
            throw new InvalidOperationException("This field hides its step buttons; use EnterAsync instead.");
        }

        for (var step = 0; step < Math.Abs(count) && await button.IsEnabledAsync(); step++)
        {
            await Commit.WaitAsync(input, async () =>
            {
                await button.ClickAsync();
                return true;
            });
        }
    }
}
