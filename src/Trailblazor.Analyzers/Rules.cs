using Microsoft.CodeAnalysis;

namespace Trailblazor.Analyzers;

internal static class Rules
{
    private const string Accessibility = "Accessibility";

    public static readonly DiagnosticDescriptor NoAccessibleName = new("TB001", "A control has no accessible name",
        "{0} has no accessible name. Set {1}.", Accessibility, DiagnosticSeverity.Warning, isEnabledByDefault: true,
        "Screen readers announce a control by its name. Without one, it is read as just its role, such as \"button\".");

    public static readonly DiagnosticDescriptor NoDialogTitle = new("TB002", "A dialog has no title",
        "The dialog has no title, which is also its accessible name", Accessibility, DiagnosticSeverity.Warning,
        isEnabledByDefault: true, "A dialog's title is announced when it opens and names it to screen readers.");

    public static readonly DiagnosticDescriptor ValueWithoutValueChanged = new("TB003",
        "Value is set without ValueChanged", "{0} sets Value without ValueChanged, so the parent's next render " +
        "undoes the user's change. {1}.", "Usage", DiagnosticSeverity.Warning, isEnabledByDefault: true,
        "An input shows the user's change at once, but the parent still holds the old value and puts it back the " +
        "next time it renders.");

    public static readonly DiagnosticDescriptor NoFieldLabel = new("TB004", "A field has no label",
        "{0} has no label. Set Label, or AriaLabel when the layout has no room for one.", Accessibility,
        DiagnosticSeverity.Warning, isEnabledByDefault: true,
        "A field's label is its accessible name, and it tells every user what to enter.");
}
