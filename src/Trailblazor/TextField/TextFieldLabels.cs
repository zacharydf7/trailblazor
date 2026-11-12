namespace Trailblazor;

/// <summary>The words a <see cref="TbTextField{TValue}"/> shows and says to screen readers.</summary>
public sealed record TextFieldLabels
{
    /// <summary>Names the clear button, which the field's label describes. Leave it null for the app's text for
    /// <see cref="TbStrings.TextFieldClear"/>.</summary>
    public string? Clear { get; init; }

    /// <summary>The message for a masked field that holds only part of its pattern, with <c>{0}</c> for the field's
    /// label. Leave it null for the app's text for <see cref="TbStrings.TextFieldMaskIncomplete"/>.</summary>
    public string? MaskIncomplete { get; init; }
}
