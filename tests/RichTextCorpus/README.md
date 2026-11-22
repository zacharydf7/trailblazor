# Rich text corpus

HTML as a rich text editor receives it on paste, each sample paired with the canonical value it must read as
(`<name>.expected.html`). The .NET unit tests check `RichTextDocument.Parse` against every sample. The browser tests
paste every sample into the editor and check that its engine reads it as `RichTextDocument.Parse` does with the
editor's schema, so the two readers agree.

| Sample | Source |
| --- | --- |
| `word-marks.html` | Copied from Word 16 on Windows: the six formats, a line break and accented text. |
| `word-lists.html` | Copied from Word 16 on Windows: a bulleted and a numbered list. |
| `web-page.html` | Copied from a web page in Chromium: a heading, styled bold, a link, a list, a table and a line break. |
| `word-structure.html` | Copied from Word 16 on Windows: two heading styles, a link and a nested numbered list. |
| `google-docs.html` | Hand-built from the HTML Google Docs puts on the clipboard. Replace it with a real copy. |
| `google-docs-structure.html` | Hand-built likewise: headings, a link, monospace text, nested and numbered lists, a rule. |
| `hostile.html` | Hand-built: scripts, event handlers, `javascript:` URLs and elements whose content never shows. |
| `loose.html` | Hand-built: markup a browser repairs, such as formatting left open across paragraphs and unclosed SVG. |
| `font-styles.html` | Hand-built: font shorthands and styles that are easy to misread, such as a shorthand resetting `font-family`, `var()` and oblique angles. |
| `images.html` | Hand-built: images in paragraphs, a list item, a quote, a link, bold text and preformatted text, with line breaks after them, alt text that has a line break or quotes, is empty or is missing, and sources that are relative, padded, missing, `http:` or `javascript:`. |
| `tables.html` | Hand-built: a caption, a header row, formats, links and line breaks in cells, paragraphs, lists, a rule, an image and a table inside cells, a spanning cell, an empty row, cells and rows left open, text between cells, a paragraph and a rule directly in a table with text before and after them, a heading after the cells, and a header cell outside the first row. |
| `mentions.html` | Hand-built: mention spans with tags, extra space and line breaks inside, one inside another, one in a link, one with no id and a trigger its text doesn't start with, an id and text that need escaping, the attribute on an element that isn't a span, two for the same item side by side, one in bold in a list, one with spaces at its edges and an empty one. Its expected value is read with the default schema, so its mentions are plain text; `RichTextEditorMentionTests` reads it with mentions kept. |

The Word samples keep everything Word wrote, except that the user folder in its file paths is renamed.
