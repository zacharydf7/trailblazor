using System.Globalization;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>The <c>TbTour</c> on the page.</summary>
public sealed class TbTourLocator(ILocator scope)
{
    /// <summary>The tour, while it shows.</summary>
    public ILocator Tour => scope.Locator(".tb-tour");

    /// <summary>The shown step's dialog.</summary>
    public ILocator StepDialog => Tour.Locator("[data-part=step]");

    /// <summary>Whether the tour shows, without waiting.</summary>
    public async Task<bool> IsOpenAsync() => await Tour.CountAsync() > 0;

    /// <summary>The shown step's position, from 0, read once the tour shows.</summary>
    public async Task<int> StepAsync() =>
        int.Parse((await Tour.GetAttributeAsync("data-step"))!, CultureInfo.InvariantCulture);

    /// <summary>The shown step's title, read once the tour shows.</summary>
    public async Task<string> TitleAsync() => (await Tour.Locator("[data-part=title]").TextContentAsync())!.Trim();

    /// <summary>Goes to the next step, as pressing Next does, and waits for it; on the last step, ends the tour as
    /// completed, as pressing Done does, and waits for it to close.</summary>
    public async Task NextAsync()
    {
        var step = await StepAsync();
        var last = await Tour.Locator("[data-part=next]").GetAttributeAsync("data-last") is not null;
        await PressAsync("next");
        await (last ? Assertions.Expect(Tour).ToHaveCountAsync(0)
            : Assertions.Expect(Tour).ToHaveAttributeAsync("data-step", (step + 1).ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Goes back a step, as pressing Back does, and waits for it. The first step has no Back button, so this
    /// throws there.</summary>
    public async Task BackAsync()
    {
        var step = await StepAsync();
        await PressAsync("back");
        await Assertions.Expect(Tour).ToHaveAttributeAsync("data-step",
            (step - 1).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Ends the tour as skipped, as pressing Skip tour does, and waits for it to close.</summary>
    public async Task SkipAsync()
    {
        await PressAsync("skip");
        await Assertions.Expect(Tour).ToHaveCountAsync(0);
    }

    private async Task PressAsync(string part)
    {
        // The step's buttons work once the tour's script has placed it.
        await Assertions.Expect(Tour).ToHaveAttributeAsync("data-ready", "");
        var button = Tour.Locator($"[data-part={part}]");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException($"The tour's step has no {part} button.");
        }

        await button.ClickAsync();
    }
}
