namespace Trailblazor;

/// <summary>The words a <see cref="TbFileUpload"/> shows and says to screen readers.</summary>
public sealed record FileUploadLabels
{
    /// <summary>The drop zone's text. Leave it null for the app's text for <see
    /// cref="TbStrings.FileUploadZone"/>.</summary>
    public string? Zone { get; init; }

    /// <summary>The button's text when there is no drop zone, and the input's name when the field has no label. Leave
    /// it null for the app's text for <see cref="TbStrings.FileUploadChoose"/>.</summary>
    public string? Choose { get; init; }

    /// <summary>Names a file's remove button, with <c>{0}</c> for the file's name. Leave it null for the app's text for
    /// <see cref="TbStrings.FileUploadRemove"/>.</summary>
    public string? Remove { get; init; }

    /// <summary>Names the button that removes every file. Leave it null for the app's text for <see
    /// cref="TbStrings.FileUploadClear"/>.</summary>
    public string? Clear { get; init; }

    /// <summary>Said after a choice, with <c>{0}</c> for the number of files. Leave it null for the app's text for <see
    /// cref="TbStrings.FileUploadChosen"/>. </summary>
    public string? Chosen { get; init; }

    /// <summary>Said after a choice of one file, with <c>{0}</c>, if used, for the number. Leave it null for the app's
    /// text for <see cref="TbStrings.FileUploadChosenOne"/>.</summary>
    public string? ChosenOne { get; init; }

    /// <summary>The message for a file of a type the field doesn't take, with <c>{0}</c> for its name and <c>{1}</c>
    /// for the types it takes. Leave it null for the app's text for <see
    /// cref="TbStrings.FileUploadWrongType"/>.</summary>
    public string? WrongType { get; init; }

    /// <summary>The message for a file over the size limit, with <c>{0}</c> for its name and <c>{1}</c> for the limit.
    /// Leave it null for the app's text for <see cref="TbStrings.FileUploadTooLarge"/>.</summary>
    public string? TooLarge { get; init; }

    /// <summary>The message when more files are chosen than allowed, with <c>{0}</c> for the most allowed. Leave it
    /// null for the app's text for <see cref="TbStrings.FileUploadTooMany"/>.</summary>
    public string? TooMany { get; init; }

    /// <summary>The message when more than one file is chosen where one is allowed, with <c>{0}</c>, if used, for the
    /// limit. Leave it null for the app's text for <see cref="TbStrings.FileUploadTooManyOne"/>.</summary>
    public string? TooManyOne { get; init; }
}
