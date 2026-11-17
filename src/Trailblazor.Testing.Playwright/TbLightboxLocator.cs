using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>An open <c>TbLightbox</c>, found by its title, or the only open lightbox.</summary>
public sealed class TbLightboxLocator(ILocator scope, string? title)
{
    private readonly TbDialogLocator _dialog = new(scope, title);

    /// <summary>The lightbox's dialog.</summary>
    public ILocator Lightbox => (title is null ? scope.GetByRole(AriaRole.Dialog)
        : scope.GetByRole(AriaRole.Dialog, new() { Name = title, Exact = true }))
        .And(scope.Locator("dialog.tb-lightbox:not([data-leaving])"));

    /// <summary>The shown image's position, from 0, read once the lightbox is open.</summary>
    public async Task<int> IndexAsync() => int.Parse((await Part("frame").GetAttributeAsync("data-index"))!,
        CultureInfo.InvariantCulture);

    /// <summary>The shown image's alt text, read once the lightbox is open.</summary>
    public async Task<string> AltAsync() => (await Part("image").GetAttributeAsync("alt"))!;

    /// <summary>The shown image's caption, or null when it has none, read once the lightbox is open.</summary>
    public async Task<string?> CaptionAsync()
    {
        await Part("image").WaitForAsync();
        var caption = Part("caption");
        return await caption.CountAsync() == 0 ? null : (await caption.TextContentAsync())!.Trim();
    }

    /// <summary>The zoom, as a multiple of the size that fits the image to the window: 1 until zoomed in.</summary>
    public async Task<double> ZoomAsync() => double.Parse((await Part("stage").GetAttributeAsync("data-zoom"))!,
        CultureInfo.InvariantCulture);

    /// <summary>How far the image is turned clockwise, in degrees: 0, 90, 180 or 270.</summary>
    public async Task<int> RotationAsync() => 90 * int.Parse((await Part("stage").GetAttributeAsync("data-turns"))!,
        CultureInfo.InvariantCulture);

    /// <summary>Shows the next image, as pressing Next does, and waits for it. On the last image of a lightbox that
    /// doesn't loop, Next is off, so this throws.</summary>
    public Task NextAsync() => PressAsync("next", "frame", "data-index");

    /// <summary>Shows the previous image, as pressing Previous does, and waits for it. On the first image of a
    /// lightbox that doesn't loop, Previous is off, so this throws.</summary>
    public Task PreviousAsync() => PressAsync("previous", "frame", "data-index");

    /// <summary>Zooms in one step, as pressing Zoom in does, and waits for it. At the closest zoom it's off, so this
    /// throws.</summary>
    public Task ZoomInAsync() => PressAsync("zoom-in", "stage", "data-zoom");

    /// <summary>Zooms out one step, as pressing Zoom out does, and waits for it. At the fitted size it's off, so this
    /// throws.</summary>
    public Task ZoomOutAsync() => PressAsync("zoom-out", "stage", "data-zoom");

    /// <summary>Turns the image a quarter turn counterclockwise, as pressing Rotate left does, and waits for it.
    /// </summary>
    public Task RotateLeftAsync() => PressAsync("rotate-left", "stage", "data-turns");

    /// <summary>Turns the image a quarter turn clockwise, as pressing Rotate right does, and waits for it.</summary>
    public Task RotateRightAsync() => PressAsync("rotate-right", "stage", "data-turns");

    /// <summary>Undoes zoom and rotation, as pressing Fit to window does, and waits for it. With neither, it's off,
    /// so this throws.</summary>
    public async Task FitAsync()
    {
        await ClickAsync("fit");
        await Assertions.Expect(Part("stage")).ToHaveAttributeAsync("data-turns", "0");
        await Assertions.Expect(Part("stage")).ToHaveAttributeAsync("data-zoom", "1");
    }

    /// <summary>Closes the lightbox with Escape, as a keyboard user does, and waits for it to fade out and for the
    /// app's <c>OpenChanged</c> to finish.</summary>
    public Task CloseAsync() => _dialog.CancelAsync();

    // Each button changes one mark on the lightbox, so the wait ends once the server has rendered the change.
    private async Task PressAsync(string part, string marked, string mark)
    {
        var before = await Part(marked).GetAttributeAsync(mark);
        await ClickAsync(part);
        await Assertions.Expect(Part(marked)).Not.ToHaveAttributeAsync(mark, before!);
    }

    private async Task ClickAsync(string part)
    {
        var button = Part(part);
        if (await button.GetAttributeAsync("aria-disabled") == "true" || await button.IsDisabledAsync())
        {
            throw new InvalidOperationException($"The lightbox's {part} button is off right now, so it can't be pressed.");
        }

        await button.ClickAsync();
    }

    private ILocator Part(string part) => Lightbox.Locator($"[data-part={part}]");
}
