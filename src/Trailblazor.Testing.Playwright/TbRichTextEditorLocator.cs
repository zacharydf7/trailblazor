using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbRichTextEditor</c>, found by its label.</summary>
public sealed class TbRichTextEditorLocator(ILocator scope, string label)
{
    /// <summary>The editing area, for typing into and reading its text.</summary>
    public ILocator Input => scope.GetByLabel(FieldLabel.Matching(label)).And(scope.Locator("[contenteditable]"));

    /// <summary>Replaces the editor's text with this HTML, as pasting it over everything does, and leaves the editor,
    /// which commits it, then waits until the app has it. The editor keeps only the formats it allows, as with any
    /// paste.</summary>
    public async Task EnterAsync(string html)
    {
        // Until the script has attached, the editing area is plain markup that a paste would bypass.
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(Input, () => PasteAsync(html));
    }

    // Says whether the paste changed the text, since leaving an editor whose text is the same commits nothing.
    private async Task<bool> PasteAsync(string html)
    {
        var before = await Input.InnerHTMLAsync();
        await Input.FocusAsync();
        await Input.PressAsync("ControlOrMeta+a");
        // Playwright can't write to the system clipboard in every browser, so the paste event carries its own data.
        await Input.EvaluateAsync("""
            (editor, html) => {
                const data = new DataTransfer();
                data.setData('text/html', html);
                data.setData('text/plain', new DOMParser().parseFromString(html, 'text/html').body.textContent);
                const event = new ClipboardEvent('paste', { bubbles: true, cancelable: true });
                Object.defineProperty(event, 'clipboardData', { value: data });
                editor.dispatchEvent(event);
            }
            """, html);
        // A paste the app transforms goes in once the app answers.
        await Assertions.Expect(Input).Not.ToHaveAttributeAsync("data-pasting", "");
        // A paste with nothing to keep, such as "", leaves the selection, so Delete removes the old text. After any
        // other paste the caret is at the end, where Delete does nothing.
        await Input.PressAsync("Delete");

        var changes = await Input.InnerHTMLAsync() != before;
        await Input.BlurAsync();
        return changes;
    }
}
