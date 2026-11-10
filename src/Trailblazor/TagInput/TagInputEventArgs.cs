namespace Trailblazor;

/// <summary>Text a tag input's script asks to add as tags: what was typed before Enter, a separator or leaving the
/// field, or pasted text. Raised by <see cref="TbTagInput"/>'s input; consumers don't normally handle it.</summary>
public sealed class TagAddEventArgs : EventArgs
{
    /// <summary>The text to add, which the field splits at its separators and line breaks.</summary>
    public string Text { get; set; } = "";
}

/// <summary>A tag a tag input's script asks to remove by key. Raised by <see cref="TbTagInput"/>; consumers don't
/// normally handle it.</summary>
public sealed class TagRemoveEventArgs : EventArgs
{
    /// <summary>The tag's key from its remove button's <c>data-key</c>, which finds the tag even after an earlier
    /// removal has moved it, and a key whose tag is already gone removes nothing. Neither holds when another tag has
    /// the same text.</summary>
    public string Key { get; set; } = "";

    /// <summary>Where focus goes once the tag is gone: <c>"previous"</c>, <c>"next"</c> or <c>"input"</c>.</summary>
    public string Focus { get; set; } = "input";
}
