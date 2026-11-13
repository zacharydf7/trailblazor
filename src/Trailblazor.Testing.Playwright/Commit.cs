using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

internal static class Commit
{
    // A field's root, a tab list, a sortable list or a grid counts the commits the app has answered, so a helper waits
    // until the one its action raised lands. The action says whether it raised one: text the field already holds raises
    // no change. The root is marked, so the wait also ends when it leaves the page: an app that re-keys the control
    // replaces the root, whose count starts again, and a reload replaces the page.
    public static async Task WaitAsync(ILocator control, Func<Task<bool>> act)
    {
        var mark = Guid.NewGuid().ToString("N");
        var before = await control.Locator("xpath=ancestor-or-self::*[@data-commits][1]").EvaluateAsync<string>(
            "(root, mark) => { root.dataset.tbAwaited = mark; return root.dataset.commits; }", mark);
        if (await act())
        {
            var waiting = Scope.Document(control).Locator($"[data-tb-awaited='{mark}'][data-commits='{before}']");
            await Assertions.Expect(waiting).ToHaveCountAsync(0);
        }
    }
}
