using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbCheckbox</c> or <c>TbSwitch</c>, found by its label.</summary>
public sealed class TbCheckboxLocator : TbFieldLocator
{
    private protected override ILocator Control => _box;

    private readonly ILocator _box;

    internal TbCheckboxLocator(ILocator scope, string label, bool isSwitch) =>
        _box = scope.GetByLabel(FieldLabel.Matching(label))
            .And(scope.Locator(isSwitch ? ".tb-switch input[role=switch]" : ".tb-checkbox input[type=checkbox]"));

    /// <summary>Whether it's checked, or on for a switch.</summary>
    public Task<bool> CheckedAsync() => _box.IsCheckedAsync();

    /// <summary>Checks it, as clicking it does, unless it's checked already, and waits until the app has it. A
    /// statically rendered one is checked the native way. A read-only or disabled one throws.</summary>
    public Task CheckAsync() => SetAsync(true);

    /// <summary>Unchecks it, unless it's unchecked already, and waits as <see cref="CheckAsync"/> does.</summary>
    public Task UncheckAsync() => SetAsync(false);

    private Task SetAsync(bool on) => Ticks.SetAsync(_box, on);
}

// Checks or unchecks a checkbox or radio: natively on a static page, otherwise once the page is interactive, waiting
// for the app.
internal static class Ticks
{
    public static async Task SetAsync(ILocator box, bool on)
    {
        var root = box.Locator("xpath=ancestor::*[@data-commits][1]");
        if (await box.IsDisabledAsync() || await box.GetAttributeAsync("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This input is read-only or disabled, so a user can't change it.");
        }

        if (await root.GetAttributeAsync("data-static") is not null)
        {
            await box.SetCheckedAsync(on);
            return;
        }

        // A prerendered field is replaced once the page is interactive, which would undo the click.
        await Assertions.Expect(root).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(box, async () =>
        {
            if (await box.IsCheckedAsync() == on)
            {
                return false;
            }

            await box.SetCheckedAsync(on);
            return true;
        });
    }
}
