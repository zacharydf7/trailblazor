using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Trailblazor;

/// <summary>A field a <see cref="TbQueryBuilder"/> lets the user write conditions on. <see cref="For{T}"/> builds them
/// from a type's properties.</summary>
/// <param name="Key">What a condition's <see cref="GridFilter.Column"/> holds: the name of the property
/// <see cref="QueryGroup.ToExpression{T}"/> compares.</param>
/// <param name="Label">The field's name in the list of fields.</param>
/// <param name="Kind">What the field holds.</param>
public sealed record QueryField(string Key, string Label, QueryFieldKind Kind)
{
    /// <summary>The values a <see cref="QueryFieldKind.Choice"/> field offers, in order.</summary>
    public IReadOnlyList<QueryChoice> Choices { get; init; } = [];

    /// <summary>A field for each of <typeparamref name="T"/>'s public properties that holds text, a number, a date,
    /// yes or no, or an enum's values, named by its <see cref="DisplayAttribute.Name"/> or else its name split into
    /// words. An enum's choices are its values, named the same way.</summary>
    public static IReadOnlyList<QueryField> For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() =>
        [.. typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .Select(property => (Property: property, Kind: KindOf(property.PropertyType)))
            .Where(field => field.Kind is not null)
            .Select(field => new QueryField(field.Property.Name, LabelOf(field.Property), field.Kind!.Value)
            {
                Choices = field.Kind == QueryFieldKind.Choice ? ChoicesOf(field.Property.PropertyType) : [],
            })];

    private static string LabelOf(PropertyInfo property) =>
        property.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? NameWords.Of(property.Name);

    private static QueryFieldKind? KindOf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string) ? QueryFieldKind.Text
            : type == typeof(bool) ? QueryFieldKind.YesNo
            : type.IsEnum ? QueryFieldKind.Choice
            : type == typeof(DateOnly) || type == typeof(DateTime) ? QueryFieldKind.Date
            : NumberTypes.IsNumber(type) ? QueryFieldKind.Number
            : null;
    }

    private static IReadOnlyList<QueryChoice> ChoicesOf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return [.. Enum.GetNames(type).Select(name => new QueryChoice(Enum.Parse(type, name), NameWords.Of(name)))];
    }
}

/// <summary>One value a <see cref="QueryFieldKind.Choice"/> field offers.</summary>
/// <param name="Value">The value a condition compares with, such as an enum value.</param>
/// <param name="Label">The value's name in the list.</param>
public sealed record QueryChoice(object Value, string Label);
