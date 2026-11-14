using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbViewport</c>, found by its box's <c>Label</c>. Each zoom waits until the app has it.</summary>
public sealed class TbViewportLocator(ILocator scope, string? label)
{
    /// <summary>The box that scrolls the content. Focus it to zoom with the keys: + and - zoom in and out, and 0 shows
    /// the content at 100%.</summary>
    public ILocator Surface => Scope.ByRole(scope, AriaRole.Region, label).And(scope.Locator("[data-part=surface]"));

    private ILocator Root => Surface.Locator("xpath=parent::*");

    /// <summary>The zoom the app has, where 1 shows the content at its own size.</summary>
    public async Task<double> ZoomAsync() =>
        double.Parse((await Root.GetAttributeAsync("data-zoom"))!, CultureInfo.InvariantCulture);

    /// <summary>Clicks the zoom in button. Throws when the zoom is at its largest.</summary>
    public Task ZoomInAsync() => ClickAsync("zoom-in");

    /// <summary>Clicks the zoom out button. Throws when the zoom is at its smallest.</summary>
    public Task ZoomOutAsync() => ClickAsync("zoom-out");

    /// <summary>The fit the zoom follows, or <see cref="ViewportFit.None"/>.</summary>
    public async Task<ViewportFit> FitAsync() => Enum.Parse<ViewportFit>((await Root.GetAttributeAsync("data-fit"))!);

    /// <summary>Picks <see cref="ViewportFit.Width"/> or <see cref="ViewportFit.Page"/> from the zoom menu.</summary>
    public Task FitAsync(ViewportFit fit)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(fit, ViewportFit.None);
        return ChooseAsync($"[role=menuitemradio] >> nth={(fit == ViewportFit.Width ? 0 : 1)}", fit.ToString());
    }

    /// <summary>Picks Actual size from the zoom menu, which shows the content at 100%.</summary>
    public async Task ActualSizeAsync()
    {
        await ChooseAsync("[role=menuitem]", "None");
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-zoom", "1");
    }

    private async Task ClickAsync(string part)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var button = Root.Locator($"[data-part=controls] [data-part={part}]");
        if (await button.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The viewport{Scope.Called(label)} can't zoom any further that way.");
        }

        var before = await Root.GetAttributeAsync("data-zoom");
        await button.ClickAsync();
        await Assertions.Expect(Root).Not.ToHaveAttributeAsync("data-zoom", before!);
    }

    // The menu's trigger is named for the zoom, so it's found inside the viewport, and its items by their place.
    private async Task ChooseAsync(string item, string fit)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var trigger = Root.Locator("[data-part=zoom-menu] [aria-haspopup=menu]");
        var menu = Scope.Document(scope).Locator($"[id='{await trigger.GetAttributeAsync("aria-controls")}']");
        var name = (await menu.Locator(item).TextContentAsync())!.Trim();
        await new TbMenuLocator(Root, (await trigger.GetAttributeAsync("aria-label"))!).ChooseAsync(name);
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-fit", fit);
    }
}
