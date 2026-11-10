using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbSlider</c>, found by its label.</summary>
public sealed class TbSliderLocator(IPage page, string label)
{
    /// <summary>The slider's range input.</summary>
    public ILocator Input => page.GetByRole(AriaRole.Slider, new() { NameRegex = FieldLabel.Matching(label) });

    /// <summary>Moves the slider to this value and waits until the field has taken it. A value between steps lands on
    /// the nearest step, as it would for the user. A statically rendered slider just moves.</summary>
    public Task SetAsync(decimal value) => SliderThumb.MoveAsync(Input, value);

    /// <summary>The value the field holds.</summary>
    public Task<decimal> ValueAsync() => SliderThumb.ValueAsync(Input);
}

/// <summary>A <c>TbRangeSlider</c>, found by its label.</summary>
public sealed class TbRangeSliderLocator(IPage page, string label)
{
    private ILocator Field => page.Locator(".tb-slider--range")
        .Filter(new() { Has = page.Locator("label", new() { HasTextRegex = FieldLabel.Matching(label) }) });

    /// <summary>The thumb for the start of the range.</summary>
    public ILocator Start => Field.Locator("[data-part=start-thumb]");

    /// <summary>The thumb for the end of the range.</summary>
    public ILocator End => Field.Locator("[data-part=end-thumb]");

    /// <summary>Moves both thumbs and waits until the field has taken the range. The thumbs can't pass each other, so
    /// the end moves first when the new start is past the old end.</summary>
    public async Task SetAsync(decimal start, decimal end)
    {
        if (start > end)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, $"The start can't be after the end, {end}.");
        }

        if (start > await SliderThumb.ValueAsync(End))
        {
            await SliderThumb.MoveAsync(End, end);
            await SliderThumb.MoveAsync(Start, start);
            return;
        }

        await SliderThumb.MoveAsync(Start, start);
        await SliderThumb.MoveAsync(End, end);
    }

    /// <summary>The start and end the field holds.</summary>
    public async Task<(decimal Start, decimal End)> RangeAsync() =>
        (await SliderThumb.ValueAsync(Start), await SliderThumb.ValueAsync(End));
}

internal static class SliderThumb
{
    // Playwright's fill refuses a value the browser snaps to a step, so this sets it and fires input and change itself.
    private const string Move = "(input, value) => { input.value = value; "
        + "input.dispatchEvent(new Event('input', { bubbles: true })); "
        + "input.dispatchEvent(new Event('change', { bubbles: true })); }";

    public static async Task MoveAsync(ILocator thumb, decimal value)
    {
        // The script skips fill's checks, so these make sure a slider the user couldn't move isn't moved either.
        await Assertions.Expect(thumb).ToBeVisibleAsync();
        await Assertions.Expect(thumb).ToBeEnabledAsync();
        await Assertions.Expect(thumb).Not.ToHaveAttributeAsync("aria-readonly", "true");
        // A browser may keep "35.00" as written while the field writes back "35", so the wait below would never pass.
        var text = value.ToString("0.############################", CultureInfo.InvariantCulture);
        if (await thumb.GetAttributeAsync("data-static") is not null)
        {
            await thumb.EvaluateAsync(Move, text);
            return;
        }

        // A prerendered slider is replaced once the page is interactive, and would lose the move.
        await Assertions.Expect(thumb).ToHaveAttributeAsync("data-ready", "");
        await thumb.EvaluateAsync(Move, text);
        // The browser snaps the value to a step, and the field writes back what it took.
        await Assertions.Expect(thumb).ToHaveAttributeAsync("data-value", await thumb.InputValueAsync());
    }

    // A static slider has no round trip to write data-value back, so the input holds what the form will post.
    public static async Task<decimal> ValueAsync(ILocator thumb) => decimal.Parse(
        await thumb.GetAttributeAsync("data-static") is null ? await thumb.GetAttributeAsync("data-value") ?? ""
            : await thumb.InputValueAsync(), CultureInfo.InvariantCulture);
}
