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
    private const DynamicallyAccessedMemberTypes Component = DynamicallyAccessedMemberTypes.All;
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

    // An emptied editor leaves a property that can't hold null as it was.
    private async Task ChangeAsync<TValue>(PropertyGridField field, TValue value)
    {
        if (value is null && field.Type.IsValueType && Nullable.GetUnderlyingType(field.Type) is null)
        {
            return;
        }

        field.Set?.Invoke(value);
        Telemetry.Used(TelemetryName, "change");
        await OnChange.InvokeAsync(field);
    }

    // The editor for the field's type. Each is named here, rather than made from the type at run time, so trimming
    // keeps them.
    private void AddEditor(RenderTreeBuilder builder, PropertyGridField field)
    {
        var type = field.Type;
        if (type == typeof(string)) Add<string>(builder, typeof(TbTextField<string>), field);
        else if (type == typeof(bool)) Add<bool>(builder, typeof(TbSwitch), field);
        else if (type == typeof(int)) Add<int>(builder, typeof(TbNumberField<int>), field);
        else if (type == typeof(int?)) Add<int?>(builder, typeof(TbNumberField<int?>), field);
        else if (type == typeof(long)) Add<long>(builder, typeof(TbNumberField<long>), field);
        else if (type == typeof(long?)) Add<long?>(builder, typeof(TbNumberField<long?>), field);
        else if (type == typeof(double)) Add<double>(builder, typeof(TbNumberField<double>), field);
        else if (type == typeof(double?)) Add<double?>(builder, typeof(TbNumberField<double?>), field);
        else if (type == typeof(decimal)) Add<decimal>(builder, typeof(TbNumberField<decimal>), field);
        else if (type == typeof(decimal?)) Add<decimal?>(builder, typeof(TbNumberField<decimal?>), field);
        // The pickers take a nullable value for a property of either kind.
        else if (type == typeof(DateOnly) || type == typeof(DateOnly?))
        {
            Add<DateOnly?>(builder, typeof(TbDatePicker<DateOnly>), field);
        }
        else if (type == typeof(DateTime) || type == typeof(DateTime?))
        {
            Add<DateTime?>(builder, typeof(TbDateTimePicker<DateTime>), field);
        }
        else if (type == typeof(TimeOnly) || type == typeof(TimeOnly?))
        {
            Add<TimeOnly?>(builder, typeof(TbTimePicker<TimeOnly>), field);
        }
        else if ((Nullable.GetUnderlyingType(type) ?? type) is { IsEnum: true } enumType
            && !enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            AddEnum(builder, field);
        }
        else AddText(builder, field);
    }

    private void Add<TValue>(RenderTreeBuilder builder, [DynamicallyAccessedMembers(Component)] Type component,
        PropertyGridField field)
    {
        builder.OpenComponent(0, component);
        builder.AddComponentParameter(1, nameof(TbFieldBase<TValue>.Label), field.LabelText);
        builder.AddComponentParameter(2, nameof(TbFieldBase<TValue>.HelperText), field.Description);
        builder.AddComponentParameter(3, nameof(TbFieldBase<TValue>.ReadOnly), ReadOnly || field.Set is null);
        builder.AddComponentParameter(10, nameof(TbFieldBase<TValue>.Value), (TValue?)field.Get());
        builder.AddComponentParameter(11, nameof(TbFieldBase<TValue>.ValueChanged),
            EventCallback.Factory.Create<TValue>(this, value => ChangeAsync(field, value)));
        builder.CloseComponent();
    }

    // An enum is a select of its values, each shown as its name split into words. A nullable enum can also be cleared.
    private void AddEnum(RenderTreeBuilder builder, PropertyGridField field)
    {
        var type = Nullable.GetUnderlyingType(field.Type) ?? field.Type;
        builder.OpenComponent<TbSelect<string>>(0);
        builder.AddComponentParameter(1, nameof(TbSelect<string>.Label), field.LabelText);
        builder.AddComponentParameter(2, nameof(TbSelect<string>.HelperText), field.Description);
        builder.AddComponentParameter(3, nameof(TbSelect<string>.ReadOnly), ReadOnly || field.Set is null);
        builder.AddComponentParameter(10, nameof(TbSelect<string>.Value), field.Get()?.ToString());
        builder.AddComponentParameter(11, nameof(TbSelect<string>.ValueChanged), EventCallback.Factory.Create<string?>(
            this, name => ChangeAsync<object?>(field, name is null ? null : Enum.Parse(type, name))));
        builder.AddComponentParameter(12, nameof(TbSelect<string>.Clearable), field.Type != type);
        builder.AddComponentParameter(13, nameof(TbSelect<string>.ChildContent), (RenderFragment)(options =>
        {
            foreach (var name in Enum.GetNames(type))
            {
                options.OpenComponent<TbOption<string>>(0);
                options.SetKey(name);
                options.AddComponentParameter(1, nameof(TbOption<string>.Value), name);
                options.AddComponentParameter(2, nameof(TbOption<string>.Text), NameWords.Of(name));
                options.CloseComponent();
            }
        }));
        builder.CloseComponent();
    }

    // A type with no editor of its own shows its text, read-only.
    private void AddText(RenderTreeBuilder builder, PropertyGridField field)
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), field.LabelText);
        builder.AddComponentParameter(2, nameof(TbTextField<string>.HelperText), field.Description);
        builder.AddComponentParameter(3, nameof(TbTextField<string>.ReadOnly), true);
        builder.AddComponentParameter(10, nameof(TbTextField<string>.Value), field.Get()?.ToString());
        builder.CloseComponent();
    }
}
