using Microsoft.CodeAnalysis;

namespace Trailblazor.Analyzers;

internal static class Rules
{
    private const string Accessibility = "Accessibility";
    private const string Usage = "Usage";

    public static readonly DiagnosticDescriptor NoAccessibleName = new("TB001", "A control has no accessible name",
        "{0} has no accessible name. Set {1}.", Accessibility, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        "Screen readers announce a control by its name. Without one, it is read as just its role, such as \"button\".");

    public static readonly DiagnosticDescriptor NoDialogTitle = new("TB002", "A dialog has no title",
        "The dialog has no title, which is also its accessible name", Accessibility, DiagnosticSeverity.Warning,
        isEnabledByDefault: true, "A dialog's title is announced when it opens and names it to screen readers.");

    public static readonly DiagnosticDescriptor ValueWithoutValueChanged = new("TB003",
        "Value is set without ValueChanged", "{0} sets Value without ValueChanged, so the parent's next render " +
        "undoes the user's change. {1}.", Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        "An input shows the user's change at once, but the parent still holds the old value and puts it back the " +
        "next time it renders.");

    public static readonly DiagnosticDescriptor NoFieldLabel = new("TB004", "A field has no label",
        "{0} has no label. Set Label, or AriaLabel when the layout has no room for one.", Accessibility,
        DiagnosticSeverity.Warning, isEnabledByDefault: true,
        "A field's label is its accessible name, and it tells every user what to enter.");

    public static readonly DiagnosticDescriptor TypeParameterAsAttribute = new("TB005",
        "An attribute looks like a type parameter", "{0} has no {1} parameter, so {1} doesn't set its type. To set " +
        "the type, write {2}.", Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        "Razor sets a generic component's type only from an attribute with the type parameter's exact name. Any " +
        "other name, such as T, doesn't set the type and is passed to the component like any other attribute.");

    public static readonly DiagnosticDescriptor OutsideItsParent = new("TB006",
        "A component is outside the one it belongs in", "{0} isn't inside a {1}, so it throws as it renders", Usage,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, "Some components work only inside another, such as a " +
        "tab inside its tabs. Anywhere else, the component throws as it renders, which ends a Blazor Server circuit.");

    public static readonly DiagnosticDescriptor ItemTypeMismatch = new("TB007",
        "An item's value isn't the type its list holds", "{0}'s value is {1}, but the {2} it's in holds {3}", Usage,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, "The value of an option, toggle item, radio, checkbox " +
        "item or menu radio item must be the type of the select, list box, toggle group, radio group, checkbox " +
        "group or menu radio group it's in. Razor infers each one's type from its own value, so a value such as 1 " +
        "in a list of strings makes an option of int, which throws as it renders.");

    public static readonly DiagnosticDescriptor SliderNotANumber = new("TB008", "A slider holds something other " +
        "than a number", "{0} holds {1}, but a slider needs a number type", Usage, DiagnosticSeverity.Warning,
        isEnabledByDefault: true, "A slider moves through numbers, so its type must be one, such as int or double. " +
        "Any other type throws as it renders.");

    public static readonly DiagnosticDescriptor TreeSetup = new("TB009", "A tree view's parameters don't go together",
        "TbTreeView {0}", Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, "A tree view takes its rows " +
        "from Items or from TbTreeItems written inside it, finds children through either a ParentKeySelector or a " +
        "ChildrenSelector and LoadChildren, virtualizes only Items, and needs a KeySelector unless its items are " +
        "their own keys. Any other setup throws as it renders.");

    public static readonly DiagnosticDescriptor CodeAsText = new("TB010", "A parameter's text looks like code",
        "{0}'s {1} is the text \"{2}\". To use it as code, write {1}=\"@{2}\".", Usage, DiagnosticSeverity.Warning,
        isEnabledByDefault: true, "Razor passes an attribute written without @ to a string parameter as plain " +
        "text, so Value=\"GetColor()\" shows the words GetColor() instead of calling the method. Only text shaped " +
        "like a call, a member path or a field starting with _ is reported, when it names a value or a method the " +
        "page can see.");

    public static readonly DiagnosticDescriptor KnownListAutocomplete = new("TB011",
        "An autocomplete opens a known list on focus", "{0} opens a known list on focus. A TbSelect with " +
        "Filterable fits better.", Usage, DiagnosticSeverity.Info, isEnabledByDefault: true,
        "An autocomplete is a text field that suggests: it shows no arrow, and a click on a field that already has " +
        "focus doesn't open its list. Given Items and OpenOnFocus with no Search, FreeText, OnTextChanged, " +
        "Columns, Start or MinCharacters, it's used to pick from a list the page already has. A TbSelect with " +
        "Filterable does that with an arrow and a click that opens it, and narrows the list as someone types, just " +
        "as the autocomplete does.");

    public static readonly DiagnosticDescriptor ReferenceKeys = new("TB012",
        "A tree's items are their own keys and compare by reference", "{0}'s items are their own keys and compare " +
        "by reference, so what is open and selected is lost when Items is loaded again. Give it a KeySelector.",
        Usage, DiagnosticSeverity.Info, isEnabledByDefault: true, "A tree keeps what is open, selected and checked " +
        "by key. When a class is its own key and doesn't override both Equals and GetHashCode, data that is " +
        "loaded again brings new instances, which are new keys: no row shows as open, selected or checked anymore, " +
        "and the tab stop goes back to the first row. A tree whose objects live as long as the page is fine as it " +
        "is, so this is a suggestion. A record, a string, and a class that overrides both Equals and GetHashCode " +
        "aren't reported.");

    public static readonly DiagnosticDescriptor GridSetup = new("TB013", "A data grid's parameters don't go together",
        "TbDataGrid {0}", Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, "A data grid takes its rows " +
        "from Items or from a DataProvider, and groups only rows held in Items. A tree grid, one with a " +
        "ChildrenSelector, takes its rows from Items and can't be grouped, split into pages or given OnRowMove. A " +
        "virtualized grid needs a Height or Fill, and can't have a PageSize or a DetailTemplate. Each of these " +
        "setups throws as it renders.");

    public static readonly DiagnosticDescriptor HotkeyKeys = new("TB014", "A hotkey's keys can't be used as written",
        "TbHotkey's keys \"{0}\" {1}", Usage, DiagnosticSeverity.Warning, isEnabledByDefault: true, "A hotkey's " +
        "keys are any of Mod, Ctrl, Alt, Shift and Meta, then one key, joined by plus signs. A key people type, with " +
        "no Mod, Ctrl, Alt or Meta, needs AllowCharacterKey. Any other keys throw as the hotkey renders.");

    public static readonly DiagnosticDescriptor SubmitHandlers = new("TB015",
        "A form has OnSubmit beside OnValidSubmit or OnInvalidSubmit", "TbForm has OnSubmit beside {0}, so it " +
        "throws as it renders. Check the form in OnSubmit with ValidateAsync, or remove OnSubmit.", Usage,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, "OnSubmit is raised in place of the form's own " +
        "check, so a form with it never raises OnValidSubmit or OnInvalidSubmit. A form given both throws as it " +
        "renders, which ends a Blazor Server circuit.");
}
