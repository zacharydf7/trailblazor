using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>Playwright helpers that find Trailblazor components by role and label, as a user would, so tests keep
/// working when the markup inside a component changes.</summary>
public static class TrailblazorPageExtensions
{
    /// <summary>The Trailblazor helpers for this page.</summary>
    public static TrailblazorPage Tb(this IPage page) => new(page);
}

/// <summary>The Trailblazor components on a page.</summary>
public sealed class TrailblazorPage(IPage page)
{
    /// <summary>The <c>TbSelect</c> or <c>TbMultiSelect</c> with this label.</summary>
    public TbSelectLocator Select(string label) => new(page, label);

    /// <summary>The <c>TbAutocomplete</c> with this label.</summary>
    public TbAutocompleteLocator Autocomplete(string label) => new(page, label);

    /// <summary>The <c>TbListBox</c> or <c>TbMultiListBox</c> with this label.</summary>
    public TbListBoxLocator ListBox(string label) => new(page, label);

    /// <summary>The <c>TbColorPicker</c> with this label.</summary>
    public TbColorPickerLocator ColorPicker(string label) => new(page, label);

    /// <summary>The <c>TbFileUpload</c> with this label.</summary>
    public TbFileUploadLocator FileUpload(string label) => new(page, label);

    /// <summary>The <c>TbDateRangePicker</c> with this label.</summary>
    public TbDateRangePickerLocator DateRangePicker(string label) => new(page, label);

    /// <summary>The <c>TbTreeView</c> with this accessible name.</summary>
    public TbTreeLocator Tree(string name) => new(page, name);

    /// <summary>The <c>TbPagination</c> with this accessible name.</summary>
    public TbPaginationLocator Pagination(string name) => new(page, name);

    /// <summary>The <c>TbPopover</c> whose trigger has this name.</summary>
    public TbPopoverLocator Popover(string trigger) => new(page, trigger);
}

/// <summary>A <c>TbPopover</c>, found by its trigger's name.</summary>
public sealed class TbPopoverLocator(IPage page, string trigger)
{
    private ILocator Trigger => page.GetByRole(AriaRole.Button, new() { Name = trigger, Exact = true });

    /// <summary>Opens the popover, if it isn't open, and returns its panel. On a statically rendered page it opens
    /// natively, without the script.</summary>
    public async Task<ILocator> OpenAsync()
    {
        var panel = await PanelAsync();
        if (!await panel.IsVisibleAsync())
        {
            await Trigger.ClickAsync();
        }

        await Assertions.Expect(panel).ToBeVisibleAsync();
        return panel;
    }

    /// <summary>Closes the popover with Escape, as a keyboard user would, if it is open.</summary>
    public async Task CloseAsync()
    {
        var panel = await PanelAsync();
        if (await panel.IsVisibleAsync())
        {
            await page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(panel).ToBeHiddenAsync();
        }
    }

    // Focus and hover work once the popover's script has attached, which a static page never waits for.
    private async Task<ILocator> PanelAsync()
    {
        if (await Trigger.GetAttributeAsync("data-static") is null)
        {
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("data-ready", "");
        }

        return page.Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
    }
}

/// <summary>A <c>TbSelect</c> or <c>TbMultiSelect</c>, found by its label.</summary>
public sealed class TbSelectLocator(IPage page, string label)
{
    /// <summary>Chooses the options with these names: the one option of a select, or each one not yet chosen in a
    /// multiple select, keeping what is already chosen. A statically rendered select is a native one, and is chosen
    /// the native way; one still prerendering is waited for.</summary>
    public async Task ChooseAsync(params string[] options)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        // The list shares the label, so only the combobox, or the native select it replaces, counts as the field.
        var field = page.GetByLabel(label, new() { Exact = true }).And(page.Locator("[role=combobox], select"));
        if (await NativeSelect.TryChooseAsync(field, options, "select"))
        {
            return;
        }

        // The keys and clicks work once the select's script has attached.
        await Assertions.Expect(field).ToHaveAttributeAsync("data-ready", "");
        var listbox = page.Locator($"[id='{await field.GetAttributeAsync("aria-controls")}']");
        NativeSelect.ThrowIfSeveral(options, await listbox.GetAttributeAsync("aria-multiselectable") == "true",
            "select");
        if (!await listbox.IsVisibleAsync())
        {
            await field.ClickAsync();
        }

        foreach (var name in options)
        {
            // A single select closes once chosen, so the option is found even while its list is hidden.
            var option = listbox.GetByRole(AriaRole.Option, new() { Name = name, Exact = true, IncludeHidden = true });
            if (await option.GetAttributeAsync("aria-selected") != "true")
            {
                await option.ClickAsync();
                // The server applies the choice, so wait for it before reading the next option's state.
                await Assertions.Expect(option).ToHaveAttributeAsync("aria-selected", "true");
            }
        }

        // A multiple select stays open for more choices.
        if (await listbox.IsVisibleAsync())
        {
            await field.PressAsync("Escape");
        }
    }

}

/// <summary>A <c>TbAutocomplete</c>, found by its label.</summary>
public sealed class TbAutocompleteLocator(IPage page, string label)
{
    /// <summary>Types the text, waits for the suggestions, and chooses the option with this name, waiting until the
    /// field has taken it. A statically rendered field is a plain input, which just gets the text.</summary>
    public async Task ChooseAsync(string text, string option)
    {
        var input = page.GetByLabel(label, new() { Exact = true }).And(page.Locator("input"));
        if (await input.GetAttributeAsync("data-static") is not null)
        {
            await input.FillAsync(text);
            return;
        }

        // Typing opens the list once the field's script has attached.
        await Assertions.Expect(input).ToHaveAttributeAsync("data-ready", "");
        await input.FillAsync(text);
        var listbox = page.Locator($"[id='{await input.GetAttributeAsync("aria-controls")}']");
        await listbox.GetByRole(AriaRole.Option, new() { Name = option, Exact = true }).ClickAsync();
        // The list closes as the option is clicked, and the server marks the choice a round trip later.
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option,
            new() { Name = option, Exact = true, IncludeHidden = true })).ToHaveAttributeAsync("aria-selected", "true");
    }
}

/// <summary>A <c>TbColorPicker</c>, found by its label.</summary>
public sealed partial class TbColorPickerLocator(IPage page, string label)
{
    /// <summary>Types the color, as <c>#rrggbb</c>, or <c>#rrggbbaa</c> for a field with <c>Alpha</c>, and waits until
    /// the field has taken it. A statically rendered field just gets the text.</summary>
    public async Task ChooseAsync(string color)
    {
        if (!HexColor().IsMatch(color))
        {
            throw new ArgumentException($"Pass the color as #rrggbb or #rrggbbaa, not {color}.", nameof(color));
        }

        var input = page.GetByLabel(label, new() { Exact = true }).And(page.Locator("input"));
        if (await input.GetAttributeAsync("data-static") is not null)
        {
            await input.FillAsync(color);
            return;
        }

        // A prerendered field is replaced once the page is interactive, so the interactive one is waited for.
        await Assertions.Expect(input).Not.ToHaveAttributeAsync("data-prerender", "");
        await input.FillAsync(color);
        await input.PressAsync("Enter");
        // The field writes an opaque color without its alpha.
        var expected = color.ToLowerInvariant();
        await Assertions.Expect(input).ToHaveAttributeAsync("data-value",
            expected.Length == 9 && expected.EndsWith("ff", StringComparison.Ordinal) ? expected[..7] : expected);
    }

    [GeneratedRegex("^#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
    private static partial Regex HexColor();
}

/// <summary>A <c>TbFileUpload</c>, found by its label.</summary>
public sealed class TbFileUploadLocator(IPage page, string label)
{
    /// <summary>Chooses the files at these paths, as dropping them or picking them in the file dialog would, and waits
    /// until the field has taken the choice. A statically rendered field just gets the files.</summary>
    public Task ChooseAsync(params string[] paths) => ChooseAsync(input => input.SetInputFilesAsync(paths));

    /// <summary>Chooses files made in the test, each with a <c>Name</c>, <c>MimeType</c> and <c>Buffer</c>, and waits
    /// until the field has taken the choice.</summary>
    public Task ChooseAsync(params FilePayload[] files) => ChooseAsync(input => input.SetInputFilesAsync(files));

    private async Task ChooseAsync(Func<ILocator, Task> choose)
    {
        var input = page.GetByLabel(label, new() { Exact = true }).And(page.Locator("input[type=file]"));
        if (await input.GetAttributeAsync("data-static") is not null)
        {
            await choose(input);
            return;
        }

        // A prerendered field is replaced once the page is interactive, and its script listens only after that, which
        // data-ready tells; the field counts the choices it has taken, which tells when it has this one.
        await Assertions.Expect(input).ToHaveAttributeAsync("data-ready", "");
        var choices = await input.GetAttributeAsync("data-choices");
        await choose(input);
        await Assertions.Expect(input).Not.ToHaveAttributeAsync("data-choices", choices ?? "");
    }
}

/// <summary>A <c>TbDateRangePicker</c>, found by its label.</summary>
public sealed class TbDateRangePickerLocator(IPage page, string label)
{
    /// <summary>Types both dates in the field's own format and waits until the field has taken them; null leaves a
    /// box empty. A statically rendered field gets them in the browser's own date boxes.</summary>
    public async Task ChooseAsync(DateOnly? start, DateOnly? end)
    {
        var group = page.GetByRole(AriaRole.Group, new() { Name = label, Exact = true });
        var boxes = group.Locator("input");
        if (await boxes.First.GetAttributeAsync("data-static") is not null)
        {
            await boxes.Nth(0).FillAsync(Iso(start));
            await boxes.Nth(1).FillAsync(Iso(end));
            return;
        }

        // A prerendered field is replaced once the page is interactive, so the calendar's script is waited for.
        await Assertions.Expect(group.Locator("[data-part=trigger]")).ToHaveAttributeAsync("data-ready", "");
        var format = await group.GetAttributeAsync("data-format");
        var culture = CultureInfo.GetCultureInfo(await group.GetAttributeAsync("data-culture") ?? "");
        await boxes.Nth(0).FillAsync(start?.ToString(format, culture) ?? "");
        await boxes.Nth(1).FillAsync(end?.ToString(format, culture) ?? "");
        await boxes.Nth(1).PressAsync("Enter");
        await Assertions.Expect(group).ToHaveAttributeAsync("data-value", $"{Iso(start)}/{Iso(end)}");
    }

    private static string Iso(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
}

/// <summary>A <c>TbTreeView</c>, found by its accessible name. Rows are named by their path of texts from a root,
/// such as <c>("North", "Ridge")</c>.</summary>
public sealed class TbTreeLocator(IPage page, string name)
{
    private ILocator Tree => page.GetByRole(AriaRole.Tree, new() { Name = name, Exact = true });

    /// <summary>The row at this path, which must be showing.</summary>
    public Task<ILocator> ItemAsync(params string[] path) => RowAsync(path);

    /// <summary>Opens each row along the path, waiting for children that load.</summary>
    public async Task ExpandAsync(params string[] path)
    {
        await Assertions.Expect(Tree).ToHaveAttributeAsync("data-ready", "");
        for (var depth = 1; depth <= path.Length; depth++)
        {
            var row = await RowAsync(path[..depth]);
            if (await row.GetAttributeAsync("aria-expanded") == "false")
            {
                await row.Locator("[data-part=toggle]").ClickAsync();
            }

            await Assertions.Expect(row).ToHaveAttributeAsync("aria-expanded", "true");
            await Assertions.Expect(Tree.Locator("[data-part=loading]")).ToHaveCountAsync(0);
        }
    }

    /// <summary>Opens the rows above the one at this path, clicks it, and waits until it is selected.</summary>
    public async Task SelectAsync(params string[] path)
    {
        await ExpandAsync(path[..^1]);
        var row = await RowAsync(path);
        await row.ClickAsync();
        await Assertions.Expect(row).ToHaveAttributeAsync("aria-selected", "true");
    }

    /// <summary>Opens the rows above the one at this path, and checks it.</summary>
    public async Task CheckAsync(params string[] path)
    {
        await ExpandAsync(path[..^1]);
        var row = await RowAsync(path);
        await row.Locator("[data-part=check]").ClickAsync();
        await Assertions.Expect(row).ToHaveAttributeAsync("aria-checked", "true");
    }

    private async Task<ILocator> RowAsync(string[] path) =>
        Tree.Locator($"[role=treeitem][data-key=\"{await Key(path)}\"]");

    // Rows are flat, so each name is looked for after its parent row and before the next row at the parent's level.
    private async Task<string> Key(string[] path)
    {
        await Assertions.Expect(Tree.Locator("[role=treeitem]").First).ToBeAttachedAsync();
        return await Tree.EvaluateAsync<string?>("""
            (tree, path) => {
                const rows = [...tree.querySelectorAll('[role=treeitem][data-key]')];
                let start = 0;
                let found = null;
                for (let depth = 0; depth < path.length; depth++) {
                    found = null;
                    for (let index = start; index < rows.length; index++) {
                        const level = Number(rows[index].getAttribute('aria-level'));
                        if (level <= depth) {
                            break;
                        }

                        if (level === depth + 1 && rows[index].dataset.text === path[depth]) {
                            found = rows[index];
                            start = index + 1;
                            break;
                        }
                    }

                    if (!found) {
                        return null;
                    }
                }

                return found.dataset.key;
            }
            """, path) ?? throw new InvalidOperationException(
                $"The tree {name} shows no row at {string.Join(" > ", path)}; open the rows above it first.");
    }
}

/// <summary>A <c>TbListBox</c> or <c>TbMultiListBox</c>, found by its label.</summary>
public sealed class TbListBoxLocator(IPage page, string label)
{
    /// <summary>Chooses the options with these names: the one option of a listbox, or each one not yet chosen in a
    /// multiple listbox, keeping what is already chosen. A statically rendered listbox is a native list, and is
    /// chosen the native way; one still prerendering is waited for.</summary>
    public async Task ChooseAsync(params string[] options)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        var field = page.GetByLabel(label, new() { Exact = true }).And(page.Locator("[role=listbox], select"));
        if (await NativeSelect.TryChooseAsync(field, options, "listbox"))
        {
            return;
        }

        // The keys and clicks work once the listbox's script has attached.
        await Assertions.Expect(field).ToHaveAttributeAsync("data-ready", "");
        NativeSelect.ThrowIfSeveral(options, await field.GetAttributeAsync("aria-multiselectable") == "true",
            "listbox");
        foreach (var name in options)
        {
            var option = field.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });
            if (await option.GetAttributeAsync("aria-selected") != "true")
            {
                await option.ClickAsync();
                // The server applies the choice, so wait for it before reading the next option's state.
                await Assertions.Expect(option).ToHaveAttributeAsync("aria-selected", "true");
            }
        }
    }

}

/// <summary>A <c>TbPagination</c>, found by its accessible name.</summary>
public sealed class TbPaginationLocator(IPage page, string name)
{
    private ILocator Navigation => page.GetByRole(AriaRole.Navigation, new() { Name = name, Exact = true });

    /// <summary>Goes to this page and waits until it is current. A compact pagination gets the number typed into its
    /// box; otherwise the page's button is clicked, after stepping toward it through the pages shown when it isn't.
    /// Buttons wait for the page to be interactive; links, from <c>PageHref</c>, are followed at once. Throws for a
    /// page past the last.</summary>
    public async Task GoToPageAsync(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        // The pagination renders its current page and page count, which also waits for it to be on the page.
        await Assertions.Expect(Navigation).ToHaveAttributeAsync("data-page", new Regex(@"^\d+$"));
        var count = int.Parse((await Navigation.GetAttributeAsync("data-page-count"))!, CultureInfo.InvariantCulture);
        if (number > count)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, $"{name} has {count} pages.");
        }

        var box = Navigation.Locator("[data-part=page-box] input");
        if (await box.CountAsync() > 0)
        {
            await GoAsync(number, async () =>
            {
                await box.FillAsync(number.ToString(CultureInfo.InvariantCulture));
                await box.PressAsync("Enter");
            });
            return;
        }

        var pages = Navigation.Locator("[data-part=page]");
        while (await CurrentAsync() is var current && current != number)
        {
            var shown = (await pages.AllInnerTextsAsync())
                .Select(shownPage => int.Parse(shownPage, CultureInfo.InvariantCulture)).ToList();
            // The page itself, or the shown page closest to it on the way there; with no page shown in between, as
            // with no siblings, the Next or Previous button.
            var between = shown.Where(step => number > current ? step > current && step < number
                : step < current && step > number).ToList();
            if (shown.Contains(number) || between.Count > 0)
            {
                var next = shown.Contains(number) ? number : number > current ? between.Max() : between.Min();
                await GoAsync(next, () => pages.Filter(new() { HasTextRegex = new Regex($"^{next}$") }).ClickAsync());
            }
            else
            {
                var step = Navigation.Locator(number > current ? "[data-part=next]" : "[data-part=previous]");
                await GoAsync(number > current ? current + 1 : current - 1, () => step.ClickAsync());
            }
        }
    }

    private async Task<int> CurrentAsync() =>
        int.Parse((await Navigation.GetAttributeAsync("data-page"))!, CultureInfo.InvariantCulture);

    // Blazor attaches its handlers once the page is interactive, so a button or the box waits for that; a link
    // navigates without it.
    private async Task GoAsync(int number, Func<Task> act)
    {
        if (await Navigation.Locator("a[data-part=page]").CountAsync() == 0
            || await Navigation.Locator("[data-part=page-box]").CountAsync() > 0)
        {
            await Assertions.Expect(Navigation).ToHaveAttributeAsync("data-ready", "");
        }

        await act();
        await Assertions.Expect(Navigation)
            .ToHaveAttributeAsync("data-page", number.ToString(CultureInfo.InvariantCulture));
    }
}

// What the select and listbox helpers share: a statically rendered field is the browser's own select.
internal static class NativeSelect
{
    // Chooses on a native select, unless the field is the interactive one or a prerendered select it will replace.
    public static async Task<bool> TryChooseAsync(ILocator field, string[] options, string kind)
    {
        var native = await field.EvaluateAsync<string?>(
            "field => field.localName !== 'select' || field.hasAttribute('data-prerender') ? null " +
            ": field.multiple ? 'multiple' : 'single'");
        if (native is null)
        {
            return false;
        }

        ThrowIfSeveral(options, native == "multiple", kind);
        // Choosing in a native multiple select replaces the selection, so the options already chosen are kept.
        var chosen = await field.EvaluateAsync<string[]>(
            "select => select.multiple ? [...select.selectedOptions].map(option => option.label) : []");
        await field.SelectOptionAsync([.. chosen.Union(options)]);
        return true;
    }

    public static void ThrowIfSeveral(string[] options, bool multiple, string kind)
    {
        if (options.Length > 1 && !multiple)
        {
            throw new ArgumentException($"Only a multiple {kind} takes more than one option.", nameof(options));
        }
    }
}
