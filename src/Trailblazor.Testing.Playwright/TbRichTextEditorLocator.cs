using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbRichTextEditor</c>, found by its label or a locator.</summary>
public sealed class TbRichTextEditorLocator(ILocator scope, ILocator input) : TbFieldLocator
{
    private protected override ILocator Control => Input;

    /// <summary>The editing area, for typing into and reading its text.</summary>
    public ILocator Input => input.And(scope.Locator("[contenteditable]"));

    /// <summary>Replaces the editor's text with this HTML, as pasting it over everything does, and leaves the editor,
    /// which commits it, then waits until the app has it. The editor keeps only the formats it allows, as with any
    /// paste.</summary>
    public async Task EnterAsync(string html)
    {
        // Until the script has attached, the editing area is plain markup that a paste would bypass.
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-ready", "");
        await Commit.WaitAsync(Input, () => PasteAsync(html));
    }

    /// <summary>Mentions an item: types this text at the end of the editor's text, chooses the suggestion with this
    /// name, and leaves the editor, which commits it, then waits until the app has it. The text must end with a
    /// trigger and the start of the mention, such as <c>"Thanks @ad"</c>.</summary>
    public Task MentionAsync(string typed, string suggestion) => ChooseAsync(typed, suggestion);

    /// <summary>Runs a slash command, one of the editor's own or of the app's: types this text at the end of the
    /// editor's text, chooses the command with this name, waits until it has run, and leaves the editor, which
    /// commits the text, then waits until the app has it. The text must end with a slash and the start of the
    /// command's name or of one of its keywords, such as <c>" /pack"</c>.</summary>
    public Task SlashCommandAsync(string typed, string command) => ChooseAsync(typed, command);

    private async Task ChooseAsync(string typed, string option)
    {
        // The list is there once the editor suggests mentions or has slash commands, and its script has attached.
        var list = scope.Page.Locator("[data-part=mention-list][data-ready]")
            .And(scope.Page.Locator($"[id='{await Input.GetAttributeAsync("aria-controls")}']"));
        await Assertions.Expect(list).ToBeAttachedAsync();
        await Input.EvaluateAsync("""
            editor => {
                editor.focus();
                // The caret goes at the end of the last paragraph's own content, since from a caret at the editor's
                // own end, after the last paragraph, Firefox types outside every paragraph. In a paragraph with
                // nothing but its line break, the caret goes before the break.
                let last = editor;
                while (last.lastChild && last.lastChild.nodeName !== 'BR') last = last.lastChild;
                const range = document.createRange();
                range.selectNodeContents(last);
                range.collapse(last.childNodes.length === 1 && last.lastChild.nodeName === 'BR');
                getSelection().removeAllRanges();
                getSelection().addRange(range);
            }
            """);
        await Input.PressSequentiallyAsync(typed);
        // The list may still show the suggestions for less of the text, and a click on one of those is ignored.
        await Assertions.Expect(list).Not.ToHaveAttributeAsync("aria-busy", "true");
        await list.GetByRole(AriaRole.Option, new() { Name = option, Exact = true }).ClickAsync();
        await Assertions.Expect(list).ToBeHiddenAsync();
        // One of the app's commands puts its HTML in and calls back only once .NET has heard of the choice.
        await Assertions.Expect(Input).Not.ToHaveAttributeAsync("data-running-command", "");
        await Input.BlurAsync();
        // An editor that updates as it is typed in sends its value several times on the way, so one commit isn't
        // the end. The editor counts the values it sent, and .NET the ones the app has finished with.
        await Assertions.Expect(Input).ToHaveAttributeAsync("data-sends-answered",
            await Input.GetAttributeAsync("data-sends") ?? "0");
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
