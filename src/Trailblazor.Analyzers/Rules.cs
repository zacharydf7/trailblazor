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
        DiagnosticSeverity.Warning, isEnabledByDefault: true, "An option or toggle item's value must be the type of " +
        "the select, list box or toggle group it's in. Razor infers each one's type from its own value, so a value " +
        "such as 1 in a list of strings makes an option of int, which throws as it renders.");

    public static readonly DiagnosticDescriptor SliderNotANumber = new("TB008", "A slider holds something other " +
        "than a number", "{0} holds {1}, but a slider needs a number type", Usage, DiagnosticSeverity.Warning,
        isEnabledByDefault: true, "A slider moves through numbers, so its type must be one, such as int or double. " +
        "Any other type throws as it renders.");
}
