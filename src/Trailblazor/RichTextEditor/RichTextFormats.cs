namespace Trailblazor;

/// <summary>The formats rich text can carry. Combine them with <c>|</c> to say which formats a document may keep.
/// </summary>
[Flags]
public enum RichTextFormats
{
    /// <summary>Plain text.</summary>
    None = 0,

    /// <summary>Bold, written as <c>&lt;strong&gt;</c>.</summary>
    Bold = 1,

    /// <summary>Italic, written as <c>&lt;em&gt;</c>.</summary>
    Italic = 2,

    /// <summary>Underline, written as <c>&lt;u&gt;</c>.</summary>
    Underline = 4,

    /// <summary>Strikethrough, written as <c>&lt;s&gt;</c>.</summary>
    Strikethrough = 8,

    /// <summary>Subscript, written as <c>&lt;sub&gt;</c>. Text can't be both subscript and superscript.</summary>
    Subscript = 16,

    /// <summary>Superscript, written as <c>&lt;sup&gt;</c>. Text can't be both subscript and superscript.</summary>
    Superscript = 32,
}
