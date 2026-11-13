using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Trailblazor;

/// <summary>One property a <see cref="TbPropertyGrid{TModel}"/> shows: how to read it, how to change it, and what to
/// call it. <see cref="For{TModel}"/> builds them from a model's properties; build your own for anything else, such
/// as a dictionary of settings.</summary>
public sealed record PropertyGridField
{
    /// <summary>The property's name, which tells it apart in <see cref="TbPropertyGrid{TModel}.OnChange"/>.</summary>
    public required string Name { get; init; }

    /// <summary>The property's type, which picks its editor: text, a number, a switch, a list of an enum's values, a
    /// date, a date and time, or a time. A <see cref="FlagsAttribute"/> enum, whose value can combine several of its
    /// names, and any other type show as read-only text.</summary>
    public required Type Type { get; init; }

    /// <summary>Reads the property's value.</summary>
    public required Func<object?> Get { get; init; }

    /// <summary>Sets the property's value. Leave it null for a property that can only be read.</summary>
    public Action<object?>? Set { get; init; }

    /// <summary>The editor's label. Defaults to <see cref="Name"/> split into words, as
    /// "Trail name" for <c>TrailName</c>.</summary>
    public string? Label { get; init; }

    /// <summary>Text shown under the editor, such as what the property does.</summary>
    public string? Description { get; init; }

    /// <summary>The heading the property is listed under. Properties without one come first.</summary>
    public string? Group { get; init; }

    /// <summary>The fields for <paramref name="model"/>'s public properties, sorted by
    /// <see cref="DisplayAttribute.Order"/>, where a property without one counts as 10000, and in declaration order
    /// among equal orders. <see cref="DisplayAttribute"/>,
    /// <see cref="DisplayNameAttribute"/>, <see cref="DescriptionAttribute"/> and <see cref="CategoryAttribute"/>
    /// name, describe and group them; <see cref="ReadOnlyAttribute"/>, <see cref="EditableAttribute"/> and a missing
    /// public setter or an <c>init</c> one make them read-only; and <see cref="BrowsableAttribute"/> or
    /// <see cref="DisplayAttribute.AutoGenerateField"/> set to false hides them.</summary>
    public static IReadOnlyList<PropertyGridField> For<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TModel>(TModel model)
        where TModel : class
    {
        ArgumentNullException.ThrowIfNull(model);
        return [.. ModelProperties.Of(typeof(TModel)).Select(property => FieldOf(property, model))];
    }

    private static PropertyGridField FieldOf(PropertyInfo property, object model) => new()
    {
        Name = property.Name,
        Type = property.PropertyType,
        Get = () => property.GetValue(model),
        Set = ModelProperties.IsEditable(property) ? value => property.SetValue(model, value) : null,
        Label = ModelProperties.LabelOf(property),
        Description = ModelProperties.DescriptionOf(property),
        Group = ModelProperties.GroupOf(property),
    };

    internal string LabelText => Label ?? NameWords.Of(Name);
}
