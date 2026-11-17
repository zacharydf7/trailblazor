namespace Trailblazor;

/// <summary>A field for writing markdown, with a toolbar and shortcuts that add its marks and a preview of how it will
/// look. Its value is the markdown text; <see cref="RichTextDocument.FromMarkdown(string?)"/> reads it into a
/// document, whose <see cref="RichTextDocument.ToHtml"/> is the HTML the preview shows.</summary>
public partial class TbMarkdownEditor;
