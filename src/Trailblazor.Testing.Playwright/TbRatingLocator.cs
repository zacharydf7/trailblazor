using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbRating</c>, found by its label.</summary>
public sealed class TbRatingLocator(ILocator scope, string label) : TbFieldLocator
{
    private protected override ILocator Control => Rating;

    private ILocator Rating => scope.GetByRole(AriaRole.Radiogroup, new() { NameRegex = FieldLabel.Matching(label) })
        .And(scope.Locator("fieldset.tb-rating"));

    private ILocator Stars => Rating.Locator("[data-part=stars] input[type=radio]");

    /// <summary>How many stars are chosen, or 0 when none are.</summary>
    public Task<int> ValueAsync() => Rating.EvaluateAsync<int>(
        "rating => Number(rating.querySelector('[data-part=stars] input:checked')?.value ?? 0)");

    /// <summary>How many stars there are.</summary>
    public Task<int> MaxAsync() => Stars.CountAsync();

    /// <summary>Chooses this many stars and waits until the app has them. A statically rendered rating is chosen the
    /// native way. A read-only or disabled rating throws.</summary>
    public async Task RateAsync(int stars)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stars, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(stars, await Stars.CountAsync());
        await ThrowIfReadOnlyAsync();
        await Ticks.SetAsync(Stars.Nth(stars - 1), on: true);
    }

    /// <summary>Takes the rating back to no stars, as clicking the chosen star does, and waits until the app has it.
    /// A rating that isn't <c>Clearable</c> throws, and so does a read-only or disabled one, or one on a static page,
    /// where clicking a chosen radio doesn't clear it.</summary>
    public async Task ClearAsync()
    {
        if (await Rating.GetAttributeAsync("data-clearable") is null)
        {
            throw new InvalidOperationException("This rating isn't Clearable, so its stars can't be taken back.");
        }

        await ThrowIfReadOnlyAsync();
        if (await Stars.First.IsDisabledAsync())
        {
            throw new InvalidOperationException("This input is read-only or disabled, so a user can't change it.");
        }

        if (await Rating.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("On a static page a chosen star stays chosen when clicked; render the "
                + "rating on an interactive page to clear it.");
        }

        var chosen = Rating.Locator("[data-part=stars] input:checked");
        if (await chosen.CountAsync() == 0)
        {
            return;
        }

        await Assertions.Expect(Rating).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(chosen, async () =>
        {
            await chosen.ClickAsync();
            return true;
        });
    }

    private async Task ThrowIfReadOnlyAsync()
    {
        if (await Rating.GetAttributeAsync("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This rating is read-only, so its stars can't be changed.");
        }
    }
}
