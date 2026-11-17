using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbMarkdownEditor</c>, found by its label or a locator.</summary>
public sealed class TbMarkdownEditorLocator(ILocator scope, ILocator input) : TbFieldLocator
{
    private protected override ILocator Control => Input;

    /// <summary>The text area, for typing into and reading its markdown.</summary>
    public ILocator Input => input.And(scope.Locator("textarea.tb-markdown-editor__input"));

    private ILocator Editor => Input.Locator("xpath=ancestor::*[@data-part='editor'][1]");

    /// <summary>The preview, once it shows.</summary>
    public ILocator Preview => Editor.Locator("[data-part=preview]:not([hidden])");

    /// <summary>The markdown the editor holds.</summary>
    public Task<string> TextAsync() => Input.InputValueAsync();

    /// <summary>The HTML the preview shows, or null while it isn't shown.</summary>
    public async Task<string?> PreviewHtmlAsync() => await Preview.CountAsync() == 0 ? null
        // Blazor marks where it renders with comments, which aren't part of the preview.
        : await Preview.EvaluateAsync<string>(@"preview => preview.innerHTML.replace(/<!--[\s\S]*?-->/g, '')");

    /// <summary>Replaces the editor's markdown with this text and leaves the editor, then waits until the app has it.
    /// A read-only or disabled editor throws.</summary>
    public async Task EnterAsync(string text)
    {
        if (!await Input.IsEditableAsync())
        {
            throw new InvalidOperationException(
                "This editor is read-only or disabled, so text can't be typed into it.");
        }

        await ReadyAsync();
        await Commit.WaitAsync(Input, async () =>
        {
            var before = await Input.InputValueAsync();
            await Input.FillAsync(text);
            await Input.BlurAsync();
            return text != before;
        });
    }

    /// <summary>Presses the toolbar button with this name, such as <c>"Bold"</c>, which marks up the selected text,
    /// and waits until the app has the new markdown. Select the text first, for example with
    /// <c>Input.SelectTextAsync()</c>.</summary>
    public async Task PressAsync(string button)
    {
        await ReadyAsync();
        var tool = Editor.Locator("[data-part=toolbar]")
            .GetByRole(AriaRole.Button, new() { Name = button, Exact = true });
        await Commit.WaitAsync(Input, async () =>
        {
            var before = await Input.InputValueAsync();
            await tool.ClickAsync();
            return await Input.InputValueAsync() != before;
        });
    }

    /// <summary>Shows the preview with the Preview button, unless it shows already, and waits until it does. An editor
    /// whose preview isn't <c>MarkdownPreview.Toggle</c> throws, since it has no such button.</summary>
    public Task ShowPreviewAsync() => PreviewAsync(true);

    /// <summary>Hides the preview with the Preview button, unless it's hidden already, and waits until the text shows
    /// again. It throws as <see cref="ShowPreviewAsync"/> does.</summary>
    public Task HidePreviewAsync() => PreviewAsync(false);

    private async Task PreviewAsync(bool shown)
    {
        var toggle = Editor.Locator("[data-part=preview-toggle]");
        if (await toggle.CountAsync() == 0)
        {
            throw new InvalidOperationException("This editor has no Preview button; its Preview is Beside or None.");
        }

        if (await toggle.GetAttributeAsync("aria-pressed") != (shown ? "true" : "false"))
        {
            await Commit.ClickedAsync(toggle);
        }

        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", shown ? "true" : "false");
    }

    // The toolbar works once the editor's script has attached.
    private Task ReadyAsync() => Assertions.Expect(Editor).ToHaveAttributeAsync("data-ready", "");
}
