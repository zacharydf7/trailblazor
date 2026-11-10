namespace Trailblazor.Docs.Examples;

public enum CheckState
{
    NotYet,
    NotApplicable,
    Tested,
}

// How far one component has been checked. Axe needs no column of its own in the data, since AxeTests runs every
// catalog entry. ScreenReaderChecked is the date of the last manual check, which only a person can record.
public sealed record AccessibilityStatus(string Component, CheckState Keyboard, CheckState ForcedColors,
    CheckState ReducedMotion)
{
    public DateOnly? ScreenReaderChecked { get; init; }
}

// One row per catalog entry, in the catalog's order. NotApplicable means: it handles no keys of its own, draws nothing
// that forced colors could lose, or has no motion.
public static class AccessibilityStatusCatalog
{
    private const CheckState Tested = CheckState.Tested;
    private const CheckState NotYet = CheckState.NotYet;
    private const CheckState None = CheckState.NotApplicable;

    public static IReadOnlyList<AccessibilityStatus> All { get; } =
    [
        new("text", None, None, None),
        new("kbd", None, Tested, None),
        new("avatar", None, Tested, None),
        new("timeline", None, Tested, None),
        new("carousel", Tested, Tested, Tested),
        new("virtual-list", Tested, None, None),
        new("image", None, NotYet, None),
        new("divider", None, NotYet, None),
        new("surface", None, NotYet, None),
        new("card", Tested, Tested, None),
        new("icon", None, NotYet, None),
        new("button", Tested, NotYet, NotYet),
        new("icon-button", NotYet, NotYet, NotYet),
        new("button-group", NotYet, NotYet, None),
        new("toggle-group", Tested, Tested, NotYet),
        new("segmented-control", Tested, Tested, None),
        new("theme-picker", NotYet, NotYet, NotYet),
        new("toolbar", Tested, NotYet, None),
        new("insert-divider", Tested, NotYet, NotYet),
        new("stack", None, None, None),
        new("spacer", None, None, None),
        new("grid", None, None, None),
        new("splitter", Tested, Tested, NotYet),
        new("container", None, None, None),
        new("app-shell", Tested, Tested, NotYet),
        new("alert", None, NotYet, None),
        new("banner", None, NotYet, None),
        new("chip", Tested, Tested, None),
        new("list", Tested, NotYet, None),
        new("sortable", Tested, NotYet, None),
        new("badge", None, Tested, None),
        new("progress", None, Tested, Tested),
        new("label", None, NotYet, None),
        new("form", Tested, None, None),
        new("text-field", Tested, NotYet, None),
        new("rich-text-editor", Tested, NotYet, None),
        new("number-field", Tested, NotYet, None),
        new("checkbox", Tested, Tested, None),
        new("checkbox-group", Tested, Tested, None),
        new("switch", NotYet, Tested, NotYet),
        new("slider", Tested, Tested, None),
        new("radio-group", Tested, Tested, None),
        new("rating", Tested, Tested, None),
        new("select", Tested, Tested, None),
        new("autocomplete", Tested, NotYet, None),
        new("tag-input", Tested, NotYet, None),
        new("color-picker", Tested, NotYet, None),
        new("file-upload", NotYet, NotYet, None),
        new("calendar", Tested, Tested, None),
        new("date-picker", Tested, NotYet, None),
        new("date-range-picker", Tested, Tested, None),
        new("listbox", Tested, NotYet, None),
        new("tooltip", Tested, NotYet, None),
        new("menu", Tested, NotYet, None),
        new("dialog", Tested, NotYet, Tested),
        new("drawer", Tested, NotYet, Tested),
        new("focus-scope", Tested, None, None),
        new("popover", Tested, NotYet, None),
        new("popconfirm", Tested, NotYet, None),
        new("overlay", Tested, NotYet, Tested),
        new("tabs", Tested, Tested, NotYet),
        new("accordion", Tested, NotYet, Tested),
        new("tree-view", Tested, Tested, Tested),
        new("data-grid", Tested, NotYet, NotYet),
        new("table", None, NotYet, None),
        new("breadcrumbs", None, None, None),
        new("stepper", Tested, Tested, None),
        new("pagination", Tested, Tested, None),
        new("table-of-contents", NotYet, NotYet, Tested),
        new("link", Tested, Tested, None),
        new("empty-state", None, None, None),
        new("skeleton", None, Tested, Tested),
        new("toast", Tested, NotYet, Tested),
    ];
}
