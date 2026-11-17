namespace Trailblazor;

/// <summary>The words a <see cref="TbSmartPaste"/> shows and says. Each one left null uses the app's text.</summary>
public sealed record SmartPasteLabels
{
    /// <summary>The title of the box that asks for text when the clipboard can't be read. Leave it null for the app's
    /// text for <see cref="TbStrings.SmartPasteTitle"/>.</summary>
    public string? Title { get; init; }

    /// <summary>The label of the text area in that box. Leave it null for the app's text for
    /// <see cref="TbStrings.SmartPasteText"/>.</summary>
    public string? Text { get; init; }

    /// <summary>The box's button that fills the form. Leave it null for the app's text for
    /// <see cref="TbStrings.SmartPasteFill"/>.</summary>
    public string? Fill { get; init; }

    /// <summary>The box's button that closes it without filling. Leave it null for the app's text for
    /// <see cref="TbStrings.SmartPasteCancel"/>.</summary>
    public string? Cancel { get; init; }

    /// <summary>Shown and said while the model reads the text. Leave it null for the app's text for
    /// <see cref="TbStrings.SmartPasteFilling"/>.</summary>
    public string? Filling { get; init; }

    /// <summary>Shown and said once fields are filled; {0} is how many. Leave it null for the app's text for
    /// <see cref="TbStrings.SmartPasteFilled"/>.</summary>
    public string? Filled { get; init; }

    /// <summary>Shown and said once one field is filled, with <c>{0}</c>, if used, for the number. Leave it null for
    /// the app's text for <see cref="TbStrings.SmartPasteFilledOne"/>.</summary>
    public string? FilledOne { get; init; }

    /// <summary>Shown and said when the text held nothing to fill. Leave it null for the app's text for
    /// <see cref="TbStrings.SmartPasteNothing"/>.</summary>
    public string? Nothing { get; init; }

    /// <summary>Shown and said when the model failed. Leave it null for the app's text for
    /// <see cref="TbStrings.SmartPasteFailed"/>.</summary>
    public string? Failed { get; init; }
}
