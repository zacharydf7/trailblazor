namespace Trailblazor;

/// <summary>What a <see cref="QueryField"/> holds, which picks the comparisons a <see cref="TbQueryBuilder"/> offers
/// for it and the editor its value is entered in.</summary>
public enum QueryFieldKind
{
    /// <summary>Text, compared ignoring case, with a text box for its value.</summary>
    Text,

    /// <summary>A number, with a number field for its value.</summary>
    Number,

    /// <summary>A date, or a date and time compared by its day, with a date picker for its value.</summary>
    Date,

    /// <summary>Yes or no, picked from a list.</summary>
    YesNo,

    /// <summary>One of the field's <see cref="QueryField.Choices"/>, such as an enum's values, picked from a list.
    /// </summary>
    Choice,
}
