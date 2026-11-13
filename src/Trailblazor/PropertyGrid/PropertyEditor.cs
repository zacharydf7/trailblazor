using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Trailblazor;

// One property's editor, as TbPropertyGrid and TbModelFields draw it: a field that suits the property's type, labeled
// and described, which calls Change with the value the user sets.
internal sealed record PropertyEditor(Type Type, string Label, string? Description, bool ReadOnly, Func<object?> Get,
    Func<object?, Task> Change)
{
    private const DynamicallyAccessedMemberTypes Component = DynamicallyAccessedMemberTypes.All;

    // The model and property the field's validation messages belong to. A field whose value isn't the property's own
    // type, such as a date picker's nullable date for a DateOnly, can't bind its ValueExpression to the property, so it
    // shows no messages of its own; Required still marks it and checks it's set.
    public object? Model { get; init; }

    public PropertyInfo? Property { get; init; }

    public bool Required { get; init; }

    public string? RequiredMessage { get; init; }

    public string? Placeholder { get; init; }

    public TextFieldType TextType { get; init; }

    public int Lines { get; init; } = 1;

    public decimal? Min { get; init; }

    public decimal? Max { get; init; }

    // Each editor is named here, rather than made from the type at run time, so trimming keeps them.
    public void Add(RenderTreeBuilder builder, object receiver)
    {
        if (Type == typeof(string)) AddText(builder, receiver);
        else if (Type == typeof(bool)) Add<bool>(builder, receiver, typeof(TbSwitch));
        else if (Type == typeof(int)) AddNumber<int>(builder, receiver);
        else if (Type == typeof(int?)) AddNumber<int?>(builder, receiver);
        else if (Type == typeof(long)) AddNumber<long>(builder, receiver);
        else if (Type == typeof(long?)) AddNumber<long?>(builder, receiver);
        else if (Type == typeof(double)) AddNumber<double>(builder, receiver);
        else if (Type == typeof(double?)) AddNumber<double?>(builder, receiver);
        else if (Type == typeof(decimal)) AddNumber<decimal>(builder, receiver);
        else if (Type == typeof(decimal?)) AddNumber<decimal?>(builder, receiver);
        // The pickers take a nullable value for a property of either kind.
        else if (Type == typeof(DateOnly) || Type == typeof(DateOnly?))
        {
            Add<DateOnly?>(builder, receiver, typeof(TbDatePicker<DateOnly>));
        }
        else if (Type == typeof(DateTime) || Type == typeof(DateTime?))
        {
            Add<DateTime?>(builder, receiver, typeof(TbDateTimePicker<DateTime>));
        }
        else if (Type == typeof(TimeOnly) || Type == typeof(TimeOnly?))
        {
            Add<TimeOnly?>(builder, receiver, typeof(TbTimePicker<TimeOnly>));
        }
        else if ((Nullable.GetUnderlyingType(Type) ?? Type) is { IsEnum: true } enumType
            && !enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            AddEnum(builder, receiver);
        }
        else AddReadOnlyText(builder);
    }

    // An emptied editor leaves a property that can't hold null as it was.
    private Task ChangeAsync(object? value) =>
        value is null && Type.IsValueType && Nullable.GetUnderlyingType(Type) is null
            ? Task.CompletedTask : Change(value);

    private void Add<TValue>(RenderTreeBuilder builder, object receiver,
        [DynamicallyAccessedMembers(Component)] Type component, Action<RenderTreeBuilder>? extras = null)
    {
        builder.OpenComponent(0, component);
        builder.AddComponentParameter(1, nameof(TbFieldBase<TValue>.Label), Label);
        builder.AddComponentParameter(2, nameof(TbFieldBase<TValue>.HelperText), Description);
        builder.AddComponentParameter(3, nameof(TbFieldBase<TValue>.ReadOnly), ReadOnly);
        builder.AddComponentParameter(10, nameof(TbFieldBase<TValue>.Value), (TValue?)Get());
        builder.AddComponentParameter(11, nameof(TbFieldBase<TValue>.ValueChanged),
            EventCallback.Factory.Create<TValue>(receiver, value => ChangeAsync(value)));
        if (Model is not null && Property?.PropertyType == typeof(TValue))
        {
            builder.AddComponentParameter(12, nameof(TbFieldBase<TValue>.ValueExpression),
                Expression.Lambda<Func<TValue>>(Expression.Property(Expression.Constant(Model), Property)));
        }
        else if (Required)
        {
            builder.AddComponentParameter(13, nameof(TbFieldBase<TValue>.Required), true);
            builder.AddComponentParameter(14, nameof(TbFieldBase<TValue>.RequiredMessage), RequiredMessage);
        }

        extras?.Invoke(builder);
        builder.CloseComponent();
    }

    private void AddText(RenderTreeBuilder builder, object receiver) =>
        Add<string>(builder, receiver, typeof(TbTextField<string>), field =>
        {
            field.AddComponentParameter(20, nameof(TbTextField<string>.Placeholder), Placeholder);
            field.AddComponentParameter(21, nameof(TbTextField<string>.Type), TextType);
            field.AddComponentParameter(22, nameof(TbTextField<string>.Lines), Lines);
        });

    private void AddNumber<TValue>(RenderTreeBuilder builder, object receiver) =>
        Add<TValue>(builder, receiver, typeof(TbNumberField<TValue>), field =>
        {
            field.AddComponentParameter(20, nameof(TbNumberField<TValue>.Min), Min);
            field.AddComponentParameter(21, nameof(TbNumberField<TValue>.Max), Max);
        });

    // An enum is a select of its values, each shown as its name split into words. A nullable enum can also be cleared.
    private void AddEnum(RenderTreeBuilder builder, object receiver)
    {
        var type = Nullable.GetUnderlyingType(Type) ?? Type;
        builder.OpenComponent<TbSelect<string>>(0);
        builder.AddComponentParameter(1, nameof(TbSelect<string>.Label), Label);
        builder.AddComponentParameter(2, nameof(TbSelect<string>.HelperText), Description);
        builder.AddComponentParameter(3, nameof(TbSelect<string>.ReadOnly), ReadOnly);
        builder.AddComponentParameter(10, nameof(TbSelect<string>.Value), Get()?.ToString());
        builder.AddComponentParameter(11, nameof(TbSelect<string>.ValueChanged), EventCallback.Factory.Create<string?>(
            receiver, name => ChangeAsync(name is null ? null : Enum.Parse(type, name))));
        builder.AddComponentParameter(12, nameof(TbSelect<string>.Clearable), Type != type);
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
        // An enum that can't be null always holds one of its values, so it's never missing.
        builder.AddComponentParameter(14, nameof(TbSelect<string>.Required), Required && Type != type);
        builder.AddComponentParameter(15, nameof(TbSelect<string>.RequiredMessage), RequiredMessage);
        builder.CloseComponent();
    }

    // A type with no editor of its own shows its text, read-only.
    private void AddReadOnlyText(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TbTextField<string>>(0);
        builder.AddComponentParameter(1, nameof(TbTextField<string>.Label), Label);
        builder.AddComponentParameter(2, nameof(TbTextField<string>.HelperText), Description);
        builder.AddComponentParameter(3, nameof(TbTextField<string>.ReadOnly), true);
        builder.AddComponentParameter(10, nameof(TbTextField<string>.Value), Get()?.ToString());
        builder.CloseComponent();
    }
}
