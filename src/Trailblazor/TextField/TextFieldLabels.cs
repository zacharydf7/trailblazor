namespace Trailblazor;

/// <summary>The words a <see cref="TbTextField{TValue}"/> shows and says to screen readers.</summary>
public sealed record TextFieldLabels
{
    /// <summary>Names the clear button, which the field's label describes. Leave it null for the app's text for
    /// <see cref="TbStrings.TextFieldClear"/>.</summary>
    public string? Clear { get; init; }

    /// <summary>Names the show-password button, which the field's label describes. Leave it null for the app's text
    /// for <see cref="TbStrings.TextFieldShowPassword"/>.</summary>
    public string? ShowPassword { get; init; }

    /// <summary>The message for a masked field that holds only part of its pattern, with <c>{0}</c> for the field's
    /// label. Leave it null for the app's text for <see cref="TbStrings.TextFieldMaskIncomplete"/>.</summary>
    public string? MaskIncomplete { get; init; }

    /// <summary>The message for a value longer than <c>MaxLength</c>, with <c>{0}</c> for the field's label and
    /// <c>{1}</c> for the limit. Leave it null for the app's text for <see cref="TbStrings.TextFieldTooLong"/>.
    /// </summary>
    public string? TooLong { get; init; }

    /// <summary>What screen readers hear for the counter without a limit, with <c>{0}</c> for the count. Leave it null
    /// for the app's text for <see cref="TbStrings.TextFieldCount"/>.</summary>
    public string? Count { get; init; }

    /// <summary>What screen readers hear for the counter with a limit, with <c>{0}</c> for the count and <c>{1}</c> for
    /// the limit. Leave it null for the app's text for <see cref="TbStrings.TextFieldCountOfLimit"/>.</summary>
    public string? CountOfLimit { get; init; }

    /// <summary>What's announced as the text nears the limit, with <c>{0}</c> for the characters left. Leave it null
    /// for the app's text for <see cref="TbStrings.TextFieldCharactersLeft"/>.</summary>
    public string? CharactersLeft { get; init; }
}
