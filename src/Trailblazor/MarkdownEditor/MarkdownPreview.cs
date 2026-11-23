namespace Trailblazor;

/// <summary>Where a <see cref="TbMarkdownEditor"/> shows its markdown as it will look.</summary>
public enum MarkdownPreview
{
    /// <summary>A Preview button in the toolbar shows the preview in place of the text, and hides it again.</summary>
    Toggle,

    /// <summary>
    /// The preview shows beside the text as it is typed when the editor is at least 40rem wide, and under it when it is
    /// narrower. Where nothing stretches the editor, give it a width.
    /// </summary>
    Beside,

    /// <summary>No preview.</summary>
    None,
}
