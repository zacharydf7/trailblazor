using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

internal static class PointerDrag
{
    // A short move from the source's middle starts the drag, which can scroll the page, so the target is measured after
    // it. The drop lands at a fraction of the target's height, in line with its middle or, when alongSource is set,
    // with the source, for a target wider than the view.
    public static async Task DragAsync(IPage page, LocatorBoundingBoxResult from,
        Func<Task<LocatorBoundingBoxResult>> to, float at, bool alongSource = false)
    {
        var x = from.X + from.Width / 2;
        await page.Mouse.MoveAsync(x, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x, from.Y + from.Height / 2 + 8, new() { Steps = 2 });
        var end = await to();
        await page.Mouse.MoveAsync(alongSource ? x : end.X + end.Width / 2, end.Y + end.Height * at,
            new() { Steps = 10 });
        await page.Mouse.UpAsync();
    }
}
