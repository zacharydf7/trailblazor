using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

/// <summary>A list of an object's properties, each with an editor that suits its type, grouped under headings: by
/// reflection over <see cref="Model"/>, or from <see cref="Fields"/> for anything else.</summary>
/// <typeparam name="TModel">The type of the object whose properties are edited.</typeparam>
public sealed partial class TbPropertyGrid<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel> : TbComponentBase
    where TModel : class
{
    private ControlTelemetry? _telemetry;
    private IReadOnlyList<PropertyGridField> _fields = [];

    /// <summary>The object whose public properties are listed, as <see cref="PropertyGridField.For{T}"/> reads them.
    /// Required unless <see cref="Fields"/> lists them.</summary>
    [Parameter]
    public TModel? Model { get; set; }

    /// <summary>The properties to list in place of <see cref="Model"/>'s, such as the keys of a dictionary, or a
    /// model's fields with some changed.</summary>
    [Parameter]
    public IReadOnlyList<PropertyGridField>? Fields { get; set; }

    /// <summary>The grid's accessible name, such as "Trail properties". Required.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>Raised after the user changes a property, with the field that changed. The new value is already set.
    /// </summary>
    [Parameter]
    public EventCallback<PropertyGridField> OnChange { get; set; }

    /// <summary>Makes every property read-only.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>A label the grid's use is counted under, such as <c>"trail-settings"</c>, through the
    /// <c>Trailblazor</c> meter: shown when it first renders, and used with the action <c>change</c> each time the
    /// user changes a property. Which property and its value are never recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    // The properties without a group come first, then each group in the order its first property comes.
    private IEnumerable<IGrouping<string?, PropertyGridField>> Groups =>
        _fields.GroupBy(property => property.Group).OrderBy(group => group.Key is null ? 0 : 1);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException($"{nameof(TbPropertyGrid<TModel>)} needs a {nameof(Label)}, which "
                + "names it for screen readers.");
        }

        _fields = Fields ?? (Model is null
            ? throw new InvalidOperationException($"{nameof(TbPropertyGrid<TModel>)} needs a {nameof(Model)} or "
                + $"{nameof(Fields)}.")
            : PropertyGridField.For(Model));
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    private async Task ChangeAsync(PropertyGridField field, object? value)
    {
        field.Set?.Invoke(value);
        Telemetry.Used(TelemetryName, "change");
        await OnChange.InvokeAsync(field);
    }

    private void AddEditor(RenderTreeBuilder builder, PropertyGridField field) =>
        new PropertyEditor(field.Type, field.LabelText, field.Description, ReadOnly || field.Set is null, field.Get,
            value => ChangeAsync(field, value)).Add(builder, this);
}
