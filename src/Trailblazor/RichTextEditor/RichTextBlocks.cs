namespace Trailblazor;

/// <summary>The kinds of block rich text can have besides plain paragraphs. Combine them with <c>|</c> to say which a
/// document may keep; a block that isn't allowed gives way to an allowed block around it or a plain paragraph.
/// </summary>
[Flags]
public enum RichTextBlocks
{
    /// <summary>Plain paragraphs only.</summary>
    None = 0,

    /// <summary>Headings, at three levels.</summary>
    Headings = 1,

    /// <summary>Block quotes.</summary>
    Quotes = 2,

    /// <summary>Bulleted lists.</summary>
    BulletedLists = 4,

    /// <summary>Numbered lists.</summary>
    NumberedLists = 8,

    /// <summary>Horizontal rules between paragraphs. Without this, rules are dropped.</summary>
    Rules = 16,
}
