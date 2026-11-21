namespace Trailblazor;

/// <summary>What a <see cref="TbSmartPaste"/> asks the app's model: the text pasted, and the fields to fill from it.
/// </summary>
/// <param name="Text">The pasted text, such as an email signature.</param>
/// <param name="Fields">The form's fields smart paste can fill, each with a label and neither disabled nor read-only:
/// text, number, date, time and yes-or-no fields, and fields that pick from options, such as a select or a radio group,
/// which come with their <see cref="SmartPasteField.Choices"/>. Every other field is left out, such as a password
/// field, a slider, or a field with no option to offer.</param>
public sealed record SmartPasteRequest(string Text, IReadOnlyList<SmartPasteField> Fields);
