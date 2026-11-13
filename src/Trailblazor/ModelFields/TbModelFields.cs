using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Trailblazor;

/// <summary>A form's fields, built from <see cref="Model"/>'s public properties: each with an editor that suits its
/// type, named, described, ordered and grouped by its data annotations, and checked by them when it sits in a
/// <see cref="TbForm"/> with <see cref="TbForm.DataAnnotations"/>. A <see cref="TbModelField"/> inside it changes any
/// one field's label, place, width or editor, or hides it.</summary>
/// <typeparam name="TModel">The type of the object the fields edit.</typeparam>
public sealed partial class TbModelFields<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel> : TbComponentBase
    where TModel : class
{
    private readonly ModelFieldOverrides _overrides;
    private ControlTelemetry? _telemetry;

    /// <summary>Creates the fields.</summary>
    public TbModelFields() => _overrides = new(StateHasChanged);

    /// <summary>The object the fields edit. Each public property shows as a field, unless
    /// <see cref="System.ComponentModel.BrowsableAttribute"/> or <see cref="DisplayAttribute.AutoGenerateField"/>
    /// hides it. Required.</summary>
    [Parameter, EditorRequired]
    public TModel? Model { get; set; }

    /// <summary>How many columns the fields are laid out in; a <see cref="TbModelField.Span"/> widens one field across
    /// several. The fields stack in one column when there's no room for more. Defaults to 1.</summary>
    [Parameter]
    public int Columns { get; set; } = 1;

    /// <summary><see cref="TbModelField"/> elements that change how single fields show.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised after the user changes one of the fields, with its property's name. The new value is already
    /// set. A field from a <see cref="TbModelField.Editor"/> raises its own events instead.</summary>
    [Parameter]
    public EventCallback<string> OnChange { get; set; }

    /// <summary>A label the fields' use is counted under, such as <c>"new-trail"</c>, through the
    /// <c>Trailblazor</c> meter: shown when they first render, and used with the action <c>change</c> each time the
    /// user changes one of them. A field from a <see cref="TbModelField.Editor"/> isn't counted here. Which field and
    /// its value are never recorded.</summary>
    [Parameter]
    public string? TelemetryName { get; set; }

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    private ControlTelemetry Telemetry => _telemetry ??= new(Services, GetType());

    private string RootStyle => ClassNames.Join(
        FormattableString.Invariant($"--tb-model-fields-columns: {Columns};"), Style) ?? "";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Model is null)
        {
            throw new InvalidOperationException($"{nameof(TbModelFields<TModel>)} needs a {nameof(Model)}.");
        }

        if (Columns < 1)
        {
            throw new InvalidOperationException($"{nameof(TbModelFields<TModel>)} needs at least one column.");
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) => Telemetry.Rendered(TelemetryName);

    // The fields in the order they show, grouped: the ones without a group first, then each group in the order its
    // first field comes.
    private IEnumerable<IGrouping<string?, ModelFieldPlace>> Groups()
    {
        var properties = ModelProperties.Of(typeof(TModel)).ToList();
        if (_overrides.Names.FirstOrDefault(name => properties.All(property => property.Name != name)) is { } unknown)
        {
            throw new InvalidOperationException($"A {nameof(TbModelField)} names {unknown}, but "
                + $"{typeof(TModel).Name} has no public property by that name, or hides it with [Browsable(false)] or "
                + "[Display(AutoGenerateField = false)].");
        }

        return properties
            .Select(property => new ModelFieldPlace(property, _overrides.For(property.Name)))
            .Where(place => place.Override?.Hidden != true)
            .OrderBy(place => place.Override?.Order ?? ModelProperties.OrderOf(place.Property))
            .GroupBy(place => ModelProperties.GroupOf(place.Property))
            .OrderBy(group => group.Key is null ? 0 : 1);
    }

    private string SpanStyle(ModelFieldPlace place) =>
        FormattableString.Invariant($"--tb-model-fields-span: {Math.Clamp(place.Override?.Span ?? 1, 1, Columns)};");

    private RenderFragment Editor(ModelFieldPlace place)
    {
        var (property, field) = (place.Property, place.Override);
        var label = field?.Label ?? ModelProperties.LabelOf(property) ?? NameWords.Of(property.Name);
        var description = field?.HelperText ?? ModelProperties.DescriptionOf(property);
        var readOnly = field?.ReadOnly == true || !ModelProperties.IsEditable(property);
        if (field?.Editor is { } editor)
        {
            return editor(new ModelFieldContext(property.Name, label, description, readOnly));
        }

        // [EmailAddress], [Phone] and [Url] are DataType attributes too, so a property can carry several.
        var dataTypes = property.GetCustomAttributes<DataTypeAttribute>().Select(found => found.DataType).ToList();
        var range = property.GetCustomAttribute<RangeAttribute>();
        var required = property.GetCustomAttribute<RequiredAttribute>();
        var shown = new PropertyEditor(property.PropertyType, label, description, readOnly,
            () => property.GetValue(Model), value => ChangeAsync(property, value))
        {
            Model = Model,
            Property = property,
            Placeholder = property.GetCustomAttribute<DisplayAttribute>()?.GetPrompt(),
            Required = required is not null,
            RequiredMessage = required is { ErrorMessage: not null } or { ErrorMessageResourceType: not null }
                ? required.FormatErrorMessage(label) : null,
            TextType = dataTypes.Select(TextTypeOf).FirstOrDefault(type => type != TextFieldType.Text),
            Lines = dataTypes.Contains(DataType.MultilineText) ? 3 : 1,
            Min = Limit(range?.Minimum),
            Max = Limit(range?.Maximum),
        };
        return builder => shown.Add(builder, this);
    }

    private async Task ChangeAsync(PropertyInfo property, object? value)
    {
        property.SetValue(Model, value);
        // A field that can't bind to its property, such as an enum's list, reports its own value as changed, so the
        // property's messages are checked again here.
        EditContext?.NotifyFieldChanged(new FieldIdentifier(Model!, property.Name));
        Telemetry.Used(TelemetryName, "change");
        await OnChange.InvokeAsync(property.Name);
    }

    private static TextFieldType TextTypeOf(DataType dataType) => dataType switch
    {
        DataType.Password => TextFieldType.Password,
        DataType.EmailAddress => TextFieldType.Email,
        DataType.PhoneNumber => TextFieldType.Tel,
        DataType.Url => TextFieldType.Url,
        _ => TextFieldType.Text,
    };

    // A RangeAttribute's limit, as a number field takes it; a limit outside decimal's range, such as double.MaxValue,
    // sets none.
    private static decimal? Limit(object? value) => value switch
    {
        int number => number,
        double number when Math.Abs(number) < 7.9e28 => (decimal)number,
        string text when decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            => number,
        _ => null,
    };
}

// One property's field and the TbModelField that changes it, if any.
internal sealed record ModelFieldPlace(PropertyInfo Property, TbModelField? Override);
