using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCarousel</c>, found by its <c>AriaLabel</c>.</summary>
public sealed class TbCarouselLocator(ILocator scope, string name)
{
    /// <summary>The carousel's region.</summary>
    public ILocator Carousel => scope.GetByRole(AriaRole.Region, new() { Name = name, Exact = true })
        .And(scope.Locator("section.tb-carousel"));

    private ILocator Slides => Carousel.Locator(":scope > [data-part=slides] > [data-part=slide]");

    /// <summary>How many slides there are.</summary>
    public async Task<int> CountAsync()
    {
        await Carousel.WaitForAsync();
        return await Slides.CountAsync();
    }

    /// <summary>The index of the slide shown, from 0.</summary>
    public async Task<int> ActiveIndexAsync()
    {
        await Carousel.WaitForAsync();
        return await Carousel.EvaluateAsync<int>("carousel => [...carousel.querySelectorAll("
            + "':scope > [data-part=slides] > [data-part=slide]')].findIndex(slide => !slide.hidden)");
    }

    /// <summary>Shows the next slide, as pressing Next does, and waits for it to appear. On the last slide of a
    /// carousel that doesn't loop, Next is disabled, so this throws, as it does on a static page.</summary>
    public Task NextAsync() => PressAsync("next", 1);

    /// <summary>Shows the previous slide, as pressing Previous does, and waits for it to appear. On the first slide
    /// of a carousel that doesn't loop, Previous is disabled, so this throws, as it does on a static page.</summary>
    public Task PreviousAsync() => PressAsync("previous", -1);

    /// <summary>Shows the slide at this index, from 0, as pressing its button in the picker does, and waits for it
    /// to appear. A carousel without a picker throws, and so does one on a static page.</summary>
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
        await picks.Nth(index).ClickAsync();
        await Assertions.Expect(Slides.Nth(index)).ToBeVisibleAsync();
    }

    private async Task PressAsync(string part, int step)
    {
        await ReadyAsync();
        // Slides that turn by themselves could turn before the click lands, so the pointer pauses them first.
        if (await Carousel.Locator(":scope > [data-part=controls] > [data-part=rotation]").CountAsync() > 0)
        {
            await Carousel.HoverAsync();
            await Assertions.Expect(Carousel.Locator(":scope > [data-part=slides]"))
                .ToHaveAttributeAsync("aria-live", "polite");
        }

        var button = Carousel.Locator($":scope > [data-part=controls] > [data-part={part}]");
        if (await button.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"There's no {part} slide; the carousel doesn't loop.");
        }

        var count = await Slides.CountAsync();
        var target = ((await ActiveIndexAsync() + step) % count + count) % count;
        await Commit.ClickedAsync(button);
        await Assertions.Expect(Slides.Nth(target)).ToBeVisibleAsync();
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
    }
}
