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

    /// <summary>A file's size under a kilobyte, with <c>{0}</c> for the number. Leave it null for the app's text for
    /// <see cref="TbStrings.FileUploadBytes"/>.</summary>
    public string? Bytes { get; init; }

    /// <summary>A file's size in kilobytes, with <c>{0}</c> for the number. Leave it null for the app's text for
    /// <see cref="TbStrings.FileUploadKilobytes"/>.</summary>
    public string? Kilobytes { get; init; }

    /// <summary>A file's size in megabytes, with <c>{0}</c> for the number. Leave it null for the app's text for
    /// <see cref="TbStrings.FileUploadMegabytes"/>.</summary>
    public string? Megabytes { get; init; }

    /// <summary>A file's size in gigabytes, with <c>{0}</c> for the number. Leave it null for the app's text for
    /// <see cref="TbStrings.FileUploadGigabytes"/>.</summary>
    public string? Gigabytes { get; init; }

    /// <summary>The button that sends the chosen files when <see cref="TbFileUpload.AutoUpload"/> is off. Leave it null
    /// for the app's text for <see cref="TbStrings.FileUploadUpload"/>.</summary>
    public string? Upload { get; init; }

    /// <summary>A file's progress bar while it is sent, with <c>{0}</c> for its name. Leave it null for the app's text
    /// for <see cref="TbStrings.FileUploadUploading"/>.</summary>
    public string? Uploading { get; init; }

    /// <summary>The button that stops a file's upload, with <c>{0}</c> for its name. Leave it null for the app's text
    /// for <see cref="TbStrings.FileUploadCancel"/>.</summary>
    public string? Cancel { get; init; }

    /// <summary>The button that sends a failed or canceled file again, with <c>{0}</c> for its name. Leave it null
    /// for the app's text for <see cref="TbStrings.FileUploadRetry"/>.</summary>
    public string? Retry { get; init; }

    /// <summary>What a sent file shows. Leave it null for the app's text for <see cref="TbStrings.FileUploadUploaded"/>.
    /// </summary>
    public string? Uploaded { get; init; }

    /// <summary>What a file shows when its upload failed. Leave it null for the app's text for
    /// <see cref="TbStrings.FileUploadFailed"/>.</summary>
    public string? Failed { get; init; }

    /// <summary>What a file shows when its upload was canceled. Leave it null for the app's text for
    /// <see cref="TbStrings.FileUploadCanceled"/>.</summary>
    public string? Canceled { get; init; }

    /// <summary>What screen readers hear when a file is sent, with <c>{0}</c> for its name. Leave it null for the
    /// app's text for <see cref="TbStrings.FileUploadFileUploaded"/>.</summary>
    public string? FileUploaded { get; init; }

    /// <summary>What screen readers hear when a file's upload fails, with <c>{0}</c> for its name. Leave it null for
    /// the app's text for <see cref="TbStrings.FileUploadFileFailed"/>.</summary>
    public string? FileFailed { get; init; }
}
