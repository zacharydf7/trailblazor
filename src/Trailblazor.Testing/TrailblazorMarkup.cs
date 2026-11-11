using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Trailblazor.Testing;

/// <summary>bUnit helpers that find Trailblazor components by their labels and names, as a user would, with the same
/// names as the Playwright helpers in Trailblazor.Testing.Playwright.</summary>
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

    /// <summary>The <c>TbRichTextEditor</c> with this label, or with this <c>AriaLabel</c> when it has no label.
    /// </summary>
    public TbRichTextEditorDriver RichTextEditor(string label) =>
        new(() => Field(label, "[role=textbox][contenteditable]"));

    /// <summary>The <c>TbMenu</c> whose trigger has this name.</summary>
    public TbMenuDriver Menu(string trigger) => new(trigger, _findAll);

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
    public Task ChooseAsync(params string[] options) => Choices.ChooseAsync(() =>
    {
        var control = _find();
        Choices.ThrowIfStaticOrDisabled(control);
        // A filterable select points to the dialog that holds its filter and listbox.
        var controls = control.GetAttribute("aria-controls");
        return (OptionElements(), _findAll($"[id='{controls}'][aria-multiselectable=true], "
            + $"[id='{controls}'] [aria-multiselectable=true]").Count > 0);
    }, options);

    private IEnumerable<IElement> OptionElements()
    {
        var control = _find();
        return control.TagName == "SELECT"
            ? control.QuerySelectorAll("option:not([value='']):not([data-unmatched]):not([data-part=status])")
            : _findAll($"[id='{control.GetAttribute("aria-controls")}'] [role=option]:not([data-part=status])");
    }
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
    public Task ChooseAsync(params string[] options) => Choices.ChooseAsync(() =>
    {
        var list = _find();
        Choices.ThrowIfStaticOrDisabled(list);
        return (OptionElements(list), list.GetAttribute("aria-multiselectable") == "true");
    }, options);

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
    internal static async Task ChooseAsync(Func<(IEnumerable<IElement> Options, bool Multiple)> find, string[] texts)
    {
        ArgumentOutOfRangeException.ThrowIfZero(texts.Length, nameof(texts));
        if (texts.Length > 1 && !find().Multiple)
        {
            throw new ArgumentException("A field that takes one option can't choose several.", nameof(texts));
        }

        foreach (var text in texts)
        {
            var option = find().Options.SingleOrDefault(option => NameOf(option) == text)
                ?? throw new InvalidOperationException($"No option reads \"{text}\".");
            if (option.GetAttribute("aria-selected") != "true")
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
    public Task DragBeforeAsync(string[] row, string[] target) => DragAsync(row, target, TreeDropPosition.Before);

    /// <summary>Drops the row at <paramref name="row"/> just after the row at <paramref name="target"/>, as dragging it
    /// onto that row's bottom edge does, which for an open row means above its first child. Both rows must be
    /// showing, and the row must be one the tree lets move.</summary>
    public Task DragAfterAsync(string[] row, string[] target) => DragAsync(row, target, TreeDropPosition.After);

    /// <summary>Drops the row at <paramref name="row"/> inside the row at <paramref name="target"/>, as dragging it
    /// onto that row's middle does. Both rows must be showing, and the row must be one the tree lets move.</summary>
    public Task DragIntoAsync(string[] row, string[] target) => DragAsync(row, target, TreeDropPosition.Inside);

    // Follows the tree's script: a selected row brings the other selected rows that can move, a drop below an open
    // row goes above its first child, and a drop on a moving row does nothing. The tree then checks it with CanDrop.
    private Task DragAsync(string[] row, string[] target, TreeDropPosition position)
    {
        var source = Item(row);
        if (!source.HasAttribute("data-draggable"))
        {
            throw new InvalidOperationException($"The row at {string.Join(" > ", row)} can't be dragged: the tree "
                + "needs an OnDrop and mustn't be disabled, and the row must be enabled, have a value and be allowed "
                + "by CanDrag.");
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
        if (position == TreeDropPosition.After && onto.GetAttribute("aria-expanded") == "true" && next is not null
            && Level(next) > Level(onto))
        {
            (onto, position) = (next, TreeDropPosition.Before);
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

    /// <summary>Clicks the row with a cell whose text is exactly this, as the browser reports a click on a plain cell
    /// away from the row's controls: the grid raises <c>OnRowClick</c> and, with <c>RowClickSelects</c>, selects it.
    /// </summary>
    public Task ClickRowAsync(string cell) =>
        Row(cell).TriggerEventAsync("ontbrowclick", new RowClickEventArgs());

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

/// <summary>A <c>TbMenu</c>, found by its trigger's name.</summary>
public sealed class TbMenuDriver
{
    private const string Items = "[role=menuitem]:not([data-part=submenu-trigger]), [role=menuitemcheckbox], "
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

        var item = Find(list, path[^1], Items, "item");
        if (item.GetAttribute("aria-disabled") == "true")
        {
            throw new InvalidOperationException($"The menu item \"{path[^1]}\" is disabled.");
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
    private static IElement Find(IElement list, string name, string selector, string kind)
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
