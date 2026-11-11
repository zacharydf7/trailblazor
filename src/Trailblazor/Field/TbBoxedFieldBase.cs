using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>A form field whose input sits in a box, such as a text field or a select, with the box's look and size.
/// </summary>
public abstract class TbBoxedFieldBase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
    : TbFieldBase<TValue>
{
    /// <summary>How the box is drawn. Defaults to <see cref="FieldVariant.Outlined"/>.</summary>
    [Parameter]
    public FieldVariant Variant { get; set; } = FieldVariant.Outlined;

    /// <summary>The height of the box and the size of its text. Defaults to <see cref="Trailblazor.Size.Medium"/>.
    /// </summary>
    [Parameter]
    public Size Size { get; set; } = Size.Medium;

    private protected override string FieldModifiers =>
        ClassNames.Join(ClassNames.Modifier("tb-field", Variant), ClassNames.Modifier("tb-field", Size));
}
