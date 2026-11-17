namespace Trailblazor;

/// <summary>What a <see cref="TbSmartPaste"/> asks the app's model: the text pasted, and the fields to fill from it.
/// </summary>
/// <param name="Text">The pasted text, such as an email signature.</param>
/// <param name="Fields">The form's fields smart paste can fill: text, number, date, time and yes-or-no fields with a
/// label, neither disabled nor read-only. Password fields and fields that pick from options, such as a select, are left
/// out.</param>
public sealed record SmartPasteRequest(string Text, IReadOnlyList<SmartPasteField> Fields);
