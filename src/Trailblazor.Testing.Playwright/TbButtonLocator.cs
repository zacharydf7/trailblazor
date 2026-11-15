using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbButton</c> or <c>TbIconButton</c>, found by its accessible name.</summary>
public sealed class TbButtonLocator(ILocator scope, string name)
{
    /// <summary>The button or link.</summary>
    public ILocator Button => scope.GetByRole(AriaRole.Button, new() { Name = name, Exact = true })
        .Or(scope.GetByRole(AriaRole.Link, new() { Name = name, Exact = true })).And(scope.Locator(".tb-button"));

    /// <summary>Whether the button is busy, as <c>Loading</c> makes it; a busy button ignores clicks.</summary>
    public async Task<bool> BusyAsync() => await Button.GetAttributeAsync("aria-busy") == "true";

    /// <summary>Clicks the button, as a user does, once the page is interactive and the button is no longer busy, and
    /// waits until the app has run its <c>OnClick</c>. On a static page, a link button is followed and a form's submit
    /// or reset button is clicked, without the wait. Throws for a disabled button, or for any other button on a static
    /// page, where it does nothing.</summary>
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

        // The button is marked, so the wait follows it when the click renames it, and ends if it leaves the page.
        var mark = Guid.NewGuid().ToString("N");
        var before = await Button.EvaluateAsync<string>(
            "(button, mark) => { button.dataset.tbAwaited = mark; return button.dataset.clicks; }", mark);
        await Button.ClickAsync();
        var waiting = Scope.Document(scope).Locator($"[data-tb-awaited='{mark}'][data-clicks='{before}']");
        await Assertions.Expect(waiting).ToHaveCountAsync(0);
    }
}
