using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCarousel</c>, found by its <c>AriaLabel</c>, or the only one in the scope.</summary>
public sealed class TbCarouselLocator(ILocator scope, string? name)
{
    /// <summary>The carousel's region.</summary>
    public ILocator Carousel => Scope.ByRole(scope, AriaRole.Region, name)
        .And(scope.Locator("section.tb-carousel"));

    private ILocator Slides => Carousel.Locator(":scope > [data-part=slides] > [data-part=slide]");

    /// <summary>How many slides there are.</summary>
    public async Task<int> CountAsync()
    {
        await Carousel.WaitForAsync();
        return await Slides.CountAsync();
    }

    /// <summary>The index of the slide shown, from 0. During a move between slides, it's the slide arriving.</summary>
    public async Task<int> ActiveIndexAsync()
    {
        await Carousel.WaitForAsync();
        return await Carousel.EvaluateAsync<int>("carousel => [...carousel.querySelectorAll("
            + "':scope > [data-part=slides] > [data-part=slide]')].findIndex(slide => !slide.hidden && !slide.inert)");
    }

    /// <summary>Shows the next slide, as pressing Next does, and waits for it to appear. On the last slide of a
    /// carousel that doesn't loop, Next is disabled, so this throws, as it does on a static page.</summary>
    public Task NextAsync() => PressAsync("next", 1);

    /// <summary>Shows the previous slide, as pressing Previous does, and waits for it to appear. On the first slide
    /// of a carousel that doesn't loop, Previous is disabled, so this throws, as it does on a static page.</summary>
    public Task PreviousAsync() => PressAsync("previous", -1);

    /// <summary>Shows the slide at this index, from 0, as pressing its button in the picker does, and waits for it
    /// to appear and for the app's <c>ActiveIndexChanged</c> to run; the slide that already shows is left as it is.
    /// A carousel without a picker throws, and so does one on a static page.</summary>
    public async Task GoToAsync(int index)
    {
        await ReadyAsync();
        var picks = Carousel.Locator(":scope > [data-part=picker] > [data-part=pick]");
        var count = await picks.CountAsync();
        if (count == 0)
        {
            throw new InvalidOperationException("This carousel has no picker; use NextAsync and PreviousAsync.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
        var picker = Carousel.Locator(":scope > [data-part=picker]");
        // Picking the slide that shows changes nothing, so the app has nothing to answer.
        await Commit.CountedAsync(picker, "data-commits", async () =>
        {
            if (await picks.Nth(index).GetAttributeAsync("aria-current") == "true")
            {
                return false;
            }

            await picks.Nth(index).ClickAsync();
            return true;
        });
        await SettledAsync(index);
    }

    private async Task PressAsync(string part, int step)
    {
        await ReadyAsync();
        var button = Carousel.Locator($":scope > [data-part=controls] > [data-part={part}]");
        if (await button.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"There's no {part} slide; the carousel doesn't loop.");
        }

        var count = await Slides.CountAsync();
        var target = ((await ActiveIndexAsync() + step) % count + count) % count;
        await Commit.ClickedAsync(button);
        await SettledAsync(target);
    }

    // A turn can move the slides, and the leaving one stays drawn until the move ends.
    private async Task SettledAsync(int index)
    {
        await Assertions.Expect(Slides.Nth(index)).ToBeVisibleAsync();
        await Assertions.Expect(Carousel.Locator(":scope > [data-part=slides]"))
            .Not.ToHaveAttributeAsync("data-move", new Regex("."));
    }

    private async Task ReadyAsync()
    {
        if (await Carousel.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page the carousel's buttons do nothing; render the page "
                + "interactively to move it.");
        }

        // A prerendered carousel is replaced once the page is interactive, and its buttons would lose the click.
        await Assertions.Expect(Carousel).ToHaveAttributeAsync("data-ready", "");

        // Slides that turn by themselves could turn before the click lands, so the pointer pauses them first.
        if (await Carousel.Locator(":scope > [data-part=controls] > [data-part=rotation]").CountAsync() > 0)
        {
            await Carousel.HoverAsync();
            await Assertions.Expect(Carousel.Locator(":scope > [data-part=slides]"))
                .ToHaveAttributeAsync("aria-live", "polite");
        }
    }
}
