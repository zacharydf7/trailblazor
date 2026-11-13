using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Trailblazor.Internal.Events;

namespace Trailblazor.Testing;

/// <summary>bUnit helpers that find Trailblazor components by their labels and names, as a user would, mostly with the
/// same names as the Playwright helpers in Trailblazor.Testing.Playwright.</summary>
public static class TrailblazorMarkupExtensions
{
    /// <summary>The Trailblazor components in this rendered markup.</summary>
    public static TrailblazorMarkup Tb<TComponent>(this IRenderedComponent<TComponent> rendered)
        where TComponent : IComponent => new(selector => rendered.FindAll(selector));
}

/// <summary>The Trailblazor components in a rendered component's markup. Each helper finds its component again on every
/// call, so it sees the markup as it is after the last render.</summary>
public sealed class TrailblazorMarkup
{
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TrailblazorMarkup(Func<string, IReadOnlyList<IElement>> findAll) => _findAll = findAll;

    /// <summary>The Trailblazor components inside the element <paramref name="scope"/> finds, such as a grid's row,
    /// so a field that repeats in every row can be told apart:
    /// <c>cut.Tb().Within(() =&gt; cut.Tb().Grid("Orders").Row("Tent")).Select("Size")</c>. The element is found again
    /// each time a helper looks for a component, since one found before a render is stale after it.</summary>
    public TrailblazorMarkup Within(Func<IElement> scope) =>
        new(selector => scope().QuerySelectorAll(selector).ToList());

    /// <summary>The fields of the <c>TbPropertyGrid</c> with this label, found by their labels as anywhere else:
    /// <c>cut.Tb().PropertyGrid("Trail properties").NumberField("Length (km)")</c>.</summary>
    public TrailblazorMarkup PropertyGrid(string label) => Within(() =>
    {
        var grids = _findAll(".tb-property-grid[role=group]").Where(grid => grid.GetAttribute("aria-label") == label)
            .ToList();
        return grids.Count == 1 ? grids[0]
            : throw new InvalidOperationException($"Expected one property grid labeled \"{label}\", found {grids.Count}.");
    });

    /// <summary>The <c>TbViewport</c> whose box has this <c>Label</c>, or the only one in the markup when no label is
    /// given.</summary>
    public TbViewportDriver Viewport(string? label = null) => new(() =>
    {
        var viewports = _findAll("[data-part=viewport]").Where(viewport => label is null
            || viewport.QuerySelector(":scope > [data-part=surface]")?.GetAttribute("aria-label") == label).ToList();
        return viewports.Count == 1 ? viewports[0]
            : throw new InvalidOperationException(label is null
                ? $"Expected one viewport, found {viewports.Count}; name the one to use."
                : $"Expected one viewport labeled \"{label}\", found {viewports.Count}.");
    }, _findAll);

    /// <summary>The <c>TbQueryBuilder</c> with this <c>Label</c>, or the only one in the markup when no label is
    /// given.</summary>
    public TbQueryBuilderDriver QueryBuilder(string? label = null) => new(() =>
    {
        var builders = _findAll("[data-part=query-builder]")
            .Where(builder => label is null || builder.GetAttribute("aria-label") == label).ToList();
        return builders.Count == 1 ? builders[0]
            : throw new InvalidOperationException(label is null
                ? $"Expected one query builder, found {builders.Count}; name the one to use."
                : $"Expected one query builder labeled \"{label}\", found {builders.Count}.");
    }, _findAll);

    /// <summary>The <c>TbTextField</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbTextFieldDriver TextField(string label) =>
        new(() => Field(label, ".tb-text-field input[data-part=input]", ".tb-text-field textarea[data-part=input]"));

    /// <summary>The <c>TbSelect</c> or <c>TbMultiSelect</c> with this label, or with this <c>AriaLabel</c> when it
    /// has no label.</summary>
    public TbSelectDriver Select(string label) => new(() => Field(label, "[role=combobox]", "select"), _findAll);

    /// <summary>The <c>TbListBox</c> or <c>TbMultiListBox</c> with this label, or with this <c>AriaLabel</c> when it
    /// has no label.</summary>
    public TbListBoxDriver ListBox(string label) => new(() => Field(label, "[role=listbox]", "select"));

    /// <summary>The <c>TbTransferList</c> with this label.</summary>
    public TbTransferListDriver TransferList(string label) => new(() =>
    {
        var lists = _findAll("fieldset.tb-transfer-list").Where(list =>
            list.QuerySelector("legend[data-part=label]") is { } legend && LabelText(legend) == label).ToList();
        return lists.Count == 1 ? lists[0] : throw new InvalidOperationException(lists.Count == 0
            ? $"No transfer list is labeled \"{label}\"." : $"{lists.Count} transfer lists are labeled \"{label}\".");
    });

    /// <summary>The <c>TbNumberField</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbNumberFieldDriver NumberField(string label) =>
        new(() => Field(label, "input[type=number]", "input[role=spinbutton]"));

    /// <summary>The <c>TbTagInput</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbTagInputDriver TagInput(string label) =>
        new(() => Field(label, "input[data-part=input].tb-tag-input__input"));

    /// <summary>The <c>TbCalendar</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbCalendarDriver Calendar(string label) => new(() => Field(label, ".tb-calendar"));

    /// <summary>The <c>TbSignaturePad</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbSignaturePadDriver SignaturePad(string label) => new(() =>
        _findAll(".tb-signature-pad").Where(pad => pad.QuerySelector(":scope > [data-part=label]") is { } named
            && LabelText(named) == label).ToList() is [var pad]
            ? pad : throw new InvalidOperationException($"No signature pad is labeled \"{label}\"."));

    /// <summary>The <c>TbRichTextEditor</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbRichTextEditorDriver RichTextEditor(string label) =>
        new(() => Field(label, "[role=textbox][contenteditable]"));

    /// <summary>The <c>TbDatePicker</c> with this label, or with this <c>AriaLabel</c> when it has no label.</summary>
    public TbDatePickerDriver DatePicker(string label) =>
        new(() => Field(label, "input[data-format]", "input[type=date][data-static]:not(.tb-calendar__input)"));

    /// <summary>The <c>TbDateTimePicker</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbDateTimePickerDriver DateTimePicker(string label) =>
        new(() => Field(label, "[role=group][data-format]:has([data-part=time-input])",
            "input[type=datetime-local][data-static]"));

    /// <summary>The <c>TbDateRangePicker</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbDateRangePickerDriver DateRangePicker(string label) =>
        new(() => Field(label, "[role=group][data-format]:has([data-part=end-input])"));

    /// <summary>The <c>TbPagination</c> with this accessible name, or the only one in the markup when no name is
    /// given.</summary>
    public TbPaginationDriver Pagination(string? name = null) => new(() =>
    {
        var navs = _findAll("nav[data-page-count]")
            .Where(nav => name is null || nav.GetAttribute("aria-label") == name).ToList();
        return navs.Count == 1 ? navs[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one pagination, found {navs.Count}; name the one to use."
                : $"No pagination is named \"{name}\".");
    });

    /// <summary>The <c>TbAccordionItem</c>, in an accordion or on its own, whose header reads this.</summary>
    public TbAccordionItemDriver AccordionItem(string header) => new(header, _findAll);

    /// <summary>The <c>TbPopconfirm</c> whose trigger has this name.</summary>
    public TbPopconfirmDriver Popconfirm(string trigger) => new(trigger, _findAll);

    /// <summary>The <c>TbMenu</c> whose trigger has this name.</summary>
    public TbMenuDriver Menu(string trigger) => new(trigger, _findAll);

    /// <summary>The <c>TbHotkey</c> with these keys, written as its <c>Keys</c> are, such as <c>"Mod+S"</c>.</summary>
    public TbHotkeyDriver Hotkey(string keys) => new(() =>
    {
        var hotkeys = _findAll("[data-part=hotkey]").Where(hotkey => hotkey.GetAttribute("data-keys") == keys).ToList();
        return hotkeys.Count == 1 ? hotkeys[0]
            : throw new InvalidOperationException($"Expected one hotkey with the keys \"{keys}\", found "
                + $"{hotkeys.Count}; write the keys as its Keys are, or use Within when several match.");
    });

    /// <summary>The <c>TbContextMenu</c> whose menu has this <c>AriaLabel</c>, or the only one in the markup when no
    /// name is given.</summary>
    public TbContextMenuDriver ContextMenu(string? name = null) => new(() =>
    {
        var targets = _findAll(".tb-context-menu[data-part=target]")
            .Where(target => name is null || TbContextMenuDriver.Menu(target).GetAttribute("aria-label") == name)
            .ToList();
        return targets.Count == 1 ? targets[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one context menu, found {targets.Count}; name the one to use."
                : $"Expected one context menu named \"{name}\", found {targets.Count}.");
    });

    /// <summary>The <c>TbTabs</c> with this accessible name, or the only tabs in the markup when no name is given.
    /// </summary>
    public TbTabsDriver Tabs(string? name = null) => new(() =>
    {
        var lists = _findAll("[role=tablist]")
            .Where(list => name is null || list.GetAttribute("aria-label") == name).ToList();
        return lists.Count == 1 ? lists[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one tab list, found {lists.Count}; name the one to use."
                : $"No tab list is named \"{name}\".");
    });

    /// <summary>The <c>TbTreeSelect</c> or <c>TbMultiTreeSelect</c> with this label, or with this <c>AriaLabel</c>
    /// when it has no label.</summary>
    public TbTreeSelectDriver TreeSelect(string label) =>
        new(() => Field(label, ".tb-tree-select__trigger"), _findAll);

    /// <summary>The <c>TbTreeView</c> with this accessible name, or the only tree in the markup when no name is given.
    /// </summary>
    public TbTreeDriver Tree(string? name = null) => new(() =>
    {
        var trees = _findAll(".tb-tree").Where(tree => name is null || tree.GetAttribute("aria-label") == name)
            .ToList();
        return trees.Count == 1 ? trees[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one tree, found {trees.Count}; name the one to use."
                : $"No tree is named \"{name}\".");
    });

    /// <summary>The <c>TbDataGrid</c> whose table has this accessible name, or the only grid in the markup when no
    /// name is given.</summary>
    public TbGridDriver Grid(string? name = null) => new(_findAll, () =>
    {
        var tables = _findAll("[data-part=grid] table")
            .Where(table => name is null || table.GetAttribute("aria-label") == name).ToList();
        return tables.Count == 1 ? tables[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one data grid, found {tables.Count}; name the one to use."
                : $"No data grid is named \"{name}\".");
    });

    /// <summary>The <c>TbCommandPalette</c> with this label, or the only one in the markup when no label is given.
    /// Open it as the app does, such as with its button, since the shortcut needs a browser.</summary>
    public TbCommandPaletteDriver CommandPalette(string? name = null) => new(() =>
    {
        var markers = _findAll("[data-command-palette]")
            .Where(marker => name is null || marker.GetAttribute("data-command-palette") == name).ToList();
        return markers.Count == 1 ? markers[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one command palette, found {markers.Count}; name the one to use."
                : $"No command palette is named \"{name}\".");
    }, _findAll);

    /// <summary>The <c>TbSortable</c> with this label, or the only one in the markup when no label is given.
    /// </summary>
    public TbSortableDriver Sortable(string? name = null) => new(() =>
    {
        var lists = _findAll("[data-part=list][data-sortable]")
            .Where(list => name is null || list.GetAttribute("aria-label") == name).ToList();
        return lists.Count == 1 ? lists[0]
            : throw new InvalidOperationException(name is null
                ? $"Expected one sortable list, found {lists.Count}; name the one to use."
                : $"No sortable list is named \"{name}\".");
    });

    // A field's label names its control by for or aria-labelledby; a required field's label ends with an asterisk. A
    // field with no label, such as one editing a grid's cell, is named by its aria-label, as Playwright finds it too.
    private IElement Field(string label, params string[] controls)
    {
        var labels = _findAll("label[data-part=label]").Where(element => LabelText(element) == label).ToList();
        if (labels.Count == 0 && _findAll(string.Join(", ", controls)).Where(control =>
                control.GetAttribute("aria-label") == label).ToList() is [var unlabeled])
        {
            return unlabeled;
        }

        if (labels.Count != 1)
        {
            throw new InvalidOperationException(labels.Count == 0
                ? $"No field is labeled \"{label}\"."
                : $"{labels.Count} fields are labeled \"{label}\".");
        }

        var named = labels[0];
        var control = named.GetAttribute("for") is { Length: > 0 } target
            ? _findAll($"[id='{target}']").FirstOrDefault()
            : _findAll($"[aria-labelledby~='{named.Id}']").FirstOrDefault();
        return control is not null && controls.Any(control.Matches) ? control
            : throw new InvalidOperationException($"The field labeled \"{label}\" isn't the kind asked for.");
    }

    private static string LabelText(IElement label) => label.TextContent.Trim().TrimEnd('*').Trim();
}

/// <summary>A <c>TbSelect</c> or <c>TbMultiSelect</c>, found by its label.</summary>
public sealed class TbSelectDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbSelectDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>The options' names, in order, leaving out a static select's placeholder, the extra option it adds to
    /// keep a value that matches no option, and the message an empty list shows. An option with content of its own is
    /// named by its <c>Text</c>.</summary>
    public IReadOnlyList<string> Options => OptionElements().Select(Choices.NameOf).ToList();

    /// <summary>The name of the select's value, as a screen reader hears it, or null while it shows its placeholder. A
    /// multiple select's chosen options are joined with ", ".</summary>
    public string? Text
    {
        get
        {
            var control = _find();
            var shown = control.TagName == "SELECT" ? control.QuerySelectorAll("option[selected]:not([value=''])")
                : control.QuerySelectorAll("[data-part=value], [data-part=chip]");
            var texts = shown.Select(Choices.NameOf).ToList();
            return texts.Count > 0 ? string.Join(", ", texts) : null;
        }
    }

    /// <summary>Chooses the options with these names, as clicking them does: the one option of a select, or each one
    /// not yet chosen in a multiple select, keeping what is already chosen. On a static page the select is a native
    /// one that posts with its form, so this throws, as it does for a disabled select.</summary>
    public Task ChooseAsync(params string[] options) => Choices.SetChosenAsync(Choosable, options, choose: true);

    /// <summary>The names of the chosen options, in order. A list of more than 100 options shows only those in view,
    /// so for one that long this throws; read <see cref="Text"/> instead.</summary>
    public IReadOnlyList<string> Chosen
    {
        get
        {
            var control = _find();
            if (_findAll($"[id='{control.GetAttribute("aria-controls")}'][data-count], "
                    + $"[id='{control.GetAttribute("aria-controls")}'] [data-count]").Count > 0)
            {
                throw new InvalidOperationException(
                    "The list shows only the options in view, so their names can't all be read; read Text instead.");
            }

            // A multiple select's Select all row is an option, but it chooses the others rather than itself.
            return OptionElements().Where(option => !option.Matches("[data-part=select-all]")
                    && (option.GetAttribute("aria-selected") == "true" || option.HasAttribute("selected")))
                .Select(Choices.NameOf).ToList();
        }
    }

    /// <summary>Unchooses the options with these names in a multiple select, as clicking them again does, leaving the
    /// others chosen. A select that takes one option throws; clear it with <see cref="ClearAsync"/>. On a static page,
    /// or when the select is disabled, this throws too, as <see cref="ChooseAsync"/> does.</summary>
    public Task UnchooseAsync(params string[] options) => Choices.SetChosenAsync(Choosable, options, choose: false);

    /// <summary>Clears the select with its clear button, as a user does. A select that isn't <c>Clearable</c>, or has
    /// nothing chosen, shows no clear button, so this throws, as it does on a static page or for a disabled select.
    /// </summary>
    public Task ClearAsync()
    {
        var control = _find();
        Choices.ThrowIfStaticOrDisabled(control);
        var clear = control.Closest(".tb-field")?.QuerySelector("[data-part=clear]")
            ?? throw new InvalidOperationException(
                "The select shows no clear button; make it Clearable, and choose an option first.");
        return clear.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private (IEnumerable<IElement> Options, bool Multiple) Choosable()
    {
        var control = _find();
        Choices.ThrowIfStaticOrDisabled(control);
        // A filterable select points to the dialog that holds its filter and listbox.
        var controls = control.GetAttribute("aria-controls");
        return (OptionElements(), _findAll($"[id='{controls}'][aria-multiselectable=true], "
            + $"[id='{controls}'] [aria-multiselectable=true]").Count > 0);
    }

    private IEnumerable<IElement> OptionElements()
    {
        var control = _find();
        return control.TagName == "SELECT"
            ? control.QuerySelectorAll("option:not([value='']):not([data-unmatched]):not([data-part=status])")
            : _findAll($"[id='{control.GetAttribute("aria-controls")}'] [role=option]:not([data-part=status])");
    }
}

/// <summary>A <c>TbTreeSelect</c> or <c>TbMultiTreeSelect</c>, found by its label. Items are named by their path of
/// texts from a root, such as <c>("North ridge", "Pine Hollow")</c>.</summary>
public sealed class TbTreeSelectDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbTreeSelectDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>The text the field shows for its value, or null while it shows its placeholder.</summary>
    public string? Text => _find().QuerySelector("[data-part=value]") is { } value
        && !value.ClassList.Contains("tb-select__placeholder") ? value.TextContent.Trim() : null;

    /// <summary>Opens the rows above the item at this path and chooses it, as clicking it does.</summary>
    public Task ChooseAsync(params string[] path) => Tree().SelectAsync(path);

    /// <summary>Opens the rows above the item at this path and, unless it is checked already, checks it in a multi
    /// tree select as clicking its box does.</summary>
    public Task CheckAsync(params string[] path) => SetCheckedAsync(path, true);

    /// <summary>Opens the rows above the item at this path and, unless it is unchecked already, unchecks it in a
    /// multi tree select as clicking its box does.</summary>
    public Task UncheckAsync(params string[] path) => SetCheckedAsync(path, false);

    /// <summary>Types this text in the field's filter box, as the user does, so the tree shows what matches.</summary>
    public Task FilterAsync(string text) => (Panel().QuerySelector("[data-part=filter]")
        ?? throw new InvalidOperationException("This tree select has no filter box; set Filterable to add one."))
        .InputAsync(new ChangeEventArgs { Value = text });

    private async Task SetCheckedAsync(string[] path, bool check)
    {
        var tree = Tree();
        await tree.ExpandAsync(path[..^1]);
        // A partly checked row checks everything under it first, so unchecking it takes a second click.
        for (var clicks = 0; tree.Item(path).GetAttribute("aria-checked") != (check ? "true" : "false"); clicks++)
        {
            if (clicks == 2)
            {
                throw new InvalidOperationException($"The row at {string.Join(" > ", path)} can't be checked or "
                    + "unchecked.");
            }

            await tree.CheckAsync(path);
        }
    }

    // A disabled field's tree can't be used, and a read-only field has none.
    private IElement Panel()
    {
        var trigger = _find();
        return trigger.HasAttribute("disabled") || trigger.GetAttribute("aria-disabled") == "true" ? throw Closed()
            : _findAll($"[id='{trigger.GetAttribute("aria-controls")}']").SingleOrDefault() ?? throw Closed();

        static InvalidOperationException Closed() =>
            new("This tree select is disabled or read-only, so its tree can't be used.");
    }

    private TbTreeDriver Tree() => new(() => Panel().QuerySelector(".tb-tree")
        ?? throw new InvalidOperationException("This tree select's tree isn't there."));
}

/// <summary>A <c>TbListBox</c> or <c>TbMultiListBox</c>, found by its label.</summary>
public sealed class TbListBoxDriver
{
    private readonly Func<IElement> _find;

    internal TbListBoxDriver(Func<IElement> find) => _find = find;

    /// <summary>The options' names, in order, leaving out the message an empty list shows. An option with content of
    /// its own is named by its <c>Text</c>.</summary>
    public IReadOnlyList<string> Options => OptionElements(_find()).Select(Choices.NameOf).ToList();

    /// <summary>The names of the chosen options, in order.</summary>
    public IReadOnlyList<string> Chosen => _find()
        .QuerySelectorAll("[role=option][aria-selected=true], option[selected]")
        .Select(Choices.NameOf).ToList();

    /// <summary>Chooses the options with these names, as clicking them does: the one option of a list box, or each one
    /// not yet chosen in a multiple list box, keeping what is already chosen. On a static page, or when the list box is
    /// disabled, this throws, as for a select.</summary>
    public Task ChooseAsync(params string[] options) => Choices.SetChosenAsync(Choosable, options, choose: true);

    /// <summary>Unchooses the options with these names in a multiple list box, as clicking them again does, leaving
    /// the others chosen. A list box that takes one option throws, since a click can't unchoose its option, and so
    /// does a static or disabled one, as for <see cref="ChooseAsync"/>.</summary>
    public Task UnchooseAsync(params string[] options) => Choices.SetChosenAsync(Choosable, options, choose: false);

    /// <summary>Unchooses every chosen option in a multiple list box.</summary>
    public Task ClearAsync() => Chosen.Count == 0 ? Task.CompletedTask : UnchooseAsync([.. Chosen]);

    private (IEnumerable<IElement> Options, bool Multiple) Choosable()
    {
        var list = _find();
        Choices.ThrowIfStaticOrDisabled(list);
        return (OptionElements(list), list.GetAttribute("aria-multiselectable") == "true");
    }

    private static IEnumerable<IElement> OptionElements(IElement list) =>
        list.QuerySelectorAll("[role=option]:not([data-part=status]), option:not([data-part=status])");
}

/// <summary>A <c>TbTransferList</c>, found by its label.</summary>
public sealed class TbTransferListDriver
{
    private readonly Func<IElement> _find;

    internal TbTransferListDriver(Func<IElement> find) => _find = find;

    /// <summary>The chosen items' text, in order.</summary>
    public IReadOnlyList<string> Chosen => new TbListBoxDriver(() => List("chosen")).Options;

    /// <summary>Marks the available items with this text and moves them to the chosen list with its button.</summary>
    public Task MoveAsync(params string[] items) => MoveAsync("available", "move-chosen", items);

    /// <summary>Marks the chosen items with this text and moves them back to the available list with its button.
    /// </summary>
    public Task MoveBackAsync(params string[] items) => MoveAsync("chosen", "move-available", items);

    private async Task MoveAsync(string from, string button, string[] items)
    {
        await new TbListBoxDriver(() => List(from)).ChooseAsync(items);
        await _find().QuerySelector($"[data-part={button}]")!.ClickAsync(new MouseEventArgs());
    }

    private IElement List(string part) => _find().QuerySelector($"[data-part={part}] [role=listbox]")
        ?? throw new InvalidOperationException("The transfer list isn't interactive yet.");
}

// What choosing in a select and a list box share, with the Playwright helpers' rules.
internal static class Choices
{
    // Clicks each named option whose state differs: one not yet chosen to choose it, or a chosen one to unchoose it.
    internal static async Task SetChosenAsync(Func<(IEnumerable<IElement> Options, bool Multiple)> find,
        string[] texts, bool choose)
    {
        ArgumentOutOfRangeException.ThrowIfZero(texts.Length, nameof(texts));
        if (choose && texts.Length > 1 && !find().Multiple)
        {
            throw new ArgumentException("A field that takes one option can't choose several.", nameof(texts));
        }

        if (!choose && !find().Multiple)
        {
            throw new InvalidOperationException("A field that takes one option can't unchoose it by clicking.");
        }

        foreach (var text in texts)
        {
            var option = find().Options.SingleOrDefault(option => NameOf(option) == text)
                ?? throw new InvalidOperationException($"No option reads \"{text}\".");
            if (option.GetAttribute("aria-selected") == "true" != choose)
            {
                // A click from a pointer has a count, which the option reports as pointer input.
                await option.ClickAsync(new MouseEventArgs { Detail = 1 });
            }
        }
    }

    // What a screen reader reads, as the Playwright helpers find it: an aria-label, else the text outside hidden parts.
    internal static string NameOf(IElement element) => element.GetAttribute("aria-label")
        ?? string.Concat(element.Descendants<IText>()
            .Where(text => text.ParentElement?.Closest("[aria-hidden=true]") is not { } hidden
                || !element.Contains(hidden))
            .Select(text => text.Data)).Trim();

    internal static void ThrowIfStaticOrDisabled(IElement control)
    {
        if (control.TagName == "SELECT")
        {
            throw new InvalidOperationException("This field renders as a native select on a static page, which posts "
                + "with its form; render it on an interactive page to choose options.");
        }

        if (control.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException("This field is disabled, so its options can't be chosen.");
        }
    }
}

/// <summary>A <c>TbDatePicker</c>, found by its label.</summary>
public sealed class TbDatePickerDriver
{
    private readonly Func<IElement> _find;

    internal TbDatePickerDriver(Func<IElement> find) => _find = find;

    /// <summary>The date the field holds, or null when it has none.</summary>
    public DateOnly? Value
    {
        get
        {
            var input = _find();
            return Dates.ReadIso(input.GetAttribute(input.HasAttribute("data-static") ? "value" : "data-value"));
        }
    }

    /// <summary>Types the date in the field's own format and commits it, as typing and leaving the field does; null
    /// clears it. A date the field can't take shows its error, as in a browser. On a static page, or when the field is
    /// read-only or disabled, this throws.</summary>
    public Task ChooseAsync(DateOnly? date)
    {
        var input = _find();
        Dates.ThrowIfStaticOrLocked(input);
        return input.ChangeAsync(new ChangeEventArgs
        {
            Value = date?.ToString(input.GetAttribute("data-format"), Dates.CultureOf(input)) ?? "",
        });
    }
}

/// <summary>A <c>TbDateTimePicker</c>, found by its label.</summary>
public sealed class TbDateTimePickerDriver
{
    private readonly Func<IElement> _find;

    internal TbDateTimePickerDriver(Func<IElement> find) => _find = find;

    /// <summary>The date and time the field holds, or null when it has none.</summary>
    public DateTime? Value
    {
        get
        {
            var picker = _find();
            var text = picker.HasAttribute("data-static") ? picker.GetAttribute("value")
                : picker.GetAttribute("data-value");
            return string.IsNullOrEmpty(text) ? null : DateTime.ParseExact(text,
                ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"], CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Types the date in the field's own format and then the time, committing each as leaving its box does;
    /// null clears the date. On a static page, or when the field is read-only or disabled, this throws.</summary>
    public async Task ChooseAsync(DateTime? value)
    {
        var group = _find();
        var dateInput = group.QuerySelector("[data-part=date-input]")!;
        Dates.ThrowIfStaticOrLocked(group.HasAttribute("data-static") ? group : dateInput);
        await dateInput.ChangeAsync(new ChangeEventArgs
        {
            Value = value?.ToString(group.GetAttribute("data-format"), Dates.CultureOf(group)) ?? "",
        });
        if (value is { } chosen)
        {
            await _find().QuerySelector("[data-part=time-input]")!.ChangeAsync(new ChangeEventArgs
            {
                Value = chosen.ToString(chosen.Second == 0 ? "HH:mm" : "HH:mm:ss", CultureInfo.InvariantCulture),
            });
        }
    }
}

/// <summary>A <c>TbDateRangePicker</c>, found by its label.</summary>
public sealed class TbDateRangePickerDriver
{
    private readonly Func<IElement> _find;

    internal TbDateRangePickerDriver(Func<IElement> find) => _find = find;

    /// <summary>The range the field holds; either end is null when it has none.</summary>
    public (DateOnly? Start, DateOnly? End) Range
    {
        get
        {
            var dates = (_find().GetAttribute("data-value") ?? throw new InvalidOperationException(
                "The date range picker doesn't report its range.")).Split('/');
            return (Dates.ReadIso(dates[0]), Dates.ReadIso(dates[1]));
        }
    }

    /// <summary>Types both dates in the field's own format and commits them, as typing in each box and leaving it does;
    /// null clears that end. On a static page, or when the field is read-only or disabled, this throws.</summary>
    public async Task ChooseAsync(DateOnly? start, DateOnly? end)
    {
        var group = _find();
        Dates.ThrowIfStaticOrLocked(group.QuerySelector("[data-part=start-input]")!);
        var format = group.GetAttribute("data-format");
        var culture = Dates.CultureOf(group);
        await group.QuerySelector("[data-part=start-input]")!.ChangeAsync(
            new ChangeEventArgs { Value = start?.ToString(format, culture) ?? "" });
        await _find().QuerySelector("[data-part=end-input]")!.ChangeAsync(
            new ChangeEventArgs { Value = end?.ToString(format, culture) ?? "" });
    }
}

internal static class Dates
{
    internal static DateOnly? ReadIso(string? text) => string.IsNullOrEmpty(text) ? null
        : DateOnly.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    // The date fields read and show dates in their culture's Gregorian calendar, so typed text has to use it too.
    internal static CultureInfo CultureOf(IElement field) =>
        CalendarDates.Gregorian(CultureInfo.GetCultureInfo(field.GetAttribute("data-culture") ?? ""));

    internal static void ThrowIfStaticOrLocked(IElement input)
    {
        if (input.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("This field renders as the browser's own date box on a static page, "
                + "which posts with its form; render it on an interactive page to choose a date.");
        }

        if (input.HasAttribute("readonly") || input.HasAttribute("disabled"))
        {
            throw new InvalidOperationException(
                "This field is read-only or disabled, so a date can't be typed into it.");
        }
    }
}

/// <summary>A <c>TbPagination</c>, found by its name.</summary>
public sealed class TbPaginationDriver
{
    private readonly Func<IElement> _find;

    internal TbPaginationDriver(Func<IElement> find) => _find = find;

    /// <summary>The page shown.</summary>
    public int Page => int.Parse(_find().GetAttribute("data-page")!, CultureInfo.InvariantCulture);

    private int PageCount => int.Parse(_find().GetAttribute("data-page-count")!, CultureInfo.InvariantCulture);

    /// <summary>Goes to this page as a user would: types it into a compact pagination's page box, or else clicks the
    /// page's button, the visible page nearest it, or Next or Previous, until it's current. A disabled pagination
    /// throws.</summary>
    public async Task GoToPageAsync(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(number, PageCount);
        if (_find().QuerySelector("[data-part=page-box] input") is { } box)
        {
            if (box.HasAttribute("disabled"))
            {
                throw new InvalidOperationException("The pagination is disabled.");
            }

            await new TbNumberFieldDriver(() => _find().QuerySelector("[data-part=page-box] input")!)
                .EnterAsync(number.ToString(CultureInfo.InvariantCulture));
            if (Page != number)
            {
                throw new InvalidOperationException($"The pagination stayed on page {Page}.");
            }

            return;
        }

        while (Page is var current && current != number)
        {
            var shown = _find().QuerySelectorAll("[data-part=page]")
                .ToDictionary(page => int.Parse(page.TextContent.Trim(), CultureInfo.InvariantCulture));
            var between = shown.Keys.Where(page => number > current ? page > current && page < number
                : page < current && page > number).ToList();
            var next = shown.ContainsKey(number) ? shown[number]
                : between.Count > 0 ? shown[number > current ? between.Max() : between.Min()]
                : _find().QuerySelector(number > current ? "[data-part=next]" : "[data-part=previous]")
                ?? throw new InvalidOperationException(
                    $"The pagination can't reach page {number} from page {current}.");
            await next.ClickAsync(new MouseEventArgs { Detail = 1 });
            if (Page == current)
            {
                throw new InvalidOperationException($"The pagination stayed on page {current}; is it disabled?");
            }
        }
    }
}

/// <summary>A <c>TbAccordionItem</c>, found by its header's text.</summary>
public sealed class TbAccordionItemDriver
{
    private readonly string _header;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbAccordionItemDriver(string header, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_header, _findAll) = (header, findAll);

    /// <summary>The header's button, whose <c>aria-expanded</c> says whether the item is open.</summary>
    public IElement Trigger
    {
        get
        {
            var triggers = _findAll("button.tb-accordion__trigger")
                .Where(trigger => Choices.NameOf(trigger) == _header).ToList();
            return triggers.Count == 1 ? triggers[0] : throw new InvalidOperationException(triggers.Count == 0
                ? $"No accordion item's header reads \"{_header}\"."
                : $"{triggers.Count} accordion items' headers read \"{_header}\".");
        }
    }

    /// <summary>Opens the item, if it's closed, and returns its panel. Throws for a disabled item that is closed.
    /// </summary>
    public async Task<IElement> ExpandAsync()
    {
        await SetAsync(true);
        return _findAll($"[id='{Trigger.GetAttribute("aria-controls")}']").Single();
    }

    /// <summary>Closes the item, if it's open. Throws for a disabled item that is open.</summary>
    public Task CollapseAsync() => SetAsync(false);

    private Task SetAsync(bool expanded)
    {
        var trigger = Trigger;
        if (trigger.GetAttribute("aria-expanded") == (expanded ? "true" : "false"))
        {
            return Task.CompletedTask;
        }

        return trigger.HasAttribute("disabled")
            ? throw new InvalidOperationException($"The accordion item \"{_header}\" is disabled.")
            : trigger.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbPopconfirm</c>, found by its trigger's name.</summary>
public sealed class TbPopconfirmDriver
{
    private readonly string _trigger;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbPopconfirmDriver(string trigger, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_trigger, _findAll) = (trigger, findAll);

    /// <summary>Presses the confirm button, which raises <c>OnConfirm</c>. In bUnit the button works without
    /// opening the panel first. A disabled popconfirm throws, since a user can't open it.</summary>
    public Task ConfirmAsync() => PressAsync("confirm");

    /// <summary>Presses the cancel button, which raises <c>OnCancel</c>. A disabled popconfirm throws.</summary>
    public Task CancelAsync() => PressAsync("cancel");

    private Task PressAsync(string part)
    {
        var triggers = _findAll("[data-part=trigger][aria-haspopup=dialog]")
            .Where(trigger => Choices.NameOf(trigger) == _trigger).ToList();
        var trigger = triggers.Count == 1 ? triggers[0] : throw new InvalidOperationException(triggers.Count == 0
            ? $"No popconfirm's trigger is named \"{_trigger}\"."
            : $"{triggers.Count} popconfirms' triggers are named \"{_trigger}\".");
        if (trigger.HasAttribute("disabled") || trigger.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The popconfirm \"{_trigger}\" is disabled.");
        }

        var button = _findAll($"[id='{trigger.GetAttribute("aria-controls")}'] [data-part={part}]").SingleOrDefault()
            ?? throw new InvalidOperationException($"The popover that \"{_trigger}\" opens isn't a popconfirm.");
        return button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }
}

/// <summary>A <c>TbTextField</c>, found by its label.</summary>
public sealed class TbTextFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbTextFieldDriver(Func<IElement> find) => _find = find;

    /// <summary>The text the field shows.</summary>
    public string? Text => _find().GetAttribute("value");

    /// <summary>Types this text into the field and commits it, as typing and then leaving the field does. It sends
    /// whichever of the input and change events the field listens for, so a mask or check runs as in a browser. A
    /// read-only or disabled field throws, since a user can't type into it.</summary>
    public async Task EnterAsync(string text)
    {
        if (_find().HasAttribute("readonly") || _find().HasAttribute("disabled"))
        {
            throw new InvalidOperationException("This field is read-only or disabled, so text can't be typed into it.");
        }

        if (_find().HasAttribute("blazor:oninput"))
        {
            await _find().InputAsync(new ChangeEventArgs { Value = text });
        }

        if (_find().HasAttribute("blazor:onchange"))
        {
            await _find().ChangeAsync(new ChangeEventArgs { Value = text });
        }
    }
}

/// <summary>A <c>TbNumberField</c>, found by its label.</summary>
public sealed class TbNumberFieldDriver
{
    private readonly Func<IElement> _find;

    internal TbNumberFieldDriver(Func<IElement> find) => _find = find;

    /// <summary>The text the field shows.</summary>
    public string? Text => _find().GetAttribute("value");

    /// <summary>Types this text into the field and commits it, as typing and then leaving the field does. Text that
    /// isn't a number shows the field's error, as in a browser.</summary>
    public Task EnterAsync(string text)
    {
        // A plain number box reads what was typed through its own event; a formatted one is a text box.
        var input = _find();
        return input.GetAttribute("role") == "spinbutton" ? input.ChangeAsync(new ChangeEventArgs { Value = text })
            : input.TriggerEventAsync("ontbnumberchange", new NumberChangeEventArgs { Value = text });
    }
}

/// <summary>A <c>TbTagInput</c>, found by its label.</summary>
public sealed class TbTagInputDriver
{
    private readonly Func<IElement> _find;

    internal TbTagInputDriver(Func<IElement> find) => _find = find;

    /// <summary>The tags the field shows, in order.</summary>
    public IReadOnlyList<string> Tags => [.. TagElements().Select(TextOf)];

    /// <summary>Adds this text as a tag, through the same event the field's script raises on Enter, so a duplicate,
    /// a tag past the limit or one the field's check refuses shows its message, as in a browser. A disabled or
    /// read-only field throws, since the user can't type in it.</summary>
    public Task AddAsync(string text)
    {
        var input = _find();
        if (input.HasAttribute("disabled") || input.HasAttribute("readonly"))
        {
            throw new InvalidOperationException("This tag input is disabled or read-only, so tags can't be added.");
        }

        return input.TriggerEventAsync("ontbtagadd", new TagAddEventArgs { Text = text });
    }

    /// <summary>Removes this tag with its remove button, as a click does. A tag the field doesn't show, or one it
    /// shows without a remove button because the field is disabled or read-only, throws.</summary>
    public Task RemoveAsync(string tag)
    {
        var button = TagElements().FirstOrDefault(element => TextOf(element) == tag)
                ?.QuerySelector("[data-part=remove]")
            ?? throw new InvalidOperationException($"The tag input has no tag \"{tag}\" that can be removed.");
        return button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private static string TextOf(IElement tag) => tag.QuerySelector(".tb-chip__label")!.TextContent.Trim();

    private IEnumerable<IElement> TagElements() =>
        _find().Closest("[data-part=control]")?.QuerySelectorAll("[data-part=tag]") ?? Enumerable.Empty<IElement>();
}

/// <summary>A <c>TbSignaturePad</c>, found by its label.</summary>
public sealed class TbSignaturePadDriver
{
    private readonly Func<IElement> _find;

    internal TbSignaturePadDriver(Func<IElement> find) => _find = find;

    /// <summary>Whether something is drawn or a name is typed.</summary>
    public bool IsSigned => _find().QuerySelector("[data-part=surface] > svg:not([data-part=draft]) > path") is not null
        || !string.IsNullOrWhiteSpace(_find().QuerySelector("[data-part=typed-name] input")?.GetAttribute("value"));

    /// <summary>Draws a stroke through these points, in the signature's space of 600 by 200, as a pointer pressed on the
    /// first and lifted on the last would. Throws when the pad is switched to typing a name.</summary>
    public Task DrawAsync(params (double X, double Y)[] points) =>
        (_find().QuerySelector("[data-part=surface]")
            ?? throw new InvalidOperationException("The signature pad is set to typing a name, so it can't be drawn on."))
            .TriggerEventAsync("ontbsignaturestroke", new SignatureStrokeEventArgs
            {
                Points = [.. points.SelectMany(point => new[] { point.X, point.Y })],
            });

    /// <summary>Clicks Undo, which takes back the last stroke. Throws when there's nothing to take back or the pad is
    /// disabled or read-only.</summary>
    public Task UndoAsync() => Button("undo").ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Clicks Clear. Throws when there's nothing to clear or the pad is disabled or read-only.</summary>
    public Task ClearAsync() => Button("clear").ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Switches to typing a name if the pad isn't already, and types this one, which the pad takes as the
    /// field changes.</summary>
    public async Task TypeNameAsync(string name)
    {
        if (_find().QuerySelector("[data-part=type-name]")?.GetAttribute("aria-pressed") != "true")
        {
            await Button("type-name").ClickAsync(new MouseEventArgs { Detail = 1 });
        }

        await _find().QuerySelector("[data-part=typed-name] input")!.ChangeAsync(new ChangeEventArgs { Value = name });
    }

    private IElement Button(string part) =>
        _find().QuerySelector($"[data-part={part}]") is { } button && !button.HasAttribute("disabled")
            ? button : throw new InvalidOperationException($"The signature pad's {part} button is disabled.");
}

/// <summary>A <c>TbCalendar</c>, found by its label.</summary>
public sealed class TbCalendarDriver
{
    private readonly Func<IElement> _find;

    internal TbCalendarDriver(Func<IElement> find) => _find = find;

    /// <summary>Picks this date as a user would: up to the years, over to its decade, then down through its year and
    /// month to the day. A disabled or read-only calendar, or a date it can't choose, throws.</summary>
    public async Task ChooseAsync(DateOnly date)
    {
        var grid = Grid();
        if (grid.GetAttribute("aria-disabled") == "true" || grid.GetAttribute("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This calendar is disabled or read-only, so no date can be chosen.");
        }

        while (Grid().GetAttribute("data-view") != "years")
        {
            await ClickAsync(_find().QuerySelector("[data-part=title]")!);
        }

        var year = $"{date.Year:0000}-01-01";
        while (Cell(year) is null)
        {
            var shown = int.Parse(Grid().QuerySelector("[data-date]")!.GetAttribute("data-date")![..4],
                CultureInfo.InvariantCulture);
            await ClickAsync(_find().QuerySelector(shown > date.Year ? "[data-part=previous]" : "[data-part=next]")!);
        }

        await ClickCellAsync(year, date);
        await ClickCellAsync($"{date.Year:0000}-{date.Month:00}-01", date);
        await ClickCellAsync(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), date);
    }

    private IElement Grid() => _find().QuerySelector("[role=grid]")
        ?? throw new InvalidOperationException("This calendar isn't interactive, so it shows no grid to pick from.");

    private IElement? Cell(string iso) => Grid().QuerySelector($"[data-date='{iso}']");

    private Task ClickCellAsync(string iso, DateOnly date) =>
        Cell(iso) is { } cell && cell.GetAttribute("aria-disabled") != "true" ? ClickAsync(cell)
            : throw new InvalidOperationException($"The calendar can't choose {date:yyyy-MM-dd}.");

    private static Task ClickAsync(IElement button) => button.ClickAsync(new MouseEventArgs { Detail = 1 });
}

/// <summary>A <c>TbRichTextEditor</c>, found by its label.</summary>
public sealed class TbRichTextEditorDriver
{
    private readonly Func<IElement> _find;

    internal TbRichTextEditorDriver(Func<IElement> find) => _find = find;

    /// <summary>Replaces the editor's text with this HTML and commits it, through the same event the editor's script
    /// raises in a browser, so the value keeps only the formats the editor allows. A disabled or read-only editor
    /// throws, since the user can't change it.</summary>
    public Task EnterAsync(string html)
    {
        var editor = _find();
        if (editor.GetAttribute("aria-disabled") == "true" || editor.GetAttribute("aria-readonly") == "true")
        {
            throw new InvalidOperationException("This editor is disabled or read-only, so its text can't be changed.");
        }

        return editor.TriggerEventAsync("ontbrichtextchange", new RichTextChangeEventArgs { Html = html });
    }
}

/// <summary>A <c>TbTreeView</c>, found by its accessible name. Rows are found by their path of texts from a root,
/// such as <c>("Docs", "Guide")</c>, with the same names as the Playwright helpers.</summary>
public sealed class TbTreeDriver
{
    private readonly Func<IElement> _find;

    internal TbTreeDriver(Func<IElement> find) => _find = find;

    /// <summary>The texts of the rows that show, in order.</summary>
    public IReadOnlyList<string> Texts => [.. OwnRows().Select(row => row.GetAttribute("data-text") ?? "")];

    /// <summary>The row at this path, which must be showing. When several rows match, it is the first.</summary>
    public IElement Item(params string[] path) => Items(path).FirstOrDefault()
        ?? throw new InvalidOperationException($"No row shows at {string.Join(" > ", path)}.");

    /// <summary>Every row at this path, for rows that share their text. Their parents must be open.</summary>
    public IReadOnlyList<IElement> Items(params string[] path)
    {
        var rows = OwnRows();
        List<IElement> found = [];
        Walk(0, 0);
        return found;

        // Each name is looked for after its parent row and before the next row at the parent's level.
        void Walk(int start, int depth)
        {
            for (var index = start; index < rows.Count; index++)
            {
                var level = int.Parse(rows[index].GetAttribute("aria-level")!, CultureInfo.InvariantCulture);
                if (level <= depth)
                {
                    break;
                }

                if (level == depth + 1 && rows[index].GetAttribute("data-text") == path[depth])
                {
                    if (depth == path.Length - 1)
                    {
                        found.Add(rows[index]);
                    }
                    else
                    {
                        Walk(index + 1, depth + 1);
                    }
                }
            }
        }
    }

    /// <summary>Opens each row along the path, as clicking its toggle does.</summary>
    public async Task ExpandAsync(params string[] path)
    {
        for (var depth = 1; depth <= path.Length; depth++)
        {
            var row = Item(path[..depth]);
            if (row.GetAttribute("aria-expanded") == "false")
            {
                await row.QuerySelector("[data-part=toggle]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
            }
        }
    }

    /// <summary>Opens every row, and the rows that appear inside them as they open.</summary>
    public async Task ExpandAllAsync()
    {
        while (OwnRows().FirstOrDefault(row => row.GetAttribute("aria-expanded") == "false") is { } row)
        {
            var key = row.GetAttribute("data-key");
            await row.QuerySelector("[data-part=toggle]")!.ClickAsync(new MouseEventArgs { Detail = 1 });
            // A disabled tree doesn't open, so the same row would be clicked forever.
            if (OwnRows().Any(again => again.GetAttribute("data-key") == key
                && again.GetAttribute("aria-expanded") == "false"))
            {
                throw new InvalidOperationException($"The row {row.GetAttribute("data-text")} didn't open.");
            }
        }
    }

    /// <summary>Opens the rows above the one at this path and selects it, as clicking it does.</summary>
    public async Task SelectAsync(params string[] path)
    {
        await ExpandAsync(path[..^1]);
        await Item(path).ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Opens the rows above the one at this path and checks it, as clicking its box does.</summary>
    public async Task CheckAsync(params string[] path)
    {
        await ExpandAsync(path[..^1]);
        await (Item(path).QuerySelector("[data-part=check]")
            ?? throw new InvalidOperationException($"The row at {string.Join(" > ", path)} has no box to check."))
            .ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Drops the row at <paramref name="row"/> just before the row at <paramref name="target"/>, as dragging
    /// it onto that row's top edge does. Both rows must be showing, and the row must be one the tree lets move.
    /// </summary>
    public Task DragBeforeAsync(string[] row, string[] target) => DragAsync(row, target, TreeMovePosition.Before);

    /// <summary>Drops the row at <paramref name="row"/> just after the row at <paramref name="target"/>, as dragging it
    /// onto that row's bottom edge does, which for an open row means above its first child. Both rows must be
    /// showing, and the row must be one the tree lets move.</summary>
    public Task DragAfterAsync(string[] row, string[] target) => DragAsync(row, target, TreeMovePosition.After);

    /// <summary>Drops the row at <paramref name="row"/> inside the row at <paramref name="target"/>, as dragging it
    /// onto that row's middle does. Both rows must be showing, and the row must be one the tree lets move.</summary>
    public Task DragIntoAsync(string[] row, string[] target) => DragAsync(row, target, TreeMovePosition.Inside);

    // Follows the tree's script: a selected row brings the other selected rows that can move, a drop below an open
    // row goes above its first child, and a drop on a moving row does nothing. The tree then checks it with CanMove.
    private Task DragAsync(string[] row, string[] target, TreeMovePosition position)
    {
        var source = Item(row);
        if (!source.HasAttribute("data-draggable"))
        {
            throw new InvalidOperationException($"The row at {string.Join(" > ", row)} can't be dragged: the tree "
                + "needs an OnMove and mustn't be disabled, and the row must be enabled, have a value and be allowed "
                + "by CanMoveItem.");
        }

        var rows = OwnRows();
        List<IElement> moved = Selected(source)
            ? [.. rows.Where(other => Selected(other) && other.HasAttribute("data-draggable"))] : [source];
        var onto = Item(target);
        if (moved.Contains(onto))
        {
            return Task.CompletedTask;
        }

        var next = rows.ElementAtOrDefault(rows.IndexOf(onto) + 1);
        if (position == TreeMovePosition.After && onto.GetAttribute("aria-expanded") == "true" && next is not null
            && Level(next) > Level(onto))
        {
            (onto, position) = (next, TreeMovePosition.Before);
        }

        return moved.Contains(onto) ? Task.CompletedTask : _find().TriggerEventAsync("ontbtreedrop",
            new TreeDropEventArgs
            {
                Rows = [.. moved.Select(other => other.GetAttribute("data-key")!)],
                Target = onto.GetAttribute("data-key")!,
                Position = position,
            });

        static bool Selected(IElement other) => other.GetAttribute("aria-selected") == "true";

        static int Level(IElement other) => int.Parse(other.GetAttribute("aria-level")!, CultureInfo.InvariantCulture);
    }

    // Leaves out the rows of a tree nested inside one of this tree's rows.
    private List<IElement> OwnRows()
    {
        var tree = _find();
        return [.. tree.QuerySelectorAll("[role=treeitem][data-key]").Where(row => row.Closest("[role=tree]") == tree)];
    }
}

/// <summary>A <c>TbDataGrid</c>, found by its table's accessible name.</summary>
public sealed class TbGridDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbGridDriver(Func<string, IReadOnlyList<IElement>> findAll, Func<IElement> find) =>
        (_findAll, _find) = (findAll, find);

    /// <summary>The row with a cell whose text is exactly this.</summary>
    public IElement Row(string cell)
    {
        var rows = _find().QuerySelectorAll("tbody tr[data-part=row]")
            .Where(row => row.Children.Any(td => td.TextContent.Trim() == cell)).ToList();
        return rows.Count == 1 ? rows[0]
            : throw new InvalidOperationException(rows.Count == 0
                ? $"No row has a cell that reads \"{cell}\"."
                : $"{rows.Count} rows have a cell that reads \"{cell}\".");
    }

    /// <summary>The cell under the column with this title in the row with a cell whose text is exactly
    /// <paramref name="row"/>.</summary>
    public IElement Cell(string row, string column) =>
        Row(row).QuerySelector($"td[data-column=\"{Header(column).GetAttribute("data-column")}\"]")
        ?? throw new InvalidOperationException($"The row \"{row}\" has no cell under \"{column}\".");

    /// <summary>Selects the cells from the one under <paramref name="fromColumn"/> in the row <paramref name="fromRow"/>
    /// to the one under <paramref name="toColumn"/> in the row <paramref name="toRow"/>, as a drag across them does. Each
    /// row is found by a cell whose text is exactly that value. Throws when the grid's <c>CellSelection</c> is off.
    /// </summary>
    public Task SelectCellsAsync(string fromRow, string fromColumn, string toRow, string toColumn)
    {
        var table = _find();
        if (!table.HasAttribute("data-cell-selection"))
        {
            throw new InvalidOperationException("The grid doesn't select cells; set its CellSelection.");
        }

        var (from, to) = (Cell(fromRow, fromColumn), Cell(toRow, toColumn));
        return table.TriggerEventAsync("ontbcellrange", new CellRangeEventArgs
        {
            FromRow = from.ParentElement!.GetAttribute("data-key"),
            FromColumn = from.GetAttribute("data-column"),
            ToRow = to.ParentElement!.GetAttribute("data-key"),
            ToColumn = to.GetAttribute("data-column"),
        });
    }

    /// <summary>The text of the selected range's cells, row by row, as Ctrl+C copies them; empty without a range.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> SelectedCells =>
        _find().Closest("[data-part=grid]")?.QuerySelector(":scope > [data-part=copy]") is { } copy
            ? [.. copy.QuerySelectorAll("tr").Select(row => (IReadOnlyList<string>)
                [.. row.QuerySelectorAll("td").Select(cell => cell.TextContent)])]
            : [];

    /// <summary>Sorts the rows by this column alone, ascending or descending, clicking its header until it shows that
    /// order.</summary>
    public async Task SortAsync(string column, bool descending = false)
    {
        var wanted = descending ? "descending" : "ascending";
        // A plain click sorts by the column alone; after that it reverses the order, then stops sorting by the column.
        for (var clicks = 0; clicks < 4; clicks++)
        {
            var header = Header(column);
            if (header.GetAttribute("aria-sort") == wanted && _find().QuerySelector("[data-part=sort-order]") is null)
            {
                return;
            }

            await (header.QuerySelector("[data-part=sort]") ?? throw new InvalidOperationException(
                $"The \"{column}\" column can't be sorted; make it Sortable.")).ClickAsync(new MouseEventArgs());
        }

        throw new InvalidOperationException($"The \"{column}\" column didn't sort {wanted}.");
    }

    /// <summary>Goes to this page with the grid's pager, as <see cref="TbPaginationDriver.GoToPageAsync"/> does.
    /// </summary>
    public Task GoToPageAsync(int number)
    {
        var pager = new TbPaginationDriver(() =>
            _find().Closest("[data-part=grid]")?.QuerySelector(":scope > [data-part=pager] nav")
            ?? throw new InvalidOperationException(
                "The grid shows no page buttons; it needs a PageSize and more than one page of rows."));
        return pager.GoToPageAsync(number);
    }

    /// <summary>Clicks the row with a cell whose text is exactly this, as the browser reports a click on a plain cell
    /// away from the row's controls: the grid raises <c>OnRowClick</c> and, with <c>RowClickSelects</c>, selects it.
    /// </summary>
    public Task ClickRowAsync(string cell) =>
        Row(cell).TriggerEventAsync("ontbrowclick", new RowClickEventArgs());

    /// <summary>Clicks the Save changes button of a grid whose <c>EditMode</c> is <c>Batch</c>. Throws when the grid
    /// holds no changes or doesn't edit in batches; an edit still open doesn't count as a change until it ends.
    /// </summary>
    public Task SaveChangesAsync() => BatchButton("save-changes").ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Clicks the Discard changes button of a grid whose <c>EditMode</c> is <c>Batch</c>. Throws when the grid
    /// holds no changes or doesn't edit in batches; an edit still open doesn't count as a change until it ends.
    /// </summary>
    public Task DiscardChangesAsync() => BatchButton("discard-changes").ClickAsync(new MouseEventArgs { Detail = 1 });

    private IElement BatchButton(string part)
    {
        var button = _find().Closest("[data-part=grid]")?.QuerySelector($":scope > [data-part=batch-bar] [data-part={part}]")
            ?? throw new InvalidOperationException("The grid has no batch bar; set its EditMode to Batch.");
        return button.GetAttribute("aria-disabled") == "true"
            ? throw new InvalidOperationException("The grid holds no changes.") : button;
    }

    /// <summary>Opens the tree grid row with a cell whose text is exactly this, with its button, so its child rows
    /// show. A row already open stays open.</summary>
    public Task ExpandAsync(string row) => SetExpandedAsync(row, true);

    /// <summary>Closes the tree grid row with a cell whose text is exactly this, with its button, so its child rows
    /// hide. A row already closed stays closed.</summary>
    public Task CollapseAsync(string row) => SetExpandedAsync(row, false);

    private Task SetExpandedAsync(string row, bool expanded)
    {
        var found = Row(row);
        var toggle = found.QuerySelector("[data-part=tree-toggle]") ?? throw new InvalidOperationException(
            $"The row \"{row}\" has no child rows to show; a tree grid's row needs children from its ChildrenSelector.");
        return found.GetAttribute("aria-expanded") == (expanded ? "true" : "false") ? Task.CompletedTask
            : toggle.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Moves the row with a cell whose text is exactly <paramref name="row"/> to <paramref name="index"/>
    /// among all the rows, on every page, through its handle's menu as a user would: Move to start or Move to end for
    /// the first or last place, else Move up or Move down a place at a time, so <c>CanMoveRow</c> and
    /// <c>OnRowMove</c> see each step. A step the app refuses ends the move. The row must be on the page shown, and the
    /// grid shows the page it lands on. Throws when the rows can't move or <paramref name="index"/> is past the last
    /// row.</summary>
    public Task MoveRowAsync(string row, int index) => HandleMenu.MoveAsync(
        () => MovableRow(row).Children.First(cell => cell.ClassList.Contains("tb-data-grid__handle"))
            .FirstElementChild!,
        () => int.Parse(MovableRow(row).GetAttribute("data-index")!, CultureInfo.InvariantCulture),
        int.Parse(_find().GetAttribute("data-count") ?? "0", CultureInfo.InvariantCulture), index, row);

    private IElement MovableRow(string cell) => Row(cell) is var row && row.HasAttribute("data-index") ? row
        : throw new InvalidOperationException("The grid's rows can't move: it needs an OnRowMove and in-memory Items, "
            + "and mustn't be sorted, filtered or grouped.");

    /// <summary>Selects the row with a cell whose text is exactly this, with its checkbox or radio button, unless it
    /// is selected already.</summary>
    public Task SelectAsync(string row)
    {
        var found = Row(row);
        var box = found.QuerySelector("[data-part=select] input")
            ?? throw new InvalidOperationException("The grid's rows can't be selected; set its SelectionMode.");
        return found.HasAttribute("data-selected") ? Task.CompletedTask : box.ChangeAsync(new ChangeEventArgs());
    }

    /// <summary>Filters the column with this title by this text. In the filter row, it types the text and commits it,
    /// as pressing Enter does, so a text filter applies without waiting for typing to pause; in a filter menu, it types
    /// the text as the value and applies the menu. For a text or number filter; choose a row's choice filter with
    /// <c>cut.Tb().Select("Filter Status")</c>, or a list filter's choices with <see cref="FilterChoicesAsync"/>.
    /// </summary>
    public async Task FilterAsync(string column, string text)
    {
        if (Menu(column) is not null)
        {
            await EnterAsync(ValueInput(column) ?? throw new InvalidOperationException(
                $"The \"{column}\" column's filter takes no typed value."), text);
            await ApplyAsync(column);
            return;
        }

        var input = FilterCell(column).QuerySelector("input")
            ?? throw new InvalidOperationException($"The \"{column}\" column has no filter; make it Filterable.");
        if (input.Matches("input[type=number], input[role=spinbutton]"))
        {
            await new TbNumberFieldDriver(() => input).EnterAsync(text);
            return;
        }

        // Send the text as an input and then as a change, the way typing and then pressing Enter would.
        await input.InputAsync(new ChangeEventArgs { Value = text });
        await input.ChangeAsync(new ChangeEventArgs { Value = text });
    }

    /// <summary>In the filter menu of the column with this title, chooses the condition with this name, such as
    /// "At least", types the text as the value, unless the condition takes none, and applies the menu.</summary>
    public async Task FilterAsync(string column, string condition, string text)
    {
        var menu = Menu(column) ?? throw new InvalidOperationException(
            $"The \"{column}\" column has no filter menu; set the grid's FilterMode to Menu.");
        var conditions = menu.QuerySelector("[role=combobox]") ?? throw new InvalidOperationException(
            $"The \"{column}\" column's filter has no conditions; use FilterAsync(column, text) or FilterChoicesAsync.");
        await new TbSelectDriver(() => conditions, _findAll).ChooseAsync(condition);
        // Choosing renders the menu again, without a value field for a condition that takes none.
        if (ValueInput(column) is { } input)
        {
            await EnterAsync(input, text);
        }

        await ApplyAsync(column);
    }

    /// <summary>Leaves exactly these choices picked in the list filter of the column with this title, naming each as
    /// the filter shows it; pass none to clear the filter. Works with the filter row's multiple select and with a
    /// filter menu's checkboxes, which it then applies.</summary>
    public async Task FilterChoicesAsync(string column, params string[] choices)
    {
        if (Menu(column) is null)
        {
            var cell = FilterCell(column);
            if (!IsList(cell))
            {
                throw NotAList(column);
            }

            if (cell.QuerySelector("[data-part=clear]") is { } clear)
            {
                await clear.ClickAsync(new MouseEventArgs { Detail = 1 });
            }

            if (choices.Length > 0)
            {
                await new TbSelectDriver(() => FilterCell(column).QuerySelector("[role=combobox]")!, _findAll)
                    .ChooseAsync(choices);
            }

            return;
        }

        var names = Menu(column)!.QuerySelectorAll("input[type=checkbox]")
            .Select(box => Choices.NameOf(box.Closest("label")!)).ToList();
        if (names.Count == 0)
        {
            throw new InvalidOperationException(
                $"The \"{column}\" column's filter isn't a list; give the column FilterChoices.");
        }

        if (choices.FirstOrDefault(choice => !names.Contains(choice)) is { } missing)
        {
            throw new InvalidOperationException($"The \"{column}\" column's filter has no choice \"{missing}\".");
        }

        foreach (var name in names)
        {
            // Each change renders the menu again, so the box is found afresh.
            var box = Menu(column)!.QuerySelectorAll("input[type=checkbox]")
                .Single(other => Choices.NameOf(other.Closest("label")!) == name);
            if (box.HasAttribute("checked") != choices.Contains(name))
            {
                await box.ChangeAsync(new ChangeEventArgs { Value = choices.Contains(name) });
            }
        }

        await ApplyAsync(column);
    }

    private const string ValueSelector = "[data-part=input]:not([role=combobox]):not([type=checkbox])";

    /// <summary>The columns' titles, in the order they show.</summary>
    public IReadOnlyList<string> Columns => _find().QuerySelectorAll("thead th[data-title]")
        .Select(header => header.GetAttribute("data-title")!).ToList();

    /// <summary>The text of the totals row's cell under the column with this title, such as <c>"Sum 42.1"</c>, or
    /// null when the column shows no total.</summary>
    public string? Total(string column)
    {
        var key = Header(column).GetAttribute("data-column");
        return TotalText(_find().QuerySelector($"tfoot [data-part=totals] td[data-column=\"{key}\"]"));
    }

    /// <summary>The text of each open group's totals cell under the column with this title, in the order the groups
    /// show, with null for a group that shows no total there. The grid shows these rows with <c>GroupTotals</c>.
    /// </summary>
    public IReadOnlyList<string?> GroupTotals(string column)
    {
        var key = Header(column).GetAttribute("data-column");
        return [.. _find().QuerySelectorAll($"tbody [data-part=group-totals] td[data-column=\"{key}\"]")
            .Select(TotalText)];
    }

    // The row's name for screen readers isn't part of the total.
    private static string? TotalText(IElement? cell)
    {
        var shown = (IElement?)cell?.Clone();
        shown?.QuerySelector("[data-part=totals-name]")?.Remove();
        var text = shown?.TextContent;
        return string.IsNullOrWhiteSpace(text) ? null
            : string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Resizes the column with this title to this many pixels, as the grid's script reports a drag of its
    /// header's edge. bUnit has no layout, so the other columns keep only the widths the grid already gave them, and
    /// the column's <c>MinWidth</c>, which the browser measures, doesn't apply.</summary>
    public Task ResizeColumnAsync(string column, double width)
    {
        var header = Header(column);
        if (header.QuerySelector("[data-part=resize]") is null)
        {
            throw new InvalidOperationException($"The \"{column}\" column can't be resized; set the grid's "
                + "ResizableColumns and leave the column's Resizable on.");
        }

        var widths = _find().QuerySelectorAll("thead th[data-width]").ToDictionary(
            other => other.GetAttribute("data-column")!,
            other => double.Parse(other.GetAttribute("data-width")!, CultureInfo.InvariantCulture));
        widths[header.GetAttribute("data-column")!] = width;
        return _find().TriggerEventAsync("ontbcolumnwidths", new ColumnWidthsEventArgs { Widths = widths });
    }

    private IElement Header(string column) => _find().QuerySelectorAll("thead th")
        .FirstOrDefault(header => header.GetAttribute("data-title") == column)
        ?? throw new InvalidOperationException($"No column is titled \"{column}\".");

    private IElement? Menu(string column) => Header(column).QuerySelector("[data-part=filter-menu]");

    private IElement FilterCell(string column) =>
        _find().QuerySelector($"[data-part=filter][data-column=\"{Header(column).GetAttribute("data-column")}\"]")
        ?? throw new InvalidOperationException($"The \"{column}\" column has no filter; make it Filterable.");

    private IElement? ValueInput(string column) => Menu(column)!.QuerySelector(ValueSelector);

    // A row's list filter is a multiple select; an enum's or a bool's is a select of one.
    private static bool IsList(IElement cell) =>
        cell.QuerySelector("[role=combobox]")?.GetAttribute("aria-controls") is { } list
        && cell.QuerySelector($"[id='{list}'][aria-multiselectable=true]") is not null;

    private static InvalidOperationException NotAList(string column) => new($"The \"{column}\" column's filter "
        + "isn't a list; give the column FilterChoices, or set the grid's FilterMode to Menu for an enum or bool.");

    private Task ApplyAsync(string column) => (Menu(column)!.QuerySelector("[data-part=filter-apply]")
        ?? throw new InvalidOperationException(
            $"The \"{column}\" column's filter is a template; use the controls it draws."))
        .ClickAsync(new MouseEventArgs { Detail = 1 });

    // A text field that updates as the user types takes input; the rest commit on change.
    private static Task EnterAsync(IElement input, string text) =>
        input.Matches("input[type=number], input[role=spinbutton]")
            ? new TbNumberFieldDriver(() => input).EnterAsync(text)
            : input.HasAttribute("blazor:oninput") ? input.InputAsync(new ChangeEventArgs { Value = text })
            : input.ChangeAsync(new ChangeEventArgs { Value = text });
}

/// <summary>A <c>TbCommandPalette</c>, found by its label.</summary>
public sealed class TbCommandPaletteDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbCommandPaletteDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>Whether the palette is open.</summary>
    public bool IsOpen => SearchBox() is not null;

    /// <summary>The names of the commands it shows, in order.</summary>
    public IReadOnlyList<string> Results => [.. Listbox().QuerySelectorAll("[data-part=option]").Select(NameOf)];

    /// <summary>Types this text in the search box, as the user does, so the list shows what matches.</summary>
    public Task SearchAsync(string text) => Open().InputAsync(new ChangeEventArgs { Value = text });

    /// <summary>Runs the command with this name, as clicking it does, and returns once the app's <c>OnChoose</c> has
    /// run. Throws for a closed palette, or a command it doesn't show or that is disabled.</summary>
    public Task ChooseAsync(string name)
    {
        var option = Listbox().QuerySelectorAll("[data-part=option]")
            .SingleOrDefault(option => NameOf(option) == name)
            ?? throw new InvalidOperationException($"The command palette shows no command named \"{name}\".");
        return option.GetAttribute("aria-disabled") == "true"
            ? throw new InvalidOperationException($"The command \"{name}\" is disabled.")
            : option.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    // A component inside a template can have a label part of its own, so only the palette's own label counts.
    private static string NameOf(IElement option) => (option.Children
        .FirstOrDefault(child => child.ClassList.Contains("tb-command-palette__text"))?.Children
        .FirstOrDefault(child => child.GetAttribute("data-part") == "label") ?? option).TextContent.Trim();

    private IElement? SearchBox()
    {
        var label = _find().GetAttribute("data-command-palette");
        return _findAll(".tb-command-palette__input")
            .SingleOrDefault(input => input.GetAttribute("aria-label") == label);
    }

    private IElement Open() => SearchBox() ?? throw new InvalidOperationException(
        "The command palette is closed; open it as the app does, such as with its button.");

    private IElement Listbox() => _findAll($"[id='{Open().GetAttribute("aria-controls")}']").Single();
}

/// <summary>A <c>TbSortable</c>, found by its label.</summary>
public sealed class TbSortableDriver
{
    private readonly Func<IElement> _find;

    internal TbSortableDriver(Func<IElement> find) => _find = find;

    /// <summary>The items' text, in the order they show.</summary>
    public IReadOnlyList<string> Items => [.. ItemElements().Select(TextOf)];

    /// <summary>Moves the item whose text is exactly <paramref name="item"/> to <paramref name="index"/>, through its
    /// handle's menu as a user would: Move to start or Move to end for the first or last place, else Move up or Move
    /// down a place at a time, so <c>CanMove</c> and <c>OnMove</c> see each step. A step the app refuses ends the
    /// move. Throws when the list is disabled or <paramref name="index"/> is past its last item.</summary>
    public Task MoveAsync(string item, int index) => HandleMenu.MoveAsync(
        () => Item(item).Children.First(child => child.GetAttribute("data-part") == "handle"),
        () => ItemElements().IndexOf(Item(item)), ItemElements().Count, index, item);

    /// <summary>Moves the item whose text is exactly <paramref name="item"/> to the end of the list labeled
    /// <paramref name="list"/> in its group, through its handle's menu as a user would. Throws when either list is
    /// disabled or no other list in the group has that label.</summary>
    public Task MoveToListAsync(string item, string list)
    {
        var menu = Item(item).Children.First(child => child.GetAttribute("data-part") == "handle");
        var trigger = HandleMenu.TriggerOf(menu, item);
        var choice = menu.QuerySelectorAll("[role=menuitem][data-list]")
            .FirstOrDefault(choice => choice.GetAttribute("data-list") == list)
            ?? throw new InvalidOperationException($"\"{item}\" can't move to \"{list}\": no other list in its group "
                + "has that label.");
        return TbMenuDriver.ClickAsync(trigger, choice);
    }

    private List<IElement> ItemElements() =>
        [.. _find().Children.Where(child => child.GetAttribute("data-part") == "item")];

    private static string TextOf(IElement item) =>
        item.Children.First(child => child.ClassList.Contains("tb-sortable__content")).TextContent.Trim();

    private IElement Item(string text)
    {
        var items = ItemElements().Where(item => TextOf(item) == text).ToList();
        return items.Count == 1 ? items[0]
            : throw new InvalidOperationException($"Expected one item that reads \"{text}\", found {items.Count}.");
    }
}

// Moves an item or a row through its handle's menu, whose items are Move up, Move down, Move to start and Move to end
// in that order, so they're found by place, whatever the app's labels call them.
internal static class HandleMenu
{
    // Finds the handle's menu button, and throws when its list is disabled.
    internal static IElement TriggerOf(IElement menu, string name)
    {
        var trigger = menu.QuerySelector("[aria-haspopup=menu]")!;
        return trigger.HasAttribute("disabled") || trigger.GetAttribute("aria-disabled") == "true"
            ? throw new InvalidOperationException($"\"{name}\" can't move, because its list is disabled.")
            : trigger;
    }

    internal static async Task MoveAsync(Func<IElement> handle, Func<int> place, int count, int index, string name)
    {
        // A grid whose rows can't move has no count, so finding the row first throws the reason, not an out-of-range
        // index.
        var from = place();
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
        while (from != index)
        {
            var menu = handle();
            var trigger = TriggerOf(menu, name);

            var step = index == 0 ? 2 : index == count - 1 ? 3 : index < from ? 0 : 1;
            await TbMenuDriver.ClickAsync(trigger, menu.QuerySelectorAll("[role=menuitem]")[step]);
            var now = place();
            if (now == from)
            {
                return;
            }

            from = now;
        }
    }
}

/// <summary>A <c>TbTabs</c>, found by its accessible name.</summary>
public sealed class TbTabsDriver
{
    private readonly Func<IElement> _find;

    internal TbTabsDriver(Func<IElement> find) => _find = find;

    /// <summary>The tabs' texts, in order.</summary>
    public IReadOnlyList<string> Tabs =>
        _find().QuerySelectorAll("[role=tab]").Select(tab => tab.TextContent.Trim()).ToList();

    /// <summary>The selected tab's text.</summary>
    public string? Selected => _find().QuerySelector("[role=tab][aria-selected=true]")?.TextContent.Trim();

    /// <summary>Selects the tab with this text, as clicking it does.</summary>
    public Task SelectAsync(string tab) => Find(tab).ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Closes the tab with this text, as clicking its close mark does. Throws for a tab that can't be closed.
    /// </summary>
    public Task CloseAsync(string tab) =>
        (Find(tab).QuerySelector("[data-part=close]")
            ?? throw new InvalidOperationException($"The tab \"{tab}\" can't be closed."))
        .ClickAsync(new MouseEventArgs { Detail = 1 });

    /// <summary>Clicks the add button that follows the tab list. Throws for tabs with no add button.</summary>
    public Task AddAsync()
    {
        var add = _find().ParentElement?.Children.FirstOrDefault(cell => cell.ClassName == "tb-tabs__actions")
            ?.QuerySelector("[data-part=add]");
        return (add ?? throw new InvalidOperationException("The tabs have no add button."))
            .ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    private IElement Find(string tab) =>
        _find().QuerySelectorAll("[role=tab]").SingleOrDefault(element => element.TextContent.Trim() == tab)
            ?? throw new InvalidOperationException($"No tab reads \"{tab}\".");
}

/// <summary>A <c>TbHotkey</c>, found by its keys.</summary>
public sealed class TbHotkeyDriver
{
    private readonly Func<IElement> _find;

    internal TbHotkeyDriver(Func<IElement> find) => _find = find;

    /// <summary>Fires the hotkey as its script does when its keys are pressed where it applies. Throws for a disabled
    /// hotkey, whose keys reach the page instead.</summary>
    public Task PressAsync()
    {
        var hotkey = _find();
        return hotkey.HasAttribute("data-disabled")
            ? throw new InvalidOperationException($"The hotkey \"{hotkey.GetAttribute("data-keys")}\" is disabled.")
            : hotkey.TriggerEventAsync("ontbhotkey", EventArgs.Empty);
    }
}

/// <summary>A <c>TbContextMenu</c>, found by its menu's name.</summary>
public sealed class TbContextMenuDriver
{
    private readonly Func<IElement> _find;
    private readonly string _key;

    internal TbContextMenuDriver(Func<IElement> find, string key = "") => (_find, _key) = (find, key);

    /// <summary>The same context menu, opened on the element inside it with this <c>data-context-key</c>, such as a
    /// row's id: <c>cut.Tb().ContextMenu().On("ridge-loop").ChooseAsync("Delete")</c>.</summary>
    public TbContextMenuDriver On(string key) => new(_find, key);

    /// <summary>Opens the menu as a right-click would and clicks the item with this name, through the submenus named
    /// before it, as in <c>ChooseAsync("Move to", "Archive")</c>. Throws for a disabled context menu or item, a key or
    /// name it doesn't have, or a static page, where the browser's own menu opens instead.</summary>
    public async Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        var target = _find();
        if (target.HasAttribute("data-static"))
        {
            throw new InvalidOperationException("This context menu is on a static page, where the browser's own menu "
                + "opens; render it on an interactive page.");
        }

        if (target.HasAttribute("data-disabled"))
        {
            throw new InvalidOperationException("This context menu is disabled.");
        }

        if (_key.Length > 0 && !target.QuerySelectorAll("[data-context-key]")
            .Any(element => element.GetAttribute("data-context-key") == _key))
        {
            throw new InvalidOperationException($"The context menu has no element with the key \"{_key}\".");
        }

        // The menu's script asks for another key's items the same way before it shows them.
        if (Menu(target).GetAttribute("data-key") != _key)
        {
            await target.TriggerEventAsync("ontbcontextmenuopen", new ContextMenuOpenEventArgs { Key = _key });
            target = _find();
        }

        var list = Menu(target);
        foreach (var name in path[..^1])
        {
            var opener = TbMenuDriver.Find(list, name, "[data-part=submenu-trigger]", "submenu");
            list = target.QuerySelector($"[id='{opener.GetAttribute("aria-controls")}']")!;
        }

        await TbMenuDriver.ClickAsync(target, TbMenuDriver.Find(list, path[^1], TbMenuDriver.Items, "item"));
    }

    internal static IElement Menu(IElement target) =>
        target.Children.Single(child => child.GetAttribute("role") == "menu");
}

/// <summary>A <c>TbViewport</c>, found by its box's <c>Label</c>. Its controls are clicked as a pointer would; the
/// keys, the wheel and a drag need a browser.</summary>
public sealed class TbViewportDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbViewportDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>The zoom, where 1 shows the content at its own size.</summary>
    public double Zoom => double.Parse(_find().GetAttribute("data-zoom")!, CultureInfo.InvariantCulture);

    /// <summary>The fit the zoom follows, or <see cref="ViewportFit.None"/>.</summary>
    public ViewportFit Fit => Enum.Parse<ViewportFit>(_find().GetAttribute("data-fit")!);

    /// <summary>Clicks the zoom in button. Throws when the zoom is at its largest or the viewport shows no controls.
    /// </summary>
    public Task ZoomInAsync() => ZoomAsync("zoom-in");

    /// <summary>Clicks the zoom out button. Throws when the zoom is at its smallest or the viewport shows no
    /// controls.</summary>
    public Task ZoomOutAsync() => ZoomAsync("zoom-out");

    private Task ZoomAsync(string part)
    {
        var button = Control(part);
        return button.GetAttribute("aria-disabled") == "true"
            ? throw new InvalidOperationException("The viewport can't zoom any further that way.")
            : button.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    /// <summary>Picks <see cref="ViewportFit.Width"/> or <see cref="ViewportFit.Page"/> from the zoom menu.</summary>
    public Task FitAsync(ViewportFit fit)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(fit, ViewportFit.None);
        var (trigger, list) = Menu();
        return TbMenuDriver.ClickAsync(trigger, list.QuerySelectorAll("[role=menuitemradio]")[fit == ViewportFit.Width ? 0 : 1]);
    }

    /// <summary>Picks Actual size from the zoom menu, which shows the content at 100%.</summary>
    public Task ActualSizeAsync()
    {
        var (trigger, list) = Menu();
        return TbMenuDriver.ClickAsync(trigger, list.QuerySelector("[role=menuitem]")!);
    }

    private IElement Control(string part) => _find().QuerySelector($"[data-part=controls] [data-part={part}]")
        ?? throw new InvalidOperationException("The viewport shows no controls; set ShowControls.");

    private (IElement Trigger, IElement List) Menu()
    {
        var trigger = Control("zoom-menu").QuerySelector("[aria-haspopup=menu]")!;
        return (trigger, _findAll($"[id='{trigger.GetAttribute("aria-controls")}']").Single());
    }
}

/// <summary>A <c>TbQueryBuilder</c>, found by its <c>Label</c>. A group is named by its path of group numbers from
/// the outer group, so no numbers is the outer group itself and <c>2, 1</c> is the first group inside the outer
/// group's second group.</summary>
public sealed class TbQueryBuilderDriver
{
    private readonly Func<IElement> _find;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbQueryBuilderDriver(Func<IElement> find, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_find, _findAll) = (find, findAll);

    /// <summary>The fields of a group's condition with this number, found by their labels, Field, Comparison and
    /// Value: <c>Condition(1).Select("Field").ChooseAsync("Region")</c>.</summary>
    public TrailblazorMarkup Condition(int number, params int[] group) =>
        new TrailblazorMarkup(_findAll).Within(() => ConditionOf(number, group));

    /// <summary>Clicks the group's Add condition button, which adds a condition on the first field.</summary>
    public Task AddConditionAsync(params int[] group) => Click(GroupOf(group), "group-actions", "add-condition");

    /// <summary>Clicks the group's Add group button, which adds a group with one condition inside it. Throws when the
    /// group is at the deepest level.</summary>
    public Task AddGroupAsync(params int[] group) => Click(GroupOf(group), "group-actions", "add-group");

    /// <summary>Clicks the remove button of the group's condition with this number.</summary>
    public Task RemoveConditionAsync(int number, params int[] group) =>
        ConditionOf(number, group).QuerySelector("[data-part=remove-condition]")!.ClickAsync(new MouseEventArgs());

    /// <summary>Clicks the remove button of the group at this path, which can't be the outer group.</summary>
    public Task RemoveGroupAsync(params int[] group)
    {
        ArgumentOutOfRangeException.ThrowIfZero(group.Length, nameof(group));
        return Click(GroupOf(group), "group-header", "remove-group");
    }

    /// <summary>Picks whether the group matches items that meet all of its conditions or any of them.</summary>
    public Task JoinAsync(QueryJoin join, params int[] group)
    {
        var select = new TbSelectDriver(() => GroupOf(group).QuerySelector(":scope > [data-part=group-header] "
            + "[role=combobox]")!, _findAll);
        return select.ChooseAsync(select.Options[join == QueryJoin.And ? 0 : 1]);
    }

    private static Task Click(IElement group, string part, string button) =>
        (group.QuerySelector($":scope > [data-part={part}] [data-part={button}]") ?? throw new InvalidOperationException(
            $"The query builder's group has no {button.Replace('-', ' ')} button here.")).ClickAsync(new MouseEventArgs());

    private IElement ConditionOf(int number, int[] group) =>
        GroupOf(group).QuerySelectorAll(":scope > [data-part=conditions] > li > [data-part=condition]")
            .ElementAtOrDefault(number - 1) ?? throw new InvalidOperationException(
                $"The query builder's group has no condition {number}.");

    private IElement GroupOf(int[] path)
    {
        var group = _find();
        foreach (var number in path)
        {
            group = group.QuerySelectorAll(":scope > [data-part=groups] > [data-part=group]").ElementAtOrDefault(number - 1)
                ?? throw new InvalidOperationException($"The query builder has no group {string.Join(", ", path)}.");
        }

        return group;
    }
}

/// <summary>A <c>TbMenu</c>, found by its trigger's name.</summary>
public sealed class TbMenuDriver
{
    internal const string Items = "[role=menuitem]:not([data-part=submenu-trigger]), [role=menuitemcheckbox], "
        + "[role=menuitemradio]";

    private readonly string _trigger;
    private readonly Func<string, IReadOnlyList<IElement>> _findAll;

    internal TbMenuDriver(string trigger, Func<string, IReadOnlyList<IElement>> findAll) =>
        (_trigger, _findAll) = (trigger, findAll);

    /// <summary>Clicks the item with this name, through the submenus named before it, as in
    /// <c>ChooseAsync("Export", "PDF")</c>. A checkbox or radio item is clicked the same way. Throws for a disabled
    /// menu or item, a name the menu doesn't have, or a button item on a static page, where only links work.</summary>
    public Task ChooseAsync(params string[] path)
    {
        ArgumentOutOfRangeException.ThrowIfZero(path.Length, nameof(path));
        var trigger = Trigger();
        if (trigger.HasAttribute("disabled") || trigger.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The menu \"{_trigger}\" is disabled.");
        }

        var list = ListOf(trigger);
        foreach (var name in path[..^1])
        {
            list = ListOf(Find(list, name, "[data-part=submenu-trigger]", "submenu"));
        }

        return ClickAsync(trigger, Find(list, path[^1], Items, "item"));
    }

    internal static Task ClickAsync(IElement trigger, IElement item)
    {
        if (item.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The menu item \"{NameOf(item)}\" is disabled.");
        }

        if (trigger.HasAttribute("data-static") && item.TagName != "A")
        {
            throw new InvalidOperationException(
                "This menu is on a static page, where only its links work; render it on an interactive page.");
        }

        return item.ClickAsync(new MouseEventArgs { Detail = 1 });
    }

    // The name the Playwright helpers match, with runs of whitespace collapsed, since a new-tab link's hint follows a
    // line break.
    private static string NameOf(IElement element) =>
        string.Join(' ', Choices.NameOf(element).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private IElement Trigger()
    {
        var triggers = _findAll("[aria-haspopup=menu][data-part=trigger]")
            .Where(button => NameOf(button) == _trigger).ToList();
        return triggers.Count == 1 ? triggers[0]
            : throw new InvalidOperationException(
                $"Expected one menu trigger named \"{_trigger}\", found {triggers.Count}.");
    }

    private IElement ListOf(IElement opener) =>
        _findAll($"[id='{opener.GetAttribute("aria-controls")}']").SingleOrDefault()
        ?? throw new InvalidOperationException($"The menu that \"{NameOf(opener)}\" opens isn't in the markup.");

    // A submenu's items sit inside the parent's list, so only the items whose nearest list is this one count.
    internal static IElement Find(IElement list, string name, string selector, string kind)
    {
        var found = list.QuerySelectorAll(selector)
            .Where(item => item.Closest("[role=menu]") == list && NameOf(item) == name).ToList();
        return found.Count switch
        {
            1 => found[0],
            0 => throw new InvalidOperationException($"The menu has no {kind} named \"{name}\"."),
            _ => throw new InvalidOperationException($"The menu has {found.Count} {kind}s named \"{name}\"."),
        };
    }
}
