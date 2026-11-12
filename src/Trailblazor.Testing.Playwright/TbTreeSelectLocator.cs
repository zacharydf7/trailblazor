using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTreeSelect</c> or <c>TbMultiTreeSelect</c>, found by its label. Items are named by their path of
/// texts from a root, such as <c>("North ridge", "Pine Hollow")</c>.</summary>
public sealed class TbTreeSelectLocator(ILocator scope, string label)
{
    // The field's label names its button, which also carries the chosen text.
    private ILocator Trigger =>
        scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator(".tb-tree-select__trigger"));

    /// <summary>Opens the tree, opens the rows above the item at this path, and chooses it, waiting until the field
    /// has taken it and closed.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        var tree = await OpenAsync();
        await tree.ExpandAsync(path[..^1]);
        await (await tree.ItemAsync(path)).ClickAsync();
        // The field closes in the same render that takes the choice.
        await Assertions.Expect(Trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    /// <summary>Opens the tree, checks the item at this path in a multi tree select unless it is checked already, and
    /// closes the tree again.</summary>
    public Task CheckAsync(params string[] path) => SetCheckedAsync(path, "true");

    /// <summary>Opens the tree, unchecks the item at this path in a multi tree select unless it is unchecked already,
    /// and closes the tree again.</summary>
    public Task UncheckAsync(params string[] path) => SetCheckedAsync(path, "false");

    /// <summary>Opens the tree and types this text in its search, waiting until the tree shows what matches. The tree
    /// stays open, so its items can then be chosen.</summary>
    public async Task SearchAsync(string text)
    {
        await OpenAsync();
        var search = (await PanelAsync()).Locator("[data-part=search]");
        if (await search.CountAsync() == 0)
        {
            throw new InvalidOperationException("This tree select has no search; set Searchable to add one.");
        }

        await search.FillAsync(text);
        await Assertions.Expect(search).ToHaveAttributeAsync("data-searched", text.Trim());
    }

    /// <summary>The text the field shows for its value, or null while it shows its placeholder.</summary>
    public async Task<string?> TextAsync()
    {
        var value = Trigger.Locator("[data-part=value]");
        return await value.GetAttributeAsync("class") is { } classes && classes.Contains("tb-select__placeholder")
            ? null : (await value.InnerTextAsync()).Trim();
    }

    // A partly checked row checks everything under it first, so unchecking it takes a second click.
    private async Task SetCheckedAsync(string[] path, string wanted)
    {
        var tree = await OpenAsync();
        await tree.ExpandAsync(path[..^1]);
        var row = await tree.ItemAsync(path);
        for (var clicks = 0; await row.GetAttributeAsync("aria-checked") is var state && state != wanted; clicks++)
        {
            if (clicks == 2)
            {
                throw new InvalidOperationException($"The row at {string.Join(" > ", path)} can't be checked or "
                    + "unchecked.");
            }

            await row.Locator("[data-part=check]").ClickAsync();
            await Assertions.Expect(row).Not.ToHaveAttributeAsync("aria-checked", state ?? "");
        }

        await CloseAsync();
    }

    // The tree is named by the field's label, and its keys and clicks work once the field's script has attached. A
    // read-only field's button opens nothing. A button can also be disabled only because the page isn't interactive
    // yet, so the disabled check waits for the script.
    private async Task<TbTreeLocator> OpenAsync()
    {
        if (await Trigger.GetAttributeAsync("aria-controls") is null)
        {
            throw Closed();
        }

        await Assertions.Expect(Trigger).ToHaveAttributeAsync("data-ready", "");
        if (await Trigger.IsDisabledAsync())
        {
            throw Closed();
        }

        if (await Trigger.GetAttributeAsync("aria-expanded") != "true")
        {
            await Trigger.ClickAsync();
        }

        var panel = await PanelAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();
        return new TbTreeLocator(panel, label);
    }

    private static InvalidOperationException Closed() =>
        new("This tree select is disabled or read-only, so its tree can't be used.");

    private async Task CloseAsync()
    {
        if (await Trigger.GetAttributeAsync("aria-expanded") == "true")
        {
            await scope.Page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("aria-expanded", "false");
        }
    }

    private async Task<ILocator> PanelAsync() =>
        Scope.Document(scope).Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
}
