using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbStepper</c>, found by its name.</summary>
public sealed class TbStepperLocator(ILocator scope, string? name)
{
    /// <summary>The stepper's list.</summary>
    public ILocator Stepper => Scope.ByRole(scope, AriaRole.List, name).And(scope.Locator("ol[data-part=stepper]"));

    private ILocator Steps => Stepper.Locator(":scope > [data-part=step]");

    // A vertical stepper's current step also holds its content, which can have titles of its own.
    private ILocator Titles => Stepper.Locator(":scope > [data-part=step] > [data-part=step-button] [data-part=title], "
        + ":scope > [data-part=step] > [data-part=step-header] [data-part=title]");

    /// <summary>The steps' titles, in order.</summary>
    public async Task<IReadOnlyList<string>> StepsAsync()
    {
        await Stepper.WaitForAsync();
        return [.. (await Titles.AllTextContentsAsync()).Select(text => text.Trim())];
    }

    /// <summary>The title of the current step, or null when no step is current.</summary>
    public async Task<string?> CurrentAsync()
    {
        await Stepper.WaitForAsync();
        var current = Stepper.Locator(":scope > [data-part=step] > [aria-current=step] [data-part=title]");
        return await current.CountAsync() == 0 ? null : (await current.TextContentAsync())?.Trim();
    }

    /// <summary>Waits until the page is interactive, goes to the step with this title as clicking its header does,
    /// and waits until it's current. A step a user can't go to throws: one that's disabled, or, in a <c>Linear</c>
    /// stepper, one ahead that isn't finished.</summary>
    public async Task GoToAsync(string title)
    {
        var titles = await StepsAsync();
        var matches = Enumerable.Range(0, titles.Count).Where(at => titles[at] == title).ToList();
        if (matches.Count != 1)
        {
            throw new InvalidOperationException(matches.Count == 0
                ? $"The stepper has no step titled \"{title}\"." : $"{matches.Count} steps are titled \"{title}\".");
        }

        var button = Steps.Nth(matches[0]).Locator(":scope > [data-part=step-button]");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException($"The step \"{title}\" can't be chosen: it's disabled, or ahead of "
                + "the current step in a linear stepper.");
        }

        await Assertions.Expect(Stepper).ToHaveAttributeAsync("data-ready", "");
        await button.ClickAsync();
        await Assertions.Expect(button).ToHaveAttributeAsync("aria-current", "step");
    }
}
