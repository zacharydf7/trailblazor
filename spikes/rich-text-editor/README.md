# Spike D: a rich text editor on Trailblazor's own engine

**Question.** Can an editor built on the browser's own editing (`contenteditable`), with Trailblazor's own document
model, edit rich text as reliably as a mature editor engine? If it can, `TbRichTextEditor` (#161) needs no
third-party code, which keeps the library free of dependencies and of any license terms beyond its own.

The spike covers the risky half: editing in the browser. The .NET half (the value model, sanitizing and binding) is
ordinary code and is designed in the recommendation below rather than prototyped.

## What gets built

- `editor.js`, a throwaway engine:
  - A document model of paragraphs, each a list of text runs carrying a set of marks: bold, italic, underline,
    strikethrough, subscript and superscript, plus line breaks.
  - A renderer from the model to the DOM, and a reader from the DOM back to the model.
  - Mapping between model positions and DOM selection points.
  - An undo history of transactions.
- Edit handling:
  - The browser performs plain typing, composition (IME), spellcheck and autocorrect itself, and the engine reads
    the result back from the DOM.
  - The engine performs everything else on the model and renders it: splitting and joining paragraphs, line breaks,
    deletions that span paragraphs, formatting, undo and redo, and paste. It cancels the browser's own handling,
    mostly through `beforeinput`. Drops are refused.
  - `document.execCommand` is never used.
- A spike component with a toolbar of toggle buttons, an HTML string value bound to .NET, and a page that shows
  the bound value and can set it from .NET.

## Pass criteria

1. **Typing.** Text typed at the start, middle and end of a paragraph, inside a marked run and at a mark boundary
   lands in the model exactly as it appears on screen, and the value is the canonical HTML for the model.
2. **Marks.**
   - Each of the six marks applies to and removes from a selected range, including a range that is only partly
     marked, and the toolbar shows a partly marked range as mixed.
   - With a collapsed selection, turning a mark on and then typing makes the new text carry the mark.
   - Subscript and superscript exclude each other.
   - Ctrl+B, Ctrl+I and Ctrl+U (Cmd on macOS) toggle their marks.
   - The toolbar's pressed states follow the selection as it moves.
3. **Paragraphs.**
   - Enter splits a paragraph, and Shift+Enter inserts a line break.
   - Backspace at the start of a paragraph and Delete at its end join it to its neighbor.
   - Deleting a range that spans paragraphs merges them.
   - Selecting everything and deleting it leaves one empty paragraph with the caret in it, and an empty value
     (`""`).
4. **Undo and redo.** Ctrl+Z, Ctrl+Y or Ctrl+Shift+Z, and the browser's own undo and redo input, undo and redo
   typing in word-sized steps, mark changes, paste and paragraph splits. Each step puts the selection back where it
   was.
5. **Composition.** In Chromium, a composition driven through the DevTools Protocol commits the right text to the
   model, and the engine leaves the composing text alone until the composition ends. A composition inside a bold
   run stays bold. Firefox and WebKit offer no way to drive a real input method from Playwright, so they get a
   manual check with the Windows Japanese input method, recorded with the results.
6. **Changes the engine didn't make.** A change the browser makes to the text directly, without a cancelable
   `beforeinput`, as spellcheck and autocorrect do, is read back into the model.
7. **Paste.**
   - HTML from Word, Google Docs and an ordinary web page keeps its marks:
     - bold from `<b>`, `<strong>` or a font weight of 600 or more, but not Google Docs' `<b>` wrapper with
       `font-weight: normal`
     - italic, underline and strikethrough from their tags or styles
     - subscript and superscript from `<sub>`, `<sup>` or `vertical-align`
   - Headings, list items and other blocks become paragraphs, and anything else becomes plain text.
   - No `<script>`, event-handler attribute or `javascript:` URL reaches the editor: a canary global they would set
     stays unset.
   - Ctrl+Shift+V, and a clipboard with only plain text, paste plain text.
8. **Binding.**
   - In Interactive Server with 200 ms of latency each way, typing 50 characters at full speed loses none of them
     and never moves the caret.
   - Setting the value from .NET renders it, and the editor's own value coming back from .NET leaves the DOM and
     the caret alone.
   - The value reaches .NET within one debounce interval after typing stops, and when the editor loses focus.
9. **Accessibility.**
   - The editable element has the `textbox` role, `aria-multiline="true"` and an accessible name from its label.
   - The toolbar has the `toolbar` role and a label, and its buttons report their state with `aria-pressed`.
   - Marks render as `<strong>`, `<em>`, `<u>`, `<s>`, `<sub>` and `<sup>`, which screen readers can report.
10. **Size.** The engine is under 12 KB minified and gzipped. ProseMirror's core packages are measured the same way
    for comparison.
11. **No errors.** No test logs a script error or a server error, in either render mode.

## How it is measured

Each criterion is a Playwright test in `spikes/Trailblazor.Spikes.Tests`, run on Chromium, Firefox and WebKit in
both render modes, except criterion 5's driven composition (Chromium only) and criterion 8's latency (Interactive
Server only, through the tests' latency proxy). Criterion 10 is measured with a minifier and gzip.

## If a criterion fails

- If criteria 1 to 7 fail in a way targeted fixes can't solve, the fallback is to wrap ProseMirror. It is
  MIT-licensed and written by one author, with a few small MIT-licensed packages beneath it. It would be bundled
  into Trailblazor's script with its license notice, so the library would stay free of copyleft terms.
- If criterion 8 fails, the engine keeps the value in the browser and sends it to .NET only on blur and on request.
- If criterion 10 fails, the budget is raised to the measured size and the reason recorded.

## Results

**Verdict: the engine meets every criterion the tests can check.** Criterion 5's manual check in Firefox and WebKit
is still to do, because it needs a person at the keyboard with a real input method.

Measured on 2026-11-08 on Windows 11, .NET SDK 10.0.403 and Playwright 1.64.1 with its bundled engines, headless.
The tests are in `spikes/Trailblazor.Spikes.Tests/Editor`. The last full run, on the code as it stands, passed all
161 of them: every criterion in three engines and both render modes, except where a criterion says otherwise.

| # | Criterion | Result |
| --- | --- | --- |
| 1 | Typing | Pass |
| 2 | Marks | Pass |
| 3 | Paragraphs | Pass, after one fix (see below) |
| 4 | Undo and redo | Pass. The browser's own undo and redo input is simulated with a dispatched `beforeinput`. |
| 5 | Composition | Pass in Chromium. Manual check in Firefox and WebKit still to do. |
| 6 | Changes the engine didn't make | Pass, after one fix (see below) |
| 7 | Paste | Pass, on hand-built samples (see below). Ctrl+Shift+V is simulated with dispatched events. |
| 8 | Binding | Pass, after one fix (see below). Timings below. |
| 9 | Accessibility | Pass |
| 10 | Size | Pass: 5.4 KB minified and gzipped (15.4 KB minified) |
| 11 | No errors | Pass |

**What the first run caught.** It failed 8 of 143 tests, for two reasons:
- **WebKit reports Shift+Enter as a new paragraph** (`insertParagraph`, not `insertLineBreak`). The engine now
  handles Enter and Shift+Enter in `keydown` and keeps `beforeinput` for soft keyboards, which send no Enter key.
- **The engine misplaced a change away from the caret.** Reading back a spellcheck-style rewrite, the engine took
  the caret's position as the end of the change, so a bold word rewritten away from the caret lost its bold. The
  fix finds the change first and uses the caret only to break ties when text repeats ("a" typed between two "a"s
  with different marks). A new test covers that tie.

**What the code review caught.**
- **A value set while the engine loaded was lost.** A parent that loads its data asynchronously would see the old
  value in the editor, and the first keystroke would send that back over the loaded one. The component now sends
  the value again once the engine attaches, and a test holds the engine's script back while .NET sets the value.
- **Word's list bullets and numbers pasted as text.** Word writes them in spans styled `mso-list: Ignore`, which the
  engine now skips. The Word sample gained a list.
- **An Enter that commits a composition in Safari would split the paragraph.** Safari ends the composition before
  that Enter's `keydown`, which reports only key code 229. The engine now ignores key code 229. Safari on macOS
  isn't in the test matrix, so this joins the manual checks.

**Binding timings.** The latency test types 50 characters in bursts of 10 at full speed, pausing longer than the
send delay so values are in flight while typing goes on. A separate timing test, in both render modes, measured the
value reaching .NET 310 to 326 ms after typing stopped, against a 300 ms send delay, and 2 to 6 ms after a blur, or
22 ms in WebKit with Interactive Server.

**Paste samples.** The Word, Google Docs and web page samples are hand-built from the HTML those sources put on the
clipboard, including Word's list markup and Google Docs' `<b>` wrapper. Stage 1 replaces them with live clipboard
captures.

**Mutation checks.** Each of these breaks made its test fail in every engine and mode it runs in:
- Dropping the caret tie-break from the read-back diff.
- Treating Google Docs' `font-weight: normal` wrapper as bold.
- Ignoring a mark turned on at a collapsed selection when the next text is typed.
- Sending the editor's own value back to it from .NET, which loses characters typed over a slow server.
- Not sending a value that .NET set while the engine loaded.
- Keeping Word's list markers.
- Not sending the value on blur.

**Size.** Measured with esbuild's minifier and `gzip -9`:
- This engine: 15.4 KB minified, 5.4 KB gzipped.
- ProseMirror's core (`prosemirror-model`, `-state`, `-view` and `-transform`): 190 KB minified, 58 KB gzipped.
  With `-history`, `-keymap` and `-commands`, the minimum for a usable editor, it is 66 KB gzipped.
- ProseMirror does much more than this engine, so the comparison only shows what wrapping it would cost, not what
  Trailblazor's finished engine will weigh. Each stage below adds to the engine. The budget for stage 1 is 15 KB
  gzipped.

**Gaps the spike leaves for stage 1.**
- **Several text nodes changed at once.** The read-back diffs a whole paragraph, so two words changed in one task
  merge into one change and take one set of marks. Spellcheck changes one word at a time, but browser extensions
  may not. The real engine diffs each changed text node, which the mutation records already name.
- **A composition that starts over a selection spanning paragraphs.** The browser deletes it natively, and the
  engine only reads the result back.
- **Drag and drop** is refused. Stage 1 moves dragged text through the model.
- **Undo history** keeps a copy of the document per step. Long documents need inverse steps instead.
- **Devices and assistive technology.** Android and iOS keyboards, macOS and Windows input methods (including an
  Enter that commits a composition in Safari), and NVDA, JAWS and VoiceOver need real-device checks, tracked with
  the other device checks in #332.

## Recommendation

**Build Trailblazor's own engine, with ProseMirror as the fallback.**
- **No dependency, and no license terms beyond Trailblazor's own.** That keeps every licensing choice open,
  including closed source.
- **Small.** The spike's engine is a tenth of ProseMirror's core gzipped, and stage 1's 15 KB budget is still about
  a quarter of it.
- **The .NET side needs the document model anyway.** Server-side sanitizing, a structured value for apps, and bUnit
  tests that run without a browser all need it. Wrapping a JavaScript engine would mean keeping two schemas in step.
- **The design leaves the hardest input to the browser.** Typing, composition, spellcheck and autocorrect happen
  natively and are read back from the DOM, as ProseMirror and Lexical handle them, so the engine stays out of an
  input method's way. The risk left is the long tail of device behavior, like Safari's Enter above, which only real
  devices can retire.
- **The fallback stays open.** ProseMirror is MIT-licensed. If a stage meets an editing problem the engine can't
  solve, Trailblazor can wrap it, bundled with its license notice.

**Engines considered.** Licenses are from npm on 2026-11-08.

| Engine | License | Why not |
| --- | --- | --- |
| TinyMCE 7 and later | GPL 2.0 or later, or commercial | Copyleft unless a license is bought. |
| CKEditor 5 | GPL 2.0 or later, or commercial | Copyleft unless a license is bought. |
| ProseMirror | MIT | The best engineered, and the fallback. It costs 58 to 66 KB gzipped and a second schema in JavaScript. |
| Tiptap | MIT core, paid extensions | ProseMirror plus a vendor layer, whose richer extensions are paid. |
| Lexical | MIT | Still 0.x (0.53), with breaking changes between minor versions. |
| Quill 2 | BSD 3-clause | Its own Delta model and opinionated DOM, harder to fit to a Trailblazor schema. |
| Squire | MIT | Small, but edits HTML in the DOM with no document model. |
| HugeRTE | MIT | A community fork of TinyMCE 6, and the largest package here (8 MB unpacked). |
| Trix | MIT | An opinionated feature set and document format of its own. |

Every engine with an MIT or BSD license would also allow closed source, with its license notice kept. Between those,
the choice comes down to dependencies, size and the .NET model, not the license.

**The value.**
- `Value` is an HTML string in one documented, canonical form:
  - `<p>` paragraphs and `<br>` line breaks
  - only the tags of the formats the editor allows
  - `&nbsp;` for each space HTML would otherwise collapse
  - an empty editor is `""`
- A .NET `RichTextDocument` holds the same content as paragraphs of runs with marks, with `Parse(html)`, `ToHtml()`
  and `ToPlainText()`.
  - Parsing sanitizes by construction: anything outside the schema becomes text.
  - The component parses every value that comes from the browser, so the server never trusts the browser's HTML.
  - Apps can use it to read stored HTML, or to export to Word, PDF or plain text without parsing HTML themselves.
- The engine and `RichTextDocument` share one test corpus of Word, Google Docs, web and hostile HTML, so the two
  parsers stay in agreement.
- Binding follows `TbTextField`: the value goes to .NET after a pause in typing (`UpdateDelay`) and when focus
  leaves.

**The API, at stage 1.** `TbRichTextEditor` is a field on `TbBoxedFieldBase<string>`, so `Label`, `HelperText`,
`Disabled`, `ReadOnly`, `Required`, `Validation`, `Variant`, `Size` and `TelemetryName` work as on every other field.
Its own parameters:
- `Formats`: the formats allowed (flags, default all six marks). It sets the schema, so a disallowed format can't be
  pasted in either, and the toolbar shows a button for each format allowed.
- `ShowToolbar`: false for an editor driven only by shortcuts, or for a plain multi-line field with
  `Formats="RichTextFormats.None"`.
- `Placeholder`, `Lines` and `MaxLines`, named as on `TbTextField`.
- `Labels`: localized tool names and shortcut hints, like the data grid's labels.

The toolbar is a `TbToolbar` of toggle buttons. The mixed pressed state from #251 shows a partly formatted selection.

**API improvements the first consumer app points to.** It works around each of these in its current editor today,
and any app would want them:
- It scans the saved HTML for strings like `font-weight: bold` to rebuild runs. `RichTextDocument` gives runs
  directly.
- It turns the editor's stray `<br>` into `""`. The canonical value is `""` when empty.
- It cuts Word's fragment out of pasted HTML with a regular expression and converts the blocks itself. Paste
  cleanup is built in, and a paste hook (stage 2) is for apps that want more.
- It hides the toolbar with an empty element. `ShowToolbar` says so directly.

**Stages.** Each stage is one or more issues, and each ends with the docs page, interactive examples, API tables,
accessibility, telemetry and testing notes for what it adds.
1. **Core and marks**, what the first consumer app needs. This stage is two issues:
   - `RichTextDocument`, with parsing, sanitizing, serializing and the shared corpus.
   - `TbRichTextEditor`, with:
     - the engine and the six marks, paragraphs and line breaks
     - undo and redo, paste cleanup and plain-text paste, and drag and drop
     - the toolbar, shortcuts, `Formats` and `ShowToolbar`
     - read-only and disabled states, validation, placeholder and sizing
     - telemetry and Trailblazor.Testing support
2. **Structure.** Headings, bulleted and numbered lists with indent and outdent, block quotes, links (a link dialog
   and an allowlist of URL schemes), clear formatting, inline code, a paste hook, and Markdown-style shortcuts such
   as `# ` and `- `.
3. **Media and tables.** Images (an upload callback and required alt text), tables, an HTML source view, and a
   character count with a maximum length.
4. **Extensibility.** Custom marks and blocks, mentions and slash commands, a floating selection toolbar, text color
   and highlight, and find and replace.

Stages 2 to 4 bring the editor up to the feature set of the mature editors in the table above. They can be
reordered once stage 1 ships.

**Testing.**
- **Unit tests:** `RichTextDocument` parsing, serializing and sanitizing, against the shared corpus.
- **bUnit:** everything the .NET side owns: rendering, labels, the toolbar per `Formats`, validation, read-only and
  disabled states, and the value's path from the engine to `ValueChanged`.
- **Playwright:** the engine, in all three browsers, starting from this spike's tests, and only for what bUnit can't
  reach.
- **Trailblazor.Testing:** bUnit can't run the engine, so the package offers a way to enter a value as if it had been
  typed, through the same path the browser's value takes (parsing, sanitizing, `ValueChanged`, validation). In
  Playwright, the editor is a labeled textbox, and the spike's tests show that filling it by its label works in
  every engine.

**For approval.**
1. The engine: Trailblazor's own, with ProseMirror as the fallback.
2. The stages above, with stage 1 as the next work.
