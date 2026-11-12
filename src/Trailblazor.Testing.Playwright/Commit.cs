using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

internal static class Commit
{
    // A field's root, a tab list, a sortable list or a grid counts the commits the app has answered, so a helper waits
    // until the one its action raised lands. The action says whether it raised one: text the field already holds raises
    // no change.
    public static async Task WaitAsync(ILocator control, Func<Task<bool>> act)
    {
        var root = control.Locator("xpath=ancestor-or-self::*[@data-commits][1]");
        var before = await root.GetAttributeAsync("data-commits") ?? "";
        if (await act())
        {
            await Assertions.Expect(root).Not.ToHaveAttributeAsync("data-commits", before);
        }
    }
}
