namespace Trailblazor;

/// <summary>Names a control with a <c>TelemetryName</c> that the app has, so the
/// <c>trailblazor.control.catalog</c> gauge can report it even before it is shown. Trailblazor's source generator
/// writes one for each literal <c>TelemetryName</c> in the app's markup; write one by hand for a name set from code or
/// bound to a value.</summary>
/// <param name="name">The control's <c>TelemetryName</c>.</param>
/// <param name="component">The component it is on, as the markup names it: <c>"TbButton"</c>, or an app's own wrapper
/// such as <c>"AppButton"</c>.</param>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class TelemetryCatalogEntryAttribute(string name, string component) : Attribute
{
    /// <summary>The control's <c>TelemetryName</c>.</summary>
    public string Name { get; } = name;

    /// <summary>The component it is on, as the markup names it, such as <c>"TbButton"</c>.</summary>
    public string Component { get; } = component;
}
