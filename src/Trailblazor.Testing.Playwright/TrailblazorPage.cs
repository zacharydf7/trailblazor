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
    public TbSelectLocator Select(string label) => new(page, page.GetByLabel(label, new() { Exact = true }));

    /// <summary>The <c>TbSelect</c> or <c>TbMultiSelect</c> this finds, such as one in a row of fields that share
    /// their labels, as <c>row.GetByLabel("Size")</c>, or one with an id, as <c>page.Locator("#size")</c>.</summary>
    public TbSelectLocator Select(ILocator field) => new(page, field);

    /// <summary>The <c>TbAutocomplete</c> with this label.</summary>
    public TbAutocompleteLocator Autocomplete(string label) => new(page, label);

    /// <summary>The <c>TbListBox</c> or <c>TbMultiListBox</c> with this label.</summary>
    public TbListBoxLocator ListBox(string label) => new(page, label);

    /// <summary>The <c>TbColorPicker</c> with this label.</summary>
    public TbColorPickerLocator ColorPicker(string label) => new(page, page.GetByLabel(label, new() { Exact = true }));

    /// <summary>The <c>TbColorPicker</c> this finds, such as one in a row of fields that share their labels, as
    /// <c>row.GetByLabel("Color")</c>, or one with an id, as <c>page.Locator("#color")</c>.</summary>
    public TbColorPickerLocator ColorPicker(ILocator field) => new(page, field);

    /// <summary>The <c>TbFileUpload</c> with this label.</summary>
    public TbFileUploadLocator FileUpload(string label) => new(page, label);

    /// <summary>The <c>TbDateRangePicker</c> with this label.</summary>
    public TbDateRangePickerLocator DateRangePicker(string label) => new(page, label);

    /// <summary>The <c>TbTreeView</c> with this accessible name.</summary>
    public TbTreeLocator Tree(string name) => new(page, name);

    /// <summary>The <c>TbPagination</c> with this accessible name.</summary>
    public TbPaginationLocator Pagination(string name) => new(page, name);

    /// <summary>The <c>TbDataGrid</c> whose table has this accessible name.</summary>
    public TbGridLocator Grid(string name) => new(page, name);

    /// <summary>The <c>TbPopover</c> whose trigger has this name.</summary>
    public TbPopoverLocator Popover(string trigger) => new(page, trigger);

    /// <summary>The <c>TbMenu</c> whose trigger has this name.</summary>
    public TbMenuLocator Menu(string trigger) => new(page, trigger);

    /// <summary>The dialog with this title.</summary>
    public TbDialogLocator Dialog(string title) => new(page, title);

    /// <summary>The <c>TbTabs</c> with this accessible name, or the only one on the page when no name is
    /// given.</summary>
    public TbTabsLocator Tabs(string? name = null) => new(page, name);

    /// <summary>The toasts on the page.</summary>
    public TbToastsLocator Toasts => new(page);

    /// <summary>The <c>TbNumberField</c> with this label.</summary>
    public TbNumberFieldLocator NumberField(string label) => new(page, label);
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
        await SettledAsync(open: true);
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
            await SettledAsync(open: false);
        }
    }

    // A browser can show or hide the panel before its toggle handler moves focus and sets aria-expanded, so that is
    // waited for.
    private async Task SettledAsync(bool open)
    {
        if (await Trigger.GetAttributeAsync("data-static") is null)
        {
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("aria-expanded", open ? "true" : "false");
        }
    }

    // Focus and hover work once the popover's script has attached, and a static page has no script to wait for.
    private async Task<ILocator> PanelAsync()
    {
        if (await Trigger.GetAttributeAsync("data-static") is null)
        {
            await Assertions.Expect(Trigger).ToHaveAttributeAsync("data-ready", "");
        }

        return page.Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
    }
}

/// <summary>A <c>TbSelect</c> or <c>TbMultiSelect</c>, found by its label or a locator.</summary>
public sealed class TbSelectLocator(IPage page, ILocator field)
{
    /// <summary>Chooses the options with these names: the one option of a select, or each one not yet chosen in a
    /// multiple select, keeping what is already chosen. A statically rendered select is a native one, and is chosen
    /// the native way; one still prerendering is waited for.</summary>
    public async Task ChooseAsync(params string[] options)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        // The list shares the label, so only the combobox, or the native select it replaces, counts as the field.
        var control = field.And(page.Locator("[role=combobox], select"));
        if (await NativeSelect.TryChooseAsync(control, options, "select"))
        {
            return;
        }

        // The keys and clicks work once the select's script has attached.
        await Assertions.Expect(control).ToHaveAttributeAsync("data-ready", "");
        // A filterable select points to the dialog that holds its filter and listbox.
        var controls = await control.GetAttributeAsync("aria-controls");
        var listbox = page.Locator($"[id='{controls}'][role=listbox], [id='{controls}'] [role=listbox]");
        NativeSelect.ThrowIfSeveral(options, await listbox.GetAttributeAsync("aria-multiselectable") == "true",
            "select");
        // The keyboard opens the list, since a click could land on a chip's remove button.
        if (!await listbox.IsVisibleAsync())
        {
            await control.FocusAsync();
            await control.PressAsync("ArrowDown");
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
            await control.PressAsync("Escape");
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

/// <summary>A <c>TbColorPicker</c>, found by its label or a locator.</summary>
public sealed partial class TbColorPickerLocator(IPage page, ILocator field)
{
    /// <summary>Types the color, as <c>#rrggbb</c>, or <c>#rrggbbaa</c> for a field with <c>Alpha</c>, and waits until
    /// the field has taken it. A statically rendered field just gets the text.</summary>
    public async Task ChooseAsync(string color)
    {
        if (!HexColor().IsMatch(color))
        {
            throw new ArgumentException($"Pass the color as #rrggbb or #rrggbbaa, not {color}.", nameof(color));
        }

        var input = field.And(page.Locator("input"));
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

        // A prerendered field is replaced once the page is interactive, and its script listens only once
        // data-ready is set; the field counts the choices it has taken, so a new count means it has this one.
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
    private int? _depth;

    private ILocator Tree => page.GetByRole(AriaRole.Tree, new() { Name = name, Exact = true });

    /// <summary>The row at this path, which must be showing. When several rows match, as rows with the same text
    /// do, it is the first.</summary>
    public Task<ILocator> ItemAsync(params string[] path) => RowAsync(path);

    /// <summary>Every row at this path, for rows that share their text, such as several "P" rows under a heading. Their
    /// parents must be open. A virtualized tree doesn't render every row, so it can't list them.</summary>
    public async Task<IReadOnlyList<ILocator>> ItemsAsync(params string[] path)
    {
        await RowsBuiltAsync();
        if (await Tree.GetAttributeAsync("data-virtual") is not null)
        {
            throw new InvalidOperationException(
                $"The tree {name} is virtualized, so it can't list every row at a path.");
        }

        var keys = await Tree.EvaluateAsync<string[]>(FindRows, path);
        return [.. await Task.WhenAll(keys.Select(RowByKeyAsync))];
    }

    /// <summary>Opens every row, and the rows that appear inside them as they open, waiting for children that load. A
    /// virtualized tree opens the rows it has rendered.</summary>
    public async Task ExpandAllAsync()
    {
        await Assertions.Expect(Tree).ToHaveAttributeAsync("data-ready", "");
        await Assertions.Expect(Tree).Not.ToHaveAttributeAsync("aria-busy", "true");
        while (await Tree.EvaluateAsync<string?>("""
                   tree => [...tree.querySelectorAll('[role=treeitem][aria-expanded=false]')]
                       .find(row => row.closest('[role=tree]') === tree)?.dataset.key ?? null
                   """) is { } key)
        {
            var row = await RowByKeyAsync(key);
            await row.Locator("[data-part=toggle]").ClickAsync();
            await Assertions.Expect(row).ToHaveAttributeAsync("aria-expanded", "true");
            await Assertions.Expect(Tree.Locator("[data-part=loading]")).ToHaveCountAsync(0);
            await RowsBuiltAsync();
        }
    }

    // Rows written as TbTreeItems that a row renders once open register as rows a render later, and the tree is busy
    // until they do.
    private async Task RowsBuiltAsync()
    {
        await Assertions.Expect(Tree.Locator("[role=treeitem]").First).ToBeAttachedAsync();
        await Assertions.Expect(Tree).Not.ToHaveAttributeAsync("aria-busy", "true");
    }

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

    /// <summary>Drags the row at <paramref name="row"/> onto the top edge of the row at <paramref name="target"/>,
    /// to move it just before that row. Both rows must be showing, with their parents open.</summary>
    public Task DragBeforeAsync(string[] row, string[] target) => DragAsync(row, target, 0.1f);

    /// <summary>Drags the row at <paramref name="row"/> onto the bottom edge of the row at <paramref name="target"/>,
    /// to move it just after that row. Both rows must be showing, with their parents open.</summary>
    public Task DragAfterAsync(string[] row, string[] target) => DragAsync(row, target, 0.9f);

    /// <summary>Drags the row at <paramref name="row"/> onto the middle of the row at <paramref name="target"/>, to
    /// move it inside that row. Both rows must be showing, with their parents open.</summary>
    public Task DragIntoAsync(string[] row, string[] target) => DragAsync(row, target, 0.5f);

    // A drag near the window's edge scrolls the page, so the target is brought to the middle first, and measured again
    // once the drag has started with a short move. The drop lands at a fraction of the target row's height.
    private async Task DragAsync(string[] row, string[] target, float at)
    {
        await Assertions.Expect(Tree).ToHaveAttributeAsync("data-ready", "");
        var sourceKey = await Key(row);
        var source = await RowByKeyAsync(sourceKey);
        var destination = await RowAsync(target);
        await destination.EvaluateAsync("row => row.scrollIntoView({ block: 'center' })");
        if (!await ShowsAsync(sourceKey))
        {
            throw new InvalidOperationException($"The tree {name} can't show {string.Join(" > ", row)} and "
                + $"{string.Join(" > ", target)} together, so one can't be dragged onto the other.");
        }

        await source.ScrollIntoViewIfNeededAsync();
        var from = await BoxAsync(source, row);
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2 + 8, new() { Steps = 2 });
        var to = await BoxAsync(destination, target);
        await page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height * at, new() { Steps = 10 });
        await page.Mouse.UpAsync();
    }

    // A virtualized tree renders only the rows in view, so a row it still has must show, clipped by the tree and the
    // page as the browser measures it, to stay rendered through the drag. It checks once and never waits.
    private Task<bool> ShowsAsync(string key) => Tree.EvaluateAsync<bool>("""
        (tree, key) => {
            const row = tree.querySelector(`[role=treeitem][data-key="${CSS.escape(key)}"]`);
            if (!row || !('virtual' in tree.dataset)) {
                return !!row;
            }

            return new Promise(resolve => {
                const observer = new IntersectionObserver(([entry]) => {
                    observer.disconnect();
                    resolve(entry.intersectionRatio > 0);
                });
                observer.observe(row);
            });
        }
        """, key);

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator row, string[] path) =>
        await row.BoundingBoxAsync() ?? throw new InvalidOperationException(
            $"The row {string.Join(" > ", path)} isn't showing.");

    private async Task<ILocator> RowAsync(string[] path) => await RowByKeyAsync(await Key(path));

    // A tree inside a row numbers its keys afresh, so this tree's rows are the ones inside as many trees as this tree
    // is.
    private async Task<ILocator> RowByKeyAsync(string key)
    {
        _depth ??= await Tree.EvaluateAsync<int>("""
            tree => {
                let depth = 0;
                for (let at = tree; at; at = at.parentElement?.closest('[role=tree]')) {
                    depth++;
                }

                return depth;
            }
            """);
        return Tree.Locator(
            $"xpath=.//*[@role='treeitem'][@data-key={XPathText(key)}][count(ancestor::*[@role='tree'])={_depth}]");
    }

    // An XPath string literal, which has no escapes, so a key holding both quotes is joined from pieces.
    private static string XPathText(string text) => !text.Contains('\'') ? $"'{text}'"
        : !text.Contains('"') ? $"\"{text}\""
        : $"concat('{text.Replace("'", "',\"'\",'", StringComparison.Ordinal)}')";

    private async Task<string> Key(string[] path)
    {
        await RowsBuiltAsync();
        if (await Tree.GetAttributeAsync("data-virtual") is not null)
        {
            return await VirtualKeyAsync(path);
        }

        return (await Tree.EvaluateAsync<string[]>(FindRows, path)).FirstOrDefault() ?? throw NoRow(path);
    }

    // Each name is looked for after its parent row and before the next row at the parent's level, in every match.
    private const string FindRows = """
        (tree, path) => {
            const rows = [...tree.querySelectorAll('[role=treeitem][data-key]')]
                .filter(row => row.closest('[role=tree]') === tree);
            const found = [];
            const walk = (start, depth) => {
                for (let index = start; index < rows.length; index++) {
                    const level = Number(rows[index].getAttribute('aria-level'));
                    if (level <= depth) {
                        break;
                    }

                    if (level === depth + 1 && rows[index].dataset.text === path[depth]) {
                        if (depth === path.length - 1) {
                            found.push(rows[index].dataset.key);
                        } else {
                            walk(index + 1, depth + 1);
                        }
                    }
                }
            };
            walk(0, 0);
            return found;
        }
        """;

    // A virtualized tree renders only the rows in view. Each row says how many rows its branch holds, so the walk
    // steps from sibling to sibling over branches it doesn't need, and scrolls on only when its next row isn't there.
    private async Task<string> VirtualKeyAsync(string[] path)
    {
        var step = new PathStep();
        while (step.Found is null)
        {
            await Tree.EvaluateAsync("""
                (tree, index) => {
                    let scroller = tree;
                    while (scroller && !(scroller.scrollHeight > scroller.clientHeight
                        && getComputedStyle(scroller).overflowY !== 'visible')) {
                        scroller = scroller.parentElement;
                    }

                    const offset = index * Number(tree.dataset.rowSize);
                    if (scroller === tree) {
                        tree.scrollTop = offset;
                        return;
                    }

                    scroller ??= document.scrollingElement;
                    const top = scroller === document.scrollingElement ? 0 : scroller.getBoundingClientRect().top;
                    scroller.scrollTop += tree.getBoundingClientRect().top + tree.clientTop + offset - top;
                }
                """, step.Index);
            await Assertions.Expect(Tree.Locator($"[role=treeitem][data-index=\"{step.Index}\"]")).ToBeAttachedAsync();
            step = await Tree.EvaluateAsync<PathStep>("""
                (tree, { path, index, depth }) => {
                    const total = Number(tree.dataset.rows);
                    while (index < total) {
                        const row = tree.querySelector(`[role=treeitem][data-index="${index}"]`);
                        if (!row) {
                            return { index, depth, found: null };
                        }

                        const level = Number(row.getAttribute('aria-level'));
                        if (level <= depth) {
                            break;
                        }

                        if (row.dataset.key && row.dataset.text === path[depth]) {
                            if (depth === path.length - 1) {
                                return { index, depth, found: row.dataset.key };
                            }

                            depth++;
                            index++;
                        } else {
                            index += 1 + Number(row.dataset.inside);
                        }
                    }

                    return { index, depth, found: '' };
                }
                """, new { path, index = step.Index, depth = step.Depth });
        }

        return step.Found.Length > 0 ? step.Found : throw NoRow(path);
    }

    private InvalidOperationException NoRow(string[] path) =>
        new($"The tree {name} shows no row at {string.Join(" > ", path)}; open the rows above it first.");

    // Where a walk through a virtualized tree has reached; Found is the row's key, or empty when there's no such row.
    // Playwright fills in a result's properties, so they are settable.
    private sealed class PathStep
    {
        public int Index { get; set; }

        public int Depth { get; set; }

        public string? Found { get; set; }
    }
}

/// <summary>A <c>TbListBox</c> or <c>TbMultiListBox</c>, found by its label.</summary>
public sealed class TbListBoxLocator(IPage page, string label)
{
    /// <summary>Chooses the options with these names: the one option of a list box, or each one not yet chosen in a
    /// multiple list box, keeping what is already chosen. A statically rendered list box is a native list, and is
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

/// <summary>A <c>TbDataGrid</c>, found by its table's accessible name. Rows are found by the exact text of one of
/// their cells, and columns by their header's title.</summary>
public sealed class TbGridLocator(IPage page, string name)
{
    private ILocator Table => page.GetByRole(AriaRole.Table, new() { Name = name, Exact = true });

    private ILocator Root => page.Locator("[data-part=grid]").Filter(new() { Has = Table });

    /// <summary>The row with a cell whose text is exactly this.</summary>
    public ILocator Row(string cell) => Table.Locator("tbody tr[data-part=row]").Filter(new()
    {
        Has = page.Locator("td", new() { HasTextRegex = new Regex($"^{Regex.Escape(cell)}$") }),
    });

    /// <summary>The cell in the column with this title, of the row with a cell whose text is exactly
    /// <paramref name="row"/>.</summary>
    public async Task<ILocator> CellAsync(string row, string column)
    {
        var id = await Header(column).GetAttributeAsync("data-column");
        return Row(row).Locator($"td[data-column=\"{id}\"]");
    }

    /// <summary>Sorts the rows by this column alone, ascending or descending, clicking its header until it shows that
    /// order, and waits for a <c>DataProvider</c>'s rows.</summary>
    public async Task SortAsync(string column, bool descending = false)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var header = Header(column);
        var wanted = descending ? "descending" : "ascending";
        // A plain click sorts by the column alone; after that it reverses the order, then stops sorting by the column.
        const string state = "header => `${header.getAttribute('aria-sort')}|${header.closest('table')"
            + ".querySelectorAll('[data-part=sort-order]').length}`";
        for (var clicks = 0; clicks < 4; clicks++)
        {
            var before = await header.EvaluateAsync<string>(state);
            if (before == $"{wanted}|0")
            {
                break;
            }

            await header.Locator("[data-part=sort]").ClickAsync();
            await page.WaitForFunctionAsync($"([header, before]) => ({state})(header) !== before",
                new object[] { await header.ElementHandleAsync(), before });
        }

        await Assertions.Expect(header).ToHaveAttributeAsync("aria-sort", wanted);
        await RowsLoadedAsync();
    }

    /// <summary>Selects the row with a cell whose text is exactly this, with its checkbox or radio button.</summary>
    public async Task SelectAsync(string row)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        await Row(row).Locator("[data-part=select] input").CheckAsync();
        await Assertions.Expect(Row(row)).ToHaveAttributeAsync("data-selected", "");
    }

    /// <summary>Filters the column with this title by this text, then waits for a <c>DataProvider</c>'s rows. In the
    /// filter row it types the text and presses Enter, so a text filter applies without waiting for typing to pause;
    /// in a filter menu it types the text as the value and applies the menu. For a text or number filter; choose a
    /// row's choice filter with <c>page.Tb().Select("Filter Status")</c>, or a list filter's choices with
    /// <see cref="FilterChoicesAsync"/>. Empty text clears the filter.</summary>
    public async Task FilterAsync(string column, string text)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var menu = Menu(column);
        if (await menu.CountAsync() > 0)
        {
            var panel = await OpenAsync(menu);
            await panel.Locator(ValueSelector).FillAsync(text);
            await ApplyAsync(panel);
            return;
        }

        var id = await Header(column).GetAttributeAsync("data-column");
        var cell = Table.Locator($"[data-part=filter][data-column=\"{id}\"]");
        var input = cell.Locator("input");
        await input.FillAsync(text);
        await input.PressAsync("Enter");
        // The cell shows the filter the grid applied, as invariant text, in the render that starts loading its rows. A
        // number box holds invariant text too, but rewrites what was typed, such as .5 as 0.5.
        if (text.Length == 0)
        {
            await Assertions.Expect(cell).Not.ToHaveAttributeAsync("data-value", new Regex(""));
        }
        else
        {
            var applied = await input.GetAttributeAsync("type") == "number"
                && decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number.ToString(CultureInfo.InvariantCulture) : text;
            await Assertions.Expect(cell).ToHaveAttributeAsync("data-value", applied);
        }

        await RowsLoadedAsync();
    }

    /// <summary>In the filter menu of the column with this title, chooses the condition with this name, such as
    /// "At least", types the text as the value, unless the condition takes none, applies the menu, and waits for a
    /// <c>DataProvider</c>'s rows.</summary>
    public async Task FilterAsync(string column, string condition, string text)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var panel = await OpenAsync(Menu(column));
        await new TbSelectLocator(page, panel.Locator("[role=combobox]")).ChooseAsync(condition);
        if (await panel.Locator(ValueSelector).CountAsync() > 0)
        {
            await panel.Locator(ValueSelector).FillAsync(text);
        }

        await ApplyAsync(panel);
    }

    /// <summary>Leaves exactly these choices picked in the list filter of the column with this title, naming each as
    /// the filter shows it; pass none to clear the filter. Works with the filter row's multiple select and with a
    /// filter menu's checkboxes, which it then applies. Then it waits for a <c>DataProvider</c>'s rows.</summary>
    public async Task FilterChoicesAsync(string column, params string[] choices)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var menu = Menu(column);
        if (await menu.CountAsync() == 0)
        {
            var id = await Header(column).GetAttributeAsync("data-column");
            var cell = Table.Locator($"[data-part=filter][data-column=\"{id}\"]");
            // Clearing first leaves only the choices asked for; the filter's value goes in the same render.
            if (await cell.GetAttributeAsync("data-value") is not null)
            {
                await cell.Locator("[data-part=clear]").ClickAsync();
                await Assertions.Expect(cell).Not.ToHaveAttributeAsync("data-value", new Regex(""));
            }

            if (choices.Length > 0)
            {
                await new TbSelectLocator(page, cell.Locator("[role=combobox]")).ChooseAsync(choices);
            }

            await RowsLoadedAsync();
            return;
        }

        // A menu without checkboxes isn't a list, so reject it before opening the menu.
        var labels = await menu.Locator("[data-part=panel] label:has(input[type=checkbox])").AllAsync();
        if (labels.Count == 0)
        {
            throw new InvalidOperationException(
                $"The \"{column}\" column's filter isn't a list; give the column FilterChoices.");
        }

        var names = new List<string>();
        foreach (var label in labels)
        {
            names.Add((await label.TextContentAsync() ?? "").Trim());
        }

        if (choices.FirstOrDefault(choice => !names.Contains(choice)) is { } missing)
        {
            throw new InvalidOperationException($"The \"{column}\" column's filter has no choice \"{missing}\".");
        }

        var panel = await OpenAsync(menu);
        for (var at = 0; at < labels.Count; at++)
        {
            var check = choices.Contains(names[at]);
            await labels[at].Locator("input").SetCheckedAsync(check);
        }

        await ApplyAsync(panel);
    }

    private const string ValueSelector = "[data-part=input]:not([role=combobox]):not([type=checkbox])";

    private ILocator Menu(string column) => Header(column).Locator("[data-part=filter-menu]");

    private static async Task<ILocator> OpenAsync(ILocator menu)
    {
        var panel = menu.Locator("[data-part=panel]");
        await menu.Locator("button").First.ClickAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();
        return panel;
    }

    // Applying closes the menu in the render that starts loading the rows.
    private async Task ApplyAsync(ILocator panel)
    {
        if (await panel.Locator("[data-part=filter-apply]").CountAsync() == 0)
        {
            throw new InvalidOperationException("The column's filter is a template; use the controls it draws.");
        }

        await panel.Locator("[data-part=filter-apply]").ClickAsync();
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await RowsLoadedAsync();
    }

    /// <summary>Goes to this page of the rows and waits until it is current and a <c>DataProvider</c>'s rows have
    /// loaded.</summary>
    public async Task GoToPageAsync(int number)
    {
        var pages = Root.Locator("[data-part=pager] nav");
        await Assertions.Expect(pages).ToHaveAttributeAsync("aria-label", new Regex("."));
        await new TbPaginationLocator(page, (await pages.GetAttributeAsync("aria-label"))!).GoToPageAsync(number);
        await RowsLoadedAsync();
    }

    // The render that changes the header or pager marks the table busy until the rows load.
    private Task RowsLoadedAsync() => Assertions.Expect(Table).Not.ToHaveAttributeAsync("aria-busy", "true");

    private ILocator Header(string column) => Table.Locator($"thead th[data-title=\"{column}\"]");
}

/// <summary>A <c>TbPagination</c>, found by its accessible name.</summary>
public sealed class TbPaginationLocator(IPage page, string name)
{
    private ILocator Navigation => page.GetByRole(AriaRole.Navigation, new() { Name = name, Exact = true });

    /// <summary>Goes to this page and waits until it is current. A compact pagination gets the number typed into its
    /// box; otherwise the page's button is clicked, after stepping toward it through the shown pages when it isn't
    /// among them. Buttons wait for the page to be interactive; links, from <c>PageHref</c>, are followed at once.
    /// Throws for a page past the last.</summary>
    public async Task GoToPageAsync(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        // Waiting for the current page and page count it renders also waits for the pagination to be on the page.
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
