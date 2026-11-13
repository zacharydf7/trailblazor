using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>Playwright helpers that find Trailblazor components by role and label, as a user would, so tests keep
/// working when the markup inside a component changes.</summary>
public static class TrailblazorPageExtensions
{
    /// <summary>The Trailblazor helpers for this page.</summary>
    public static TrailblazorPage Tb(this IPage page) => new(page.Locator(":root"));

    /// <summary>The Trailblazor helpers for the components inside what this finds, such as an open dialog or a grid
    /// row, so a label used again elsewhere on the page doesn't get in the way. Dialogs, toasts, announcements and the
    /// lists a field opens are still found anywhere on the page, but only while the scope is on it; once it's gone,
    /// as when a dialog you scoped to has closed, use <c>page.Tb()</c> for them.</summary>
    public static TrailblazorPage Tb(this ILocator scope) => new(scope);

    /// <summary>The Trailblazor helpers for the page in this frame.</summary>
    public static TrailblazorPage Tb(this IFrameLocator frame) => new(frame.Locator(":root"));
}

internal static class Scope
{
    // What a component's aria-controls names, and what lives at the page's root, such as dialogs, toasts and the
    // announcer, can be outside a scope, so they're found in the scope's document.
    public static ILocator Document(ILocator scope) => scope.Locator("xpath=ancestor-or-self::html").First;

    // A trigger that waits for its first use, such as a move handle's menu, is marked once interactive and starts its
    // script on focus; blurring first makes sure a trigger that already has focus fires a focus event.
    public static async Task ReadyAsync(ILocator scope, ILocator trigger)
    {
        await trigger.And(scope.Locator("[data-ready], [data-attach-on-use]")).WaitForAsync();
        if (await trigger.GetAttributeAsync("data-attach-on-use") is not null)
        {
            await trigger.BlurAsync();
            await trigger.FocusAsync();
        }

        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-ready", "");
    }
}

/// <summary>The Trailblazor components on a page, or in one part of it.</summary>
public sealed class TrailblazorPage(ILocator scope)
{
    /// <summary>The <c>TbSelect</c> or <c>TbMultiSelect</c> with this label.</summary>
    public TbSelectLocator Select(string label) => new(scope, scope.GetByLabel(FieldLabel.Matching(label)));

    /// <summary>The <c>TbSelect</c> or <c>TbMultiSelect</c> this finds, inside the scope, such as one in a row of
    /// fields that share their labels, as <c>row.GetByLabel("Size")</c>, or one with an id, as
    /// <c>page.Locator("#size")</c>.</summary>
    public TbSelectLocator Select(ILocator field) => new(scope, field);

    /// <summary>The <c>TbAutocomplete</c> with this label.</summary>
    public TbAutocompleteLocator Autocomplete(string label) => new(scope, label);

    /// <summary>The <c>TbListBox</c> or <c>TbMultiListBox</c> with this label.</summary>
    public TbListBoxLocator ListBox(string label) => new(scope, label);

    /// <summary>The <c>TbTransferList</c> with this label.</summary>
    public TbTransferListLocator TransferList(string label) => new(scope, label);

    /// <summary>The <c>TbColorPicker</c> with this label.</summary>
    public TbColorPickerLocator ColorPicker(string label) => new(scope, scope.GetByLabel(FieldLabel.Matching(label)));

    /// <summary>The <c>TbColorPicker</c> this finds, inside the scope, such as one in a row of fields that share their
    /// labels, as <c>row.GetByLabel("Color")</c>, or one with an id, as <c>page.Locator("#color")</c>.</summary>
    public TbColorPickerLocator ColorPicker(ILocator field) => new(scope, field);

    /// <summary>The <c>TbFileUpload</c> with this label.</summary>
    public TbFileUploadLocator FileUpload(string label) => new(scope, label);

    /// <summary>The <c>TbDateRangePicker</c> with this label.</summary>
    public TbDateRangePickerLocator DateRangePicker(string label) => new(scope, label);

    /// <summary>The <c>TbAccordion</c> item with this header.</summary>
    public TbAccordionItemLocator AccordionItem(string header) => new(scope, header);

    /// <summary>The <c>TbTooltip</c> around this trigger.</summary>
    public TbTooltipLocator Tooltip(ILocator trigger) => new(trigger);

    /// <summary>The <c>TbSplitter</c> separator with this name, such as "Resize Trails".</summary>
    public TbSplitterLocator Splitter(string separator) => new(scope, separator);

    /// <summary>The <c>TbQueryBuilder</c> with this <c>Label</c>.</summary>
    public TbQueryBuilderLocator QueryBuilder(string label) => new(scope, label);

    /// <summary>The <c>TbViewport</c> whose box has this <c>Label</c>.</summary>
    public TbViewportLocator Viewport(string label) => new(scope, label);

    /// <summary>The <c>TbSlider</c> with this label.</summary>
    public TbSliderLocator Slider(string label) => new(scope, label);

    /// <summary>The <c>TbRangeSlider</c> with this label.</summary>
    public TbRangeSliderLocator RangeSlider(string label) => new(scope, label);

    /// <summary>The <c>TbDatePicker</c> with this label.</summary>
    public TbDatePickerLocator DatePicker(string label) => new(scope, label);

    /// <summary>The <c>TbDateTimePicker</c> with this label.</summary>
    public TbDateTimePickerLocator DateTimePicker(string label) => new(scope, label);

    /// <summary>The <c>TbCalendar</c> with this label.</summary>
    public TbCalendarLocator Calendar(string label) => new(scope, label);

    /// <summary>The <c>TbTreeSelect</c> or <c>TbMultiTreeSelect</c> with this label.</summary>
    public TbTreeSelectLocator TreeSelect(string label) => new(scope, label);

    /// <summary>The <c>TbTreeView</c> with this accessible name.</summary>
    public TbTreeLocator Tree(string name) => new(scope, name);

    /// <summary>The <c>TbPagination</c> with this accessible name.</summary>
    public TbPaginationLocator Pagination(string name) => new(scope, name);

    /// <summary>The <c>TbDataGrid</c> whose table has this accessible name.</summary>
    public TbGridLocator Grid(string name) => new(scope, name);

    /// <summary>The <c>TbCommandPalette</c> with this label, or the only one in the scope.</summary>
    public TbCommandPaletteLocator CommandPalette(string? name = null) => new(scope, name);

    /// <summary>The <c>TbHotkey</c> with these keys, written as the hotkey's <c>Keys</c> are, such as
    /// <c>"Mod+S"</c>.</summary>
    public TbHotkeyLocator Hotkey(string keys) => new(scope, keys);

    /// <summary>The <c>TbSortable</c> with this label.</summary>
    public TbSortableLocator Sortable(string name) => new(scope, name);

    /// <summary>The <c>TbPopover</c> whose trigger has this name.</summary>
    public TbPopoverLocator Popover(string trigger) => new(scope, trigger);

    /// <summary>The <c>TbPopconfirm</c> whose trigger has this name.</summary>
    public TbPopconfirmLocator Popconfirm(string trigger) => new(new TbPopoverLocator(scope, trigger));

    /// <summary>The fields of the <c>TbPropertyGrid</c> with this label, found by their labels as anywhere else:
    /// <c>page.Tb().PropertyGrid("Trail properties").NumberField("Length (km)")</c>.</summary>
    public TrailblazorPage PropertyGrid(string label) =>
        new(scope.GetByRole(AriaRole.Group, new() { Name = label, Exact = true }).And(scope.Locator(".tb-property-grid")));

    /// <summary>The <c>TbMenu</c> whose trigger has this name.</summary>
    public TbMenuLocator Menu(string trigger) => new(scope, trigger);

    /// <summary>The <c>TbContextMenu</c> that opens on this element, such as <c>page.GetByText("Ridge loop")</c>.
    /// </summary>
    public TbContextMenuLocator ContextMenu(ILocator at) => new(scope, at);

    /// <summary>The dialog with this title.</summary>
    public TbDialogLocator Dialog(string title) => new(Scope.Document(scope), title);

    /// <summary>The <c>TbTabs</c> with this accessible name, or the only one in the scope when no name is
    /// given.</summary>
    public TbTabsLocator Tabs(string? name = null) => new(scope, name);

    /// <summary>The toasts on the page.</summary>
    public TbToastsLocator Toasts => new(Scope.Document(scope));

    /// <summary>What the page's components say to screen readers.</summary>
    public TbAnnouncementsLocator Announcements => new(Scope.Document(scope));

    /// <summary>The <c>TbNumberField</c> with this label.</summary>
    public TbNumberFieldLocator NumberField(string label) => new(scope, label);

    /// <summary>The <c>TbTagInput</c> with this label.</summary>
    public TbTagInputLocator TagInput(string label) => new(scope, label);

    /// <summary>The <c>TbRichTextEditor</c> with this label.</summary>
    public TbRichTextEditorLocator RichTextEditor(string label) => new(scope, label);
}

/// <summary>A <c>TbPopover</c>, found by its trigger's name.</summary>
public sealed class TbPopoverLocator(ILocator scope, string trigger)
{
    private ILocator Trigger => scope.GetByRole(AriaRole.Button, new() { Name = trigger, Exact = true });

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
            await scope.Page.Keyboard.PressAsync("Escape");
            await Assertions.Expect(panel).ToBeHiddenAsync();
            await SettledAsync(open: false);
        }
    }

    // Waits until the panel has closed by itself, as a popconfirm's does once it's answered. The answer may remove the
    // trigger, as a delete in a list does, so a trigger that's gone counts as closed.
    internal async Task ClosedAsync(ILocator panel)
    {
        await Assertions.Expect(panel).ToBeHiddenAsync();
        await Assertions.Expect(Trigger.And(scope.Locator("[aria-expanded=true]"))).ToHaveCountAsync(0);
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
            await Scope.ReadyAsync(scope, Trigger);
        }

        return Scope.Document(scope).Locator($"[id='{await Trigger.GetAttributeAsync("aria-controls")}']");
    }
}

/// <summary>A <c>TbPopconfirm</c>, found by its trigger's name.</summary>
public sealed class TbPopconfirmLocator
{
    private readonly TbPopoverLocator _popover;

    internal TbPopconfirmLocator(TbPopoverLocator popover) => _popover = popover;

    /// <summary>Opens the confirmation, presses its confirm button and waits until it has closed, which it does once
    /// the app's <c>OnConfirm</c> has run.</summary>
    public Task ConfirmAsync() => PressAsync("confirm");

    /// <summary>Opens the confirmation, presses its cancel button and waits until it has closed.</summary>
    public Task CancelAsync() => PressAsync("cancel");

    private async Task PressAsync(string part)
    {
        var panel = await _popover.OpenAsync();
        await panel.Locator($"[data-part={part}]").ClickAsync();
        await _popover.ClosedAsync(panel);
    }
}

/// <summary>A <c>TbSelect</c> or <c>TbMultiSelect</c>, found by its label or a locator.</summary>
public sealed class TbSelectLocator(ILocator scope, ILocator field)
{
    /// <summary>Chooses the options with these names: the one option of a select, or each one not yet chosen in a
    /// multiple select, keeping what is already chosen. A statically rendered select is a native one, and is chosen
    /// the native way; one still prerendering is waited for.</summary>
    public async Task ChooseAsync(params string[] options)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        if (await NativeSelect.TryChooseAsync(Control, options, "select"))
        {
            return;
        }

        await SetChosenAsync(options, choose: true);
    }

    /// <summary>Unchooses the options with these names in a multiple select, as clicking them again does, leaving the
    /// others chosen. A select that takes one option throws; clear it with <see cref="ClearAsync"/>. On a static page
    /// this throws too, since a native select posts with its form.</summary>
    public async Task UnchooseAsync(params string[] options)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        await NativeSelect.ThrowIfStaticAsync(Control);
        await SetChosenAsync(options, choose: false);
    }

    /// <summary>Clears the select with its clear button, as a user does, and waits until nothing is chosen. A select
    /// that isn't <c>Clearable</c>, or has nothing chosen, shows no clear button, so this throws, as it does on a
    /// static page.</summary>
    public async Task ClearAsync()
    {
        var control = Control;
        await NativeSelect.ThrowIfStaticAsync(control);
        await Scope.ReadyAsync(scope, control);
        var clear = control.Locator("xpath=ancestor::*[@data-part='control'][1]").Locator("[data-part=clear]");
        if (await clear.CountAsync() == 0)
        {
            throw new InvalidOperationException(
                "The select shows no clear button; make it Clearable, and choose an option first.");
        }

        await clear.ClickAsync();
        await Assertions.Expect(control.Locator("[data-part=value], [data-part=chip]")).ToHaveCountAsync(0);
    }

    /// <summary>The name of the select's value, as a screen reader hears it, or null while it shows its placeholder. A
    /// multiple select's chosen options are joined with ", ".</summary>
    public async Task<string?> TextAsync()
    {
        var shown = await Control.EvaluateAsync<string[]>(Choices.ShownNames);
        return shown.Length > 0 ? string.Join(", ", shown) : null;
    }

    /// <summary>The names of the chosen options, in order. A list of more than 100 options shows only those in view,
    /// so for one that long this throws; read <see cref="TextAsync"/> instead.</summary>
    public async Task<IReadOnlyList<string>> ChosenAsync()
    {
        var control = Control;
        // A prerendered select is replaced by the interactive one, which holds its own list.
        await Assertions.Expect(control).Not.ToHaveAttributeAsync("data-prerender", "");
        if (await control.EvaluateAsync<bool>("control => control.localName === 'select'"))
        {
            return await control.Locator(NativeSelect.Chosen).AllInnerTextsAsync();
        }

        var listbox = await ListboxAsync(control);
        if (await listbox.GetAttributeAsync("data-count") is not null)
        {
            throw new InvalidOperationException(
                "The list shows only the options in view, so their names can't all be read; read TextAsync instead.");
        }

        return await listbox.EvaluateAsync<string[]>(Choices.ChosenNames);
    }

    // The list shares the label, so only the combobox, or the native select it replaces, counts as the field.
    private ILocator Control => @field.And(scope.Locator("[role=combobox], select"));

    // A filterable select points to the dialog that holds its filter and listbox.
    private async Task<ILocator> ListboxAsync(ILocator control)
    {
        var controls = await control.GetAttributeAsync("aria-controls");
        return Scope.Document(scope).Locator($"[id='{controls}'][role=listbox], [id='{controls}'] [role=listbox]");
    }

    private async Task SetChosenAsync(string[] options, bool choose)
    {
        var control = Control;
        // The keys and clicks work once the select's script has attached.
        await Scope.ReadyAsync(scope, control);
        var listbox = await ListboxAsync(control);
        var multiple = await listbox.GetAttributeAsync("aria-multiselectable") == "true";
        if (choose)
        {
            NativeSelect.ThrowIfSeveral(options, multiple, "select");
        }
        else if (!multiple)
        {
            throw new InvalidOperationException("A select that takes one option can't unchoose it; clear it instead.");
        }

        // The keyboard opens the list, since a click could land on a chip's remove button.
        if (!await listbox.IsVisibleAsync())
        {
            await control.FocusAsync();
            await control.PressAsync("ArrowDown");
        }

        var state = choose ? "true" : "false";
        foreach (var name in options)
        {
            // A single select closes once chosen, so the option is found even while its list is hidden.
            var option = listbox.GetByRole(AriaRole.Option, new() { Name = name, Exact = true, IncludeHidden = true });
            if (await option.GetAttributeAsync("aria-selected") != state)
            {
                await option.ClickAsync();
                // The server applies the choice, so wait for it before reading the next option's state.
                await Assertions.Expect(option).ToHaveAttributeAsync("aria-selected", state);
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
public sealed class TbAutocompleteLocator(ILocator scope, string label)
{
    /// <summary>Types the text, waits for the suggestions, and chooses the option with this name, waiting until the
    /// field has taken it. A statically rendered field is a plain input, which just gets the text.</summary>
    public async Task ChooseAsync(string text, string option)
    {
        var input = scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("input"));
        if (await input.GetAttributeAsync("data-static") is not null)
        {
            await input.FillAsync(text);
            return;
        }

        // Typing opens the list once the field's script has attached.
        await Assertions.Expect(input).ToHaveAttributeAsync("data-ready", "");
        await input.FillAsync(text);
        var listbox = Scope.Document(scope).Locator($"[id='{await input.GetAttributeAsync("aria-controls")}']");
        await listbox.GetByRole(AriaRole.Option, new() { Name = option, Exact = true }).ClickAsync();
        // The list closes as the option is clicked, and the server marks the choice a round trip later.
        await Assertions.Expect(listbox.GetByRole(AriaRole.Option,
            new() { Name = option, Exact = true, IncludeHidden = true })).ToHaveAttributeAsync("aria-selected", "true");
    }
}

/// <summary>A <c>TbColorPicker</c>, found by its label or a locator.</summary>
public sealed partial class TbColorPickerLocator(ILocator scope, ILocator field)
{
    /// <summary>Types the color, as <c>#rrggbb</c>, or <c>#rrggbbaa</c> for a field with <c>Alpha</c>, and waits until
    /// the field has taken it. A statically rendered field just gets the text.</summary>
    public async Task ChooseAsync(string color)
    {
        if (!HexColor().IsMatch(color))
        {
            throw new ArgumentException($"Pass the color as #rrggbb or #rrggbbaa, not {color}.", nameof(color));
        }

        var input = field.And(scope.Locator("input"));
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
public sealed class TbFileUploadLocator(ILocator scope, string label)
{
    /// <summary>Chooses the files at these paths, as dropping them or picking them in the file dialog would, and waits
    /// until the field has taken the choice. A statically rendered field just gets the files.</summary>
    public Task ChooseAsync(params string[] paths) => ChooseAsync(input => input.SetInputFilesAsync(paths));

    /// <summary>Chooses files made in the test, each with a <c>Name</c>, <c>MimeType</c> and <c>Buffer</c>, and waits
    /// until the field has taken the choice.</summary>
    public Task ChooseAsync(params FilePayload[] files) => ChooseAsync(input => input.SetInputFilesAsync(files));

    private async Task ChooseAsync(Func<ILocator, Task> choose)
    {
        var input = scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("input[type=file]"));
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
public sealed class TbDateRangePickerLocator(ILocator scope, string label)
{
    /// <summary>Types both dates in the field's own format and waits until the field has taken them; null leaves a
    /// box empty. A statically rendered field gets them in the browser's own date boxes.</summary>
    public async Task ChooseAsync(DateOnly? start, DateOnly? end)
    {
        var group = scope.GetByRole(AriaRole.Group, new() { Name = label, Exact = true });
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
        var culture = GregorianCulture.Named(await group.GetAttributeAsync("data-culture"));
        await boxes.Nth(0).FillAsync(start?.ToString(format, culture) ?? "");
        await boxes.Nth(1).FillAsync(end?.ToString(format, culture) ?? "");
        await boxes.Nth(1).PressAsync("Enter");
        await Assertions.Expect(group).ToHaveAttributeAsync("data-value", $"{Iso(start)}/{Iso(end)}");
    }

    /// <summary>The range the field holds; either date is null when the field has none. For a statically rendered
    /// field, this reads the browser's own date boxes.</summary>
    public async Task<(DateOnly? Start, DateOnly? End)> RangeAsync()
    {
        var group = scope.GetByRole(AriaRole.Group, new() { Name = label, Exact = true });
        var boxes = group.Locator("input");
        if (await boxes.First.GetAttributeAsync("data-static") is not null)
        {
            return (TbDatePickerLocator.ReadIso(await boxes.Nth(0).InputValueAsync()),
                TbDatePickerLocator.ReadIso(await boxes.Nth(1).InputValueAsync()));
        }

        var value = await group.GetAttributeAsync("data-value")
            ?? throw new InvalidOperationException($"The date range picker \"{label}\" doesn't report its range.");
        var dates = value.Split('/');
        return (TbDatePickerLocator.ReadIso(dates[0]), TbDatePickerLocator.ReadIso(dates[1]));
    }

    private static string Iso(DateOnly? date) => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
}

/// <summary>A <c>TbTreeView</c>, found by its accessible name. Rows are named by their path of texts from a root,
/// such as <c>("North", "Ridge")</c>.</summary>
public sealed class TbTreeLocator(ILocator scope, string name)
{
    private int? _depth;

    // A tree with no rows is a group, so that it can hold its message.
    private ILocator Tree => scope.GetByRole(AriaRole.Tree, new() { Name = name, Exact = true })
        .Or(scope.GetByRole(AriaRole.Group, new() { Name = name, Exact = true }).And(scope.Locator(".tb-tree")));

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

    // A drag near the window's edge scrolls the page, so the target is brought to the middle first.
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
        await PointerDrag.DragAsync(scope.Page, await BoxAsync(source, row), () => BoxAsync(destination, target), at);
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
public sealed class TbListBoxLocator(ILocator scope, string label)
{
    /// <summary>Chooses the options with these names: the one option of a list box, or each one not yet chosen in a
    /// multiple list box, keeping what is already chosen. A statically rendered list box is a native list, and is
    /// chosen the native way; one still prerendering is waited for.</summary>
    public async Task ChooseAsync(params string[] options)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        if (await NativeSelect.TryChooseAsync(Field, options, "listbox"))
        {
            return;
        }

        await SetChosenAsync(options, choose: true);
    }

    /// <summary>Unchooses the options with these names in a multiple list box, as clicking them again does, leaving
    /// the others chosen. A list box that takes one option throws, since a click can't unchoose its option, and so
    /// does a static one, whose native list posts with its form.</summary>
    public async Task UnchooseAsync(params string[] options)
    {
        ArgumentOutOfRangeException.ThrowIfZero(options.Length, nameof(options));
        await NativeSelect.ThrowIfStaticAsync(Field);
        await SetChosenAsync(options, choose: false);
    }

    /// <summary>Unchooses every chosen option in a multiple list box.</summary>
    public async Task ClearAsync()
    {
        if (await ChosenAsync() is { Count: > 0 } chosen)
        {
            await UnchooseAsync([.. chosen]);
        }
    }

    /// <summary>The names of the chosen options, in order.</summary>
    public async Task<IReadOnlyList<string>> ChosenAsync()
    {
        // A prerendered list box is replaced by the interactive one.
        await Assertions.Expect(Field).Not.ToHaveAttributeAsync("data-prerender", "");
        // A native list box has no placeholder, so every chosen option counts.
        return await Field.EvaluateAsync<bool>("list => list.localName === 'select'")
            ? await Field.Locator("option:checked").AllInnerTextsAsync()
            : await Field.EvaluateAsync<string[]>(Choices.ChosenNames);
    }

    private ILocator Field => scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("[role=listbox], select"));

    private async Task SetChosenAsync(string[] options, bool choose)
    {
        var field = Field;
        // The keys and clicks work once the listbox's script has attached.
        await Assertions.Expect(field).ToHaveAttributeAsync("data-ready", "");
        var multiple = await field.GetAttributeAsync("aria-multiselectable") == "true";
        if (choose)
        {
            NativeSelect.ThrowIfSeveral(options, multiple, "listbox");
        }
        else if (!multiple)
        {
            throw new InvalidOperationException("A list box that takes one option can't unchoose it by clicking.");
        }

        var state = choose ? "true" : "false";
        foreach (var name in options)
        {
            var option = field.GetByRole(AriaRole.Option, new() { Name = name, Exact = true });
            if (await option.GetAttributeAsync("aria-selected") != state)
            {
                await option.ClickAsync();
                // The server applies the choice, so wait for it before reading the next option's state.
                await Assertions.Expect(option).ToHaveAttributeAsync("aria-selected", state);
            }
        }
    }
}

/// <summary>A <c>TbTransferList</c>, found by its label.</summary>
public sealed class TbTransferListLocator(ILocator scope, string label)
{
    private ILocator Root => scope.GetByRole(AriaRole.Group, new() { NameRegex = FieldLabel.Matching(label) })
        .And(scope.Locator("fieldset.tb-transfer-list"));

    /// <summary>The chosen items' text, in order.</summary>
    public Task<IReadOnlyList<string>> ChosenAsync() =>
        Root.Locator("[data-part=chosen] [role=option]").AllInnerTextsAsync();

    /// <summary>Marks the available items with this text, moves them to the chosen list with its button, and waits
    /// for them to arrive.</summary>
    public Task MoveAsync(params string[] items) => MoveAsync("available", "chosen", "move-chosen", items);

    /// <summary>Marks the chosen items with this text, moves them back to the available list with its button, and
    /// waits for them to arrive.</summary>
    public Task MoveBackAsync(params string[] items) => MoveAsync("chosen", "available", "move-available", items);

    private async Task MoveAsync(string from, string to, string button, string[] items)
    {
        ArgumentOutOfRangeException.ThrowIfZero(items.Length, nameof(items));
        var list = Root.Locator($"[data-part={from}] [role=listbox]");
        // The keys and clicks work once the list's script has attached.
        await Assertions.Expect(list).ToHaveAttributeAsync("data-ready", "");
        foreach (var item in items)
        {
            var option = list.GetByRole(AriaRole.Option, new() { Name = item, Exact = true });
            if (await option.GetAttributeAsync("aria-selected") != "true")
            {
                await option.ClickAsync();
                await Assertions.Expect(option).ToHaveAttributeAsync("aria-selected", "true");
            }
        }

        await Root.Locator($"[data-part={button}]").ClickAsync();
        foreach (var item in items)
        {
            await Assertions.Expect(Root.Locator($"[data-part={to}]")
                .GetByRole(AriaRole.Option, new() { Name = item, Exact = true })).ToHaveCountAsync(1);
        }
    }
}

/// <summary>A <c>TbDataGrid</c>, found by its table's accessible name. Rows are found by the exact text of one of
/// their cells, and columns by their header's title.</summary>
public sealed class TbGridLocator(ILocator scope, string name)
{
    // With cell navigation the table is a grid to assistive technology, and with child rows a tree grid.
    private ILocator Table => scope.GetByRole(AriaRole.Table, new() { Name = name, Exact = true })
        .Or(scope.GetByRole(AriaRole.Grid, new() { Name = name, Exact = true }))
        .Or(scope.GetByRole(AriaRole.Treegrid, new() { Name = name, Exact = true }));

    private ILocator Root => Table.Locator("xpath=ancestor::*[@data-part='grid'][1]");

    /// <summary>The row with a cell whose text is exactly this.</summary>
    public ILocator Row(string cell) => Table.Locator("tbody tr[data-part=row] > td")
        .Filter(new() { HasTextRegex = new Regex($"^{Regex.Escape(cell)}$") }).Locator("xpath=parent::tr");

    /// <summary>The cell in the column with this title, of the row with a cell whose text is exactly
    /// <paramref name="row"/>.</summary>
    public async Task<ILocator> CellAsync(string row, string column)
    {
        var id = await Header(column).GetAttributeAsync("data-column");
        return Row(row).Locator($"td[data-column=\"{id}\"]");
    }

    /// <summary>Clicks the Save changes button of a grid whose <c>EditMode</c> is <c>Batch</c>, and waits until the app
    /// has taken the changes or shown why it can't. Throws when the grid holds no changes or doesn't edit in
    /// batches; an edit still open doesn't count as a change until it ends.</summary>
    public async Task SaveChangesAsync()
    {
        var save = await BatchButtonAsync("save-changes");
        await Commit.WaitAsync(save, async () =>
        {
            await save.ClickAsync();
            return true;
        });
    }

    /// <summary>Clicks the Discard changes button of a grid whose <c>EditMode</c> is <c>Batch</c>, and waits until every
    /// cell shows its item's value again. Throws when the grid holds no changes or doesn't edit in batches;
    /// an edit still open doesn't count as a change until it ends.</summary>
    public async Task DiscardChangesAsync()
    {
        var discard = await BatchButtonAsync("discard-changes");
        await discard.ClickAsync();
        await Assertions.Expect(discard).ToHaveAttributeAsync("aria-disabled", "true");
    }

    // A change that just landed enables the button only after the app re-renders, so wait briefly for that, then throw
    // if it's still disabled, since clicking a disabled button would wait forever.
    private async Task<ILocator> BatchButtonAsync(string part)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var button = Root.Locator($"[data-part=batch-bar] [data-part={part}]");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException("The grid has no batch bar; set its EditMode to Batch.");
        }

        try
        {
            await Assertions.Expect(button).Not.ToHaveAttributeAsync("aria-disabled", "true", new() { Timeout = 2000 });
        }
        catch (PlaywrightException exception)
        {
            throw new InvalidOperationException("The grid holds no changes.", exception);
        }

        return button;
    }

    /// <summary>Opens the tree grid row with a cell whose text is exactly this, with its button, and waits for its
    /// child rows to show. A row already open stays open.</summary>
    public Task ExpandAsync(string row) => SetExpandedAsync(row, true);

    /// <summary>Closes the tree grid row with a cell whose text is exactly this, with its button, and waits for its
    /// child rows to hide. A row already closed stays closed.</summary>
    public Task CollapseAsync(string row) => SetExpandedAsync(row, false);

    private async Task SetExpandedAsync(string row, bool expanded)
    {
        var found = Row(row);
        var wanted = expanded ? "true" : "false";
        if (await found.GetAttributeAsync("aria-expanded") != wanted)
        {
            await found.Locator("[data-part=tree-toggle]").ClickAsync();
        }

        await Assertions.Expect(found).ToHaveAttributeAsync("aria-expanded", wanted);
    }

    /// <summary>Moves the row with a cell whose text is exactly <paramref name="row"/> to <paramref name="index"/>
    /// among all the rows, on every page, through its handle's menu as a user would: Move to start or Move to end for
    /// the first or last place, else Move up or Move down a place at a time, so <c>CanMoveRow</c> and
    /// <c>OnRowMove</c> see each step. Waits for each step's answer, and a step the app refuses ends the move. The row
    /// must be on the page shown, and the grid shows the page it lands on. Throws when the rows can't move or
    /// <paramref name="index"/> is past the last row.</summary>
    public Task MoveRowAsync(string row, int index) => HandleMoves.MoveAsync(Root, Handle(row), () => PlaceAsync(row),
        async () => int.Parse(await Table.GetAttributeAsync("data-count") ?? "0", CultureInfo.InvariantCulture), index,
        row);

    /// <summary>Drags the row with a cell whose text is exactly <paramref name="row"/> by its handle onto the top half
    /// of the row with a cell whose text is exactly <paramref name="target"/>, to move it just before that row, and
    /// waits for the app's answer. Both rows must be on the page shown.</summary>
    public Task DragRowBeforeAsync(string row, string target) => DragRowAsync(row, target, before: true);

    /// <summary>Drags the row with a cell whose text is exactly <paramref name="row"/> by its handle onto the bottom
    /// half of the row with a cell whose text is exactly <paramref name="target"/>, to move it just after that row, and
    /// waits for the app's answer. Both rows must be on the page shown.</summary>
    public Task DragRowAfterAsync(string row, string target) => DragRowAsync(row, target, before: false);

    private async Task DragRowAsync(string row, string target, bool before) => await HandleMoves.DragAsync(Root, Root,
        Handle(row), Row(target), before, await PlaceAsync(row), await PlaceAsync(target), row);

    private ILocator Handle(string row) =>
        Row(row).Locator(":scope > td > [data-part=handle] > [aria-haspopup=menu]");

    private async Task<int> PlaceAsync(string row) => int.Parse(await Row(row).GetAttributeAsync("data-index")
        ?? throw new InvalidOperationException("The grid's rows can't move: it needs an OnRowMove and in-memory "
            + "Items, and mustn't be sorted, filtered or grouped."), CultureInfo.InvariantCulture);

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
            await scope.Page.WaitForFunctionAsync($"([header, before]) => ({state})(header) !== before",
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
        await new TbSelectLocator(scope, panel.Locator("[role=combobox]")).ChooseAsync(condition);
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
                await new TbSelectLocator(scope, cell.Locator("[role=combobox]")).ChooseAsync(choices);
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
        await new TbPaginationLocator(scope, (await pages.GetAttributeAsync("aria-label"))!).GoToPageAsync(number);
        await RowsLoadedAsync();
    }

    // The render that changes the header or pager marks the table busy until the rows load.
    private Task RowsLoadedAsync() => Assertions.Expect(Table).Not.ToHaveAttributeAsync("aria-busy", "true");

    /// <summary>The columns' titles, in the order they show.</summary>
    public async Task<IReadOnlyList<string>> ColumnsAsync() => await Table.Locator("thead th[data-title]")
        .EvaluateAllAsync<string[]>("headers => headers.map(header => header.dataset.title)");

    /// <summary>The text of the totals row's cell under the column with this title, such as <c>"Sum 42.1"</c>, or
    /// null when the column shows no total.</summary>
    public async Task<string?> TotalAsync(string column)
    {
        var key = await Header(column).GetAttributeAsync("data-column");
        var texts = await TotalTextsAsync(Table.Locator($"tfoot [data-part=totals] td[data-column=\"{key}\"]"));
        return TotalText(string.Join(' ', texts));
    }

    /// <summary>The text of each open group's totals cell under the column with this title, in the order the groups
    /// show, with null for a group that shows no total there. The grid shows these rows with <c>GroupTotals</c>.
    /// </summary>
    public async Task<IReadOnlyList<string?>> GroupTotalsAsync(string column)
    {
        var key = await Header(column).GetAttributeAsync("data-column");
        var texts = await TotalTextsAsync(Table.Locator($"tbody [data-part=group-totals] td[data-column=\"{key}\"]"));
        return [.. texts.Select(TotalText)];
    }

    // The row's name for screen readers isn't part of the total.
    private static Task<string[]> TotalTextsAsync(ILocator cells) => cells.EvaluateAllAsync<string[]>("""
        cells => cells.map(cell => {
            const name = cell.querySelector('[data-part=totals-name]');
            return name ? cell.innerText.replace(name.innerText, '') : cell.innerText;
        })
        """);

    private static string? TotalText(string text)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0 ? null : string.Join(' ', words);
    }

    /// <summary>Resizes the column with this title to this many pixels by dragging its header's edge, and waits for
    /// the grid to keep the width. The column stops at its <c>MinWidth</c>.</summary>
    public async Task ResizeColumnAsync(string column, double width)
    {
        await Assertions.Expect(Root).ToHaveAttributeAsync("data-ready", "");
        var header = Header(column);
        var edge = header.Locator("[data-part=resize]");
        if (await edge.CountAsync() == 0)
        {
            throw new InvalidOperationException($"The \"{column}\" column can't be resized; set the grid's "
                + "ResizableColumns and leave the column's Resizable on.");
        }

        await header.ScrollIntoViewIfNeededAsync();
        var box = (await edge.BoundingBoxAsync())!;
        var by = width - (await header.BoundingBoxAsync())!.Width;
        var rtl = await header.EvaluateAsync<bool>("header => getComputedStyle(header).direction === 'rtl'");
        var (x, y) = (box.X + box.Width / 2, box.Y + box.Height / 2);
        await scope.Page.Mouse.MoveAsync(x, y);
        await scope.Page.Mouse.DownAsync();
        await scope.Page.Mouse.MoveAsync(x + (float)(rtl ? -by : by), y, new() { Steps = 5 });
        await scope.Page.Mouse.UpAsync();
        // The grid renders the width it keeps, which matches the header once the server has it.
        await scope.Page.WaitForFunctionAsync("header => header.dataset.width"
            + " && Math.abs(header.getBoundingClientRect().width - Number(header.dataset.width)) < 1",
            await header.ElementHandleAsync());
    }

    private ILocator Header(string column) => Table.Locator($"thead th[data-title=\"{column}\"]");
}

/// <summary>A <c>TbPagination</c>, found by its accessible name.</summary>
public sealed class TbPaginationLocator(ILocator scope, string name)
{
    private ILocator Navigation => scope.GetByRole(AriaRole.Navigation, new() { Name = name, Exact = true });

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

// Option names as the helpers read them: what a screen reader hears.
internal static class Choices
{
    // An aria-label, else the text outside hidden parts.
    private const string NameOf = """
        element => {
            if (element.hasAttribute('aria-label')) return element.getAttribute('aria-label');
            const copy = element.cloneNode(true);
            copy.querySelectorAll('[aria-hidden=true]').forEach(hidden => hidden.remove());
            return copy.textContent.trim();
        }
        """;

    // A native select shows its chosen options other than the placeholder, including one that keeps a value no option
    // has.
    public const string ShownNames = "control => control.localName === 'select' "
        + "? [...control.selectedOptions].filter(option => option.value !== '').map(option => option.text.trim()) "
        + ": [...control.querySelectorAll('[data-part=value], [data-part=chip]')].map(" + NameOf + ")";

    public const string ChosenNames = "list => [...list.querySelectorAll("
        + "'[role=option][aria-selected=true]:not([data-part=select-all])')].map(" + NameOf + ")";
}

// What the select and listbox helpers share: a statically rendered field is the browser's own select.
internal static class NativeSelect
{
    // The chosen options, less a static select's placeholder and the option that keeps a value no option has.
    public const string Chosen = "option:checked:not([value='']):not([data-unmatched])";

    public static async Task ThrowIfStaticAsync(ILocator field)
    {
        if (await field.EvaluateAsync<bool>(
                "field => field.localName === 'select' && !field.hasAttribute('data-prerender')"))
        {
            throw new InvalidOperationException("On a static page this field renders as a native select, which posts "
                + "with its form; render it on an interactive page to unchoose or clear options.");
        }
    }

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
