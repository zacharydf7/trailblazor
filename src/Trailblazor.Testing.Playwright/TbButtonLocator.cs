using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbButton</c>, <c>TbIconButton</c> or <c>TbFab</c>, found by its accessible name.</summary>
public sealed class TbButtonLocator(ILocator scope, string name)
{
    /// <summary>The button or link.</summary>
    public ILocator Button => scope.GetByRole(AriaRole.Button, new() { Name = name, Exact = true })
        .Or(scope.GetByRole(AriaRole.Link, new() { Name = name, Exact = true })).And(scope.Locator(".tb-button"));

    /// <summary>Whether the button is busy, as <c>Loading</c> makes it; a busy button ignores clicks.</summary>
    public async Task<bool> BusyAsync() => await Button.GetAttributeAsync("aria-busy") == "true";

    /// <summary>Whether a toggle button is pressed.</summary>
    public async Task<bool> PressedAsync() => await Button.GetAttributeAsync("aria-pressed") == "true";

    /// <summary>Whether a toggle button shows its mixed state, on for only part of what it acts on.</summary>
    public async Task<bool> MixedAsync() => await Button.GetAttributeAsync("aria-pressed") == "mixed";

    /// <summary>Clicks the button, as a user does, once the page is interactive and the button is no longer busy, and
    /// waits until the app has run its <c>OnClick</c>, or until a dialog opens, since a handler that awaits the
    /// dialog's answer finishes only once the test answers it. On a static page, a link button is followed and a
    /// form's submit or reset button is clicked, without the wait. Throws for a disabled button, or for any other
    /// button on a static page, where it does nothing.</summary>
    public async Task ClickAsync()
    {
        await Button.WaitForAsync();
        if (await Button.GetAttributeAsync("data-static") is not null)
        {
            // A link, or a form's submit or reset button, still works without the app.
            if (!await Button.EvaluateAsync<bool>("button => button.localName === 'a' "
                    + "|| (button.type === 'submit' || button.type === 'reset') && button.form !== null"))
            {
                throw new InvalidOperationException("On a static page the button does nothing; render the page "
                    + "interactively to use it.");
            }

            if (await Button.IsDisabledAsync() || await Button.GetAttributeAsync("aria-disabled") == "true")
            {
                throw new InvalidOperationException("The button is disabled, so a user can't click it.");
            }

            await Button.ClickAsync();
            return;
        }

        // A prerendered button is replaced once the page is interactive, and would lose the click.
        await Assertions.Expect(Button).ToHaveAttributeAsync("data-clicks", new Regex(@"^\d+$"));
        await Assertions.Expect(Button).Not.ToHaveAttributeAsync("aria-busy", "true");
        if (await Button.IsDisabledAsync() || await Button.GetAttributeAsync("aria-disabled") == "true")
        {
            throw new InvalidOperationException("The button is disabled, so a user can't click it.");
        }

        // The wait follows the button it marks, so it holds when the click renames the button.
        await Commit.CountedAsync(Button, "data-clicks", async () =>
        {
            await Button.ClickAsync();
            return true;
        });
    }
}
