using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbTreeSelect</c> or <c>TbMultiTreeSelect</c>, found by its label. Items are named by their path of
/// texts from a root, such as <c>("North ridge", "Pine Hollow")</c>.</summary>
public sealed class TbTreeSelectLocator(ILocator scope, string label) : TbFieldLocator
{
    private protected override ILocator Control => Trigger;

    // The field's label names its button, which also carries the chosen text.
    private ILocator Trigger =>
        scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator(".tb-tree-select__trigger"));

    /// <summary>Opens the tree, opens the rows above the item at this path, and chooses it, waiting until the field
    /// has taken it and closed.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        var tree = await OpenTreeAsync();
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

    /// <summary>Opens the tree and types this text in its filter box, waiting until the tree shows what matches. The
    /// tree stays open, so its items can then be chosen.</summary>
    public async Task FilterAsync(string text)
    {
        await OpenTreeAsync();
        var filter = (await PanelAsync()).Locator("[data-part=filter]");
        if (await filter.CountAsync() == 0)
        {
            throw new InvalidOperationException("This tree select has no filter box; set Filterable to add one.");
        }

        await filter.FillAsync(text);
        await Assertions.Expect(filter).ToHaveAttributeAsync("data-filtered", text.Trim());
    }

    /// <summary>Opens the tree and waits until it shows. A disabled or read-only tree select throws, since it has no
    /// tree to open.</summary>
    public Task OpenAsync() => OpenTreeAsync();

    /// <summary>Closes the tree, as pressing Escape does, and waits until it's hidden. A closed tree is left as it
    /// is.</summary>
    public async Task CloseAsync()
    {
        if (await Trigger.GetAttributeAsync("aria-expanded") == "true")
        {
            await scope.Page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("aria-expanded", "false");
        }
    }

    /// <summary>Whether the tree is showing.</summary>
    public async Task<bool> IsOpenAsync() => await Trigger.GetAttributeAsync("aria-expanded") == "true";

    /// <summary>Whether the tree select is disabled, so its tree can't open. A prerendered field is replaced once the
    /// page is interactive, so this waits for that.</summary>
    public async Task<bool> DisabledAsync()
    {
        // A read-only field's button opens nothing, and a static field's button never turns on.
        if (await Trigger.GetAttributeAsync("aria-controls") is not null
            && await Trigger.GetAttributeAsync("data-static") is null)
        {
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("data-ready", "");
        }

        return await Trigger.IsDisabledAsync();
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
        var tree = await OpenTreeAsync();
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
    private async Task<TbTreeLocator> OpenTreeAsync()
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

    private async Task<ILocator> PanelAsync() =>
        Scope.Document(scope).Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
}
