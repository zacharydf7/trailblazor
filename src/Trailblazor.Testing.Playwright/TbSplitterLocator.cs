using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The <c>TbSplitter</c> separator that resizes one pane, found by its name, such as "Resize Trails".
/// </summary>
public sealed class TbSplitterLocator(ILocator scope, string separator)
{
    /// <summary>The separator, whose <c>aria-valuenow</c> is the pane's size in percent.</summary>
    public ILocator Separator => scope.GetByRole(AriaRole.Separator, new() { Name = separator, Exact = true });

    // The nearest splitter around the separator is its own, even when splitters are nested.
    private ILocator Splitter => Separator.Locator(
        "xpath=ancestor::*[contains(concat(' ', normalize-space(@class), ' '), ' tb-splitter ')][1]");

    /// <summary>Resizes the pane to this percent with the separator's keys, as a keyboard user would, and waits until
    /// the app has the size.</summary>
    public async Task ResizeAsync(int percent)
    {
        // The keys work once the splitter's script has attached.
        await Assertions.Expect(Splitter).ToHaveAttributeAsync("data-ready", "");
        var vertical = await Separator.GetAttributeAsync("aria-orientation") == "horizontal";
        var rightToLeft = await Separator.EvaluateAsync<bool>("separator => getComputedStyle(separator).direction "
            + "=== 'rtl'");
        var (grow, shrink) = vertical ? ("ArrowDown", "ArrowUp")
            : rightToLeft ? ("ArrowLeft", "ArrowRight") : ("ArrowRight", "ArrowLeft");
        var gap = percent - await SizeAsync();
        var key = gap > 0 ? grow : shrink;
        await Separator.FocusAsync();
        // Shift moves by ten, so a long way goes in tens first.
        for (var left = Math.Abs(gap); left > 0; left -= left >= 10 ? 10 : 1)
        {
            await Separator.Page.Keyboard.PressAsync(left >= 10 ? $"Shift+{key}" : key);
        }

        // The app has the size once every report the keys sent has come back.
        await Assertions.Expect(Splitter).Not.ToHaveAttributeAsync("data-reporting", "");
        await Assertions.Expect(Separator).ToHaveAttributeAsync("aria-valuenow",
            percent.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>The pane's size in percent, as the separator shows it; after <see cref="ResizeAsync"/>, the app has the
    /// same size.</summary>
    public async Task<int> SizeAsync() =>
        int.Parse(await Separator.GetAttributeAsync("aria-valuenow") ?? "", CultureInfo.InvariantCulture);
}
