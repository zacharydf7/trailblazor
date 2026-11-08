# Rich text corpus

HTML as a rich text editor receives it on paste, each sample paired with the canonical value it must read as
(`<name>.expected.html`). The .NET unit tests check `RichTextDocument.Parse` against every sample. The editor
engine's browser tests (#354) will check the browser side against the same files, so the two readers agree.

| Sample | Source |
| --- | --- |
| `word-marks.html` | Copied from Word 16 on Windows: the six formats, a line break and accented text. |
| `word-lists.html` | Copied from Word 16 on Windows: a bulleted and a numbered list. |
| `web-page.html` | Copied from a web page in Chromium: a heading, styled bold, a link, a list, a table and a line break. |
| `google-docs.html` | Hand-built from the HTML Google Docs puts on the clipboard. Replace it with a real copy. |
| `hostile.html` | Hand-built: scripts, event handlers, `javascript:` URLs and elements whose content never shows. |
| `loose.html` | Hand-built: markup a browser repairs, such as formatting left open across paragraphs and unclosed SVG. |

The Word samples keep everything Word wrote, except that the user folder in its file paths is renamed.
