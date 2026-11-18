using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

internal static class SeparatorKeys
{
    // The key presses that move a separator by this many percent, as a keyboard user would: the arrow that grows the
    // part before it for a gap above zero, and the one that shrinks it for a gap below. Shift moves by ten, so a long
    // way goes in tens first.
    public static async Task<IReadOnlyList<string>> PressesAsync(ILocator separator, int gap)
    {
        var vertical = await separator.GetAttributeAsync("aria-orientation") == "horizontal";
        var rightToLeft = await separator.EvaluateAsync<bool>(
            "separator => getComputedStyle(separator).direction === 'rtl'");
        var (grow, shrink) = vertical ? ("ArrowDown", "ArrowUp")
            : rightToLeft ? ("ArrowLeft", "ArrowRight") : ("ArrowRight", "ArrowLeft");
        var key = gap > 0 ? grow : shrink;
        var presses = new List<string>();
        for (var left = Math.Abs(gap); left > 0; left -= left >= 10 ? 10 : 1)
        {
            presses.Add(left >= 10 ? $"Shift+{key}" : key);
        }

        return presses;
    }
}
