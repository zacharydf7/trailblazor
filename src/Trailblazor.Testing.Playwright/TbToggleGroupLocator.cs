using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbToggleGroup</c> or <c>TbMultiToggleGroup</c>, found by its label, or by its <c>AriaLabel</c>
/// when it has no label. Its items are named by their text, or by their <c>AriaLabel</c> when they show only an icon.
/// </summary>
public sealed class TbToggleGroupLocator(ILocator scope, string label)
{
    private ILocator Group => scope.GetByRole(AriaRole.Group, new() { Name = label, Exact = true })
        .And(scope.Locator(".tb-toggle-group"));

    /// <summary>The pressed items' names, in order.</summary>
    public async Task<IReadOnlyList<string>> ChosenAsync() => await Group.EvaluateAsync<string[]>(Choices.PressedNames);

    /// <summary>Presses the items with these names that aren't pressed yet, and waits until the app has each. Give a
    /// <c>TbToggleGroup</c> just one, since it keeps one item pressed. A disabled item throws, and so does a statically
    /// rendered group.</summary>
    public Task ChooseAsync(params string[] items) => SetAsync(items, pressed: true);

    /// <summary>Releases the pressed items with these names by pressing them again, which a <c>TbMultiToggleGroup</c>
    /// or a <c>Clearable</c> <c>TbToggleGroup</c> allows, and throws and waits as <see cref="ChooseAsync"/> does.
    /// </summary>
    public Task UnchooseAsync(params string[] items) => SetAsync(items, pressed: false);

    private async Task SetAsync(string[] items, bool pressed)
    {
        ArgumentOutOfRangeException.ThrowIfZero(items.Length, nameof(items));
        if (await Group.GetAttributeAsync("data-static") is not null)
        {
            throw new InvalidOperationException("A toggle group on a static page can't be pressed; render it on an "
                + "interactive page.");
        }

        if (pressed && items.Length > 1 && await Group.GetAttributeAsync("data-multiple") is null)
        {
            throw new ArgumentException("A TbToggleGroup has one item pressed at a time, so choose just one.",
                nameof(items));
        }

        if (!pressed && await Group.GetAttributeAsync("data-clearable") is null)
        {
            throw new InvalidOperationException("This TbToggleGroup can't release its pressed item; make it Clearable "
                + "to allow that.");
        }

        await Assertions.Expect(Group).ToHaveAttributeAsync("data-ready", "");
        var state = pressed ? "true" : "false";
        foreach (var name in items)
        {
            var item = Group.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });
            if (await item.IsDisabledAsync() || await item.GetAttributeAsync("aria-disabled") == "true")
            {
                throw new InvalidOperationException($"\"{name}\" is disabled.");
            }

            if (await item.GetAttributeAsync("aria-pressed") != state)
            {
                await item.ClickAsync();
                // The pressed state arrives with the next render, so wait for it before reading the next item.
                await Assertions.Expect(item).ToHaveAttributeAsync("aria-pressed", state);
            }
        }
    }
}
