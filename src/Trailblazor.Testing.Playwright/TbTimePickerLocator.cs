using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTimePicker</c>, found by its label or a locator.</summary>
public sealed class TbTimePickerLocator(ILocator scope, ILocator input) : TbFieldLocator
{
    private protected override ILocator Control => Input;

    /// <summary>The box the time is typed in, or the browser's own time box before the page is interactive.
    /// </summary>
    public ILocator Input => input.And(scope.Locator("input[data-part=input]"));

    /// <summary>Fills the time and leaves the box, which commits it, and waits until the app has it; null clears it. A
    /// statically rendered field gets the time without waiting for the app. A read-only or disabled field throws.
    /// </summary>
    public async Task ChooseAsync(TimeOnly? time)
    {
        var input = Input;
        if (!await input.IsEditableAsync())
        {
            throw new InvalidOperationException(
                "This field is read-only or disabled, so a time can't be typed into it.");
        }

        // Both boxes read the time in 24-hour form, with seconds only when it has them.
        var text = time is { } chosen ? chosen.ToString(chosen.Second == 0 && chosen.Millisecond == 0 ? "HH:mm"
            : "HH:mm:ss.FFF", CultureInfo.InvariantCulture) : "";
        if (await input.GetAttributeAsync("data-static") is not null)
        {
            await input.FillAsync(text);
            return;
        }

        // A prerendered field is replaced once the page is interactive, and would lose the time.
        await Assertions.Expect(input).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(input, async () =>
        {
            var changes = await ValueAsync() != time;
            await input.FillAsync(text);
            await input.BlurAsync();
            return changes;
        });
    }

    /// <summary>The time the field holds, or null when it has none.</summary>
    public async Task<TimeOnly?> ValueAsync() =>
        (await Input.GetAttributeAsync("data-value") ?? await Input.InputValueAsync()) is { Length: > 0 } text
        ? TimeOnly.ParseExact(text, ["HH:mm", "HH:mm:ss", "HH:mm:ss.FFF"], CultureInfo.InvariantCulture) : null;
}
