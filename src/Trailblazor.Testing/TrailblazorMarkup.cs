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

    /// <summary>The <c>TbSelect</c> or <c>TbMultiSelect</c> with this label.</summary>
    public TbSelectDriver Select(string label) => new(() => Field(label, "[role=combobox]", "select"), _findAll);

    /// <summary>The <c>TbListBox</c> or <c>TbMultiListBox</c> with this label.</summary>
    public TbListBoxDriver ListBox(string label) => new(() => Field(label, "[role=listbox]", "select"));

    /// <summary>The <c>TbNumberField</c> with this label.</summary>
    public TbNumberFieldDriver NumberField(string label) =>
        new(() => Field(label, "input[type=number]", "input[role=spinbutton]"));

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

    // A field's label names its control by for or aria-labelledby; a label's text ends with a required marker.
    private IElement Field(string label, params string[] controls)
    {
        var labels = _findAll("label[data-part=label]").Where(element => LabelText(element) == label).ToList();
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

    /// <summary>The options' names, in order, leaving out a static select's placeholder and the value it keeps when no
    /// option has it. An option with content of its own is named by its <c>Text</c>.</summary>
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
    /// one that posts with its form, so there is nothing to choose in bUnit.</summary>
    public Task ChooseAsync(params string[] options) => Choices.ChooseAsync(() =>
    {
        var control = _find();
        Choices.ThrowIfStaticOrDisabled(control);
        return (OptionElements(),
            _findAll($"[id='{control.GetAttribute("aria-controls")}'][aria-multiselectable=true]").Count > 0);
    }, options);

    private IEnumerable<IElement> OptionElements()
    {
        var control = _find();
        return control.TagName == "SELECT" ? control.QuerySelectorAll("option:not([value='']):not([data-unmatched])")
            : _findAll($"[id='{control.GetAttribute("aria-controls")}'] [role=option]");
    }
}

/// <summary>A <c>TbListBox</c> or <c>TbMultiListBox</c>, found by its label.</summary>
public sealed class TbListBoxDriver
{
    private readonly Func<IElement> _find;

    internal TbListBoxDriver(Func<IElement> find) => _find = find;

    /// <summary>The options' names, in order. An option with content of its own is named by its <c>Text</c>.
    /// </summary>
    public IReadOnlyList<string> Options => _find().QuerySelectorAll("[role=option], option")
        .Select(Choices.NameOf).ToList();

    /// <summary>The names of the chosen options, in order.</summary>
    public IReadOnlyList<string> Chosen => _find()
        .QuerySelectorAll("[role=option][aria-selected=true], option[selected]")
        .Select(Choices.NameOf).ToList();

    /// <summary>Chooses the options with these names, as clicking them does: the one option of a list box, or each one
    /// not yet chosen in a multiple list box, keeping what is already chosen. On a static page there is nothing to
    /// choose in bUnit, as for a select.</summary>
    public Task ChooseAsync(params string[] options) => Choices.ChooseAsync(() =>
    {
        var list = _find();
        Choices.ThrowIfStaticOrDisabled(list);
        return (list.QuerySelectorAll("[role=option]"), list.GetAttribute("aria-multiselectable") == "true");
    }, options);
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
    public Task SelectAsync(string tab) =>
        (_find().QuerySelectorAll("[role=tab]").SingleOrDefault(element => element.TextContent.Trim() == tab)
            ?? throw new InvalidOperationException($"No tab reads \"{tab}\"."))
        .ClickAsync(new MouseEventArgs { Detail = 1 });
}
