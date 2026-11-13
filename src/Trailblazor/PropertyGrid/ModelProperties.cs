using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Trailblazor;

// How TbPropertyGrid and TbModelFields read a model's properties: which show, in what order, under what name, group
// and description, and which can be changed.
internal static class ModelProperties
{
    // ASP.NET Core's default Order for a property without one, so a small Order puts a property before the rest.
    internal const int UnsetOrder = 10000;

    internal static IEnumerable<PropertyInfo> Of(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0
                && property.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false
                && property.GetCustomAttribute<DisplayAttribute>()?.GetAutoGenerateField() != false)
            .OrderBy(OrderOf);

    internal static int OrderOf(PropertyInfo property) =>
        property.GetCustomAttribute<DisplayAttribute>()?.GetOrder() ?? UnsetOrder;

    // An init accessor sets a property only as its object is made, so it doesn't make the property editable.
    internal static bool IsEditable(PropertyInfo property) => property.SetMethod is { IsPublic: true } setter
        && !setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit))
        && property.GetCustomAttribute<ReadOnlyAttribute>()?.IsReadOnly != true
        && property.GetCustomAttribute<EditableAttribute>()?.AllowEdit != false;

    internal static string? LabelOf(PropertyInfo property) =>
        property.GetCustomAttribute<DisplayAttribute>()?.GetName()
        ?? property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;

    internal static string? DescriptionOf(PropertyInfo property) =>
        property.GetCustomAttribute<DisplayAttribute>()?.GetDescription()
        ?? property.GetCustomAttribute<DescriptionAttribute>()?.Description;

    internal static string? GroupOf(PropertyInfo property) =>
        property.GetCustomAttribute<DisplayAttribute>()?.GetGroupName()
        ?? property.GetCustomAttribute<CategoryAttribute>()?.Category;
}
