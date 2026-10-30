namespace Trailblazor;

/// <summary>The words a <see cref="TbFileUpload"/> shows and says to screen readers.</summary>
public sealed record FileUploadLabels
{
    /// <summary>The drop zone's text. Defaults to "Drop files here, or click to choose them.".</summary>
    public string Zone { get; init; } = "Drop files here, or click to choose them.";

    /// <summary>The button's text when there is no drop zone, and the input's name when the field has no label.
    /// Defaults to "Choose files".</summary>
    public string Choose { get; init; } = "Choose files";

    /// <summary>Names a file's remove button, with <c>{0}</c> for the file's name. Defaults to "Remove {0}".</summary>
    public string Remove { get; init; } = "Remove {0}";

    /// <summary>Names the button that removes every file. Defaults to "Clear".</summary>
    public string Clear { get; init; } = "Clear";

    /// <summary>Said after a choice, with <c>{0}</c> for the number of files. Defaults to "{0} files chosen.".
    /// </summary>
    public string Chosen { get; init; } = "{0} files chosen.";

    /// <summary>Said after a choice of one file. Defaults to "1 file chosen.".</summary>
    public string ChosenOne { get; init; } = "1 file chosen.";

    /// <summary>The message for a file of a type the field doesn't take, with <c>{0}</c> for its name and <c>{1}</c>
    /// for the types it takes. Defaults to "{0} isn't one of {1}.".</summary>
    public string WrongType { get; init; } = "{0} isn't one of {1}.";

    /// <summary>The message for a file over the size limit, with <c>{0}</c> for its name and <c>{1}</c> for the
    /// limit. Defaults to "{0} is larger than {1}.".</summary>
    public string TooLarge { get; init; } = "{0} is larger than {1}.";

    /// <summary>The message when more files are chosen than allowed, with <c>{0}</c> for the most allowed. Defaults to
    /// "Choose at most {0} files.".</summary>
    public string TooMany { get; init; } = "Choose at most {0} files.";

    /// <summary>The message when more than one file is chosen where one is allowed. Defaults to "Choose only one
    /// file.".</summary>
    public string TooManyOne { get; init; } = "Choose only one file.";
}
