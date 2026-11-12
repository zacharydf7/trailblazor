namespace Trailblazor;

/// <summary>The words a <see cref="TbCodeField"/> shows and says to screen readers.</summary>
public sealed record CodeFieldLabels
{
    /// <summary>The message for a code typed only in part, with <c>{0}</c> for the field's label and <c>{1}</c> for
    /// how many characters the code has. Leave it null for the app's text for
    /// <see cref="TbStrings.CodeFieldIncomplete"/>.</summary>
    public string? Incomplete { get; init; }

    /// <summary>Describes the field by its length, which only its boxes show, with <c>{0}</c> for how many characters
    /// the code has. Leave it null for the app's text for <see cref="TbStrings.CodeFieldDigits"/> or
    /// <see cref="TbStrings.CodeFieldCharacters"/>.</summary>
    public string? Length { get; init; }
}
