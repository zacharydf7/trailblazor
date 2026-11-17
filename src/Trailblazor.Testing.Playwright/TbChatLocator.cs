using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbChat</c>, found by its <c>AriaLabel</c>, or the only one in the scope.</summary>
public sealed class TbChatLocator(ILocator scope, string? name)
{
    /// <summary>The chat's region.</summary>
    public ILocator Chat => Scope.ByRole(scope, AriaRole.Region, name).And(scope.Locator("section.tb-chat"));

    /// <summary>Each message, oldest first, as its author's name and its text, such as <c>"Robin: Meet at nine"</c>.
    /// </summary>
    public async Task<IReadOnlyList<string>> MessagesAsync()
    {
        await Chat.WaitForAsync();
        return await Chat.Locator(":scope > [data-part=messages] > [data-part=message]").EvaluateAllAsync<string[]>(
            "messages => messages.map(message => message.querySelector('[data-part=author]').textContent.trim() + ': '"
            + " + message.querySelector('[data-part=body]').textContent.trim())");
    }

    /// <summary>Who the chat says is typing, such as <c>"Robin is typing…"</c>, or null when no one is.</summary>
    public async Task<string?> TypingAsync()
    {
        await Chat.WaitForAsync();
        var typing = Chat.Locator(":scope > [data-part=typing]");
        return await typing.CountAsync() == 0 ? null : (await typing.TextContentAsync())?.Trim();
    }

    /// <summary>Types the text in the box and presses Enter, as a user does, and waits until the app's <c>OnSend</c>
    /// has run. A chat without its composer, or with <c>Disabled</c> set, throws.</summary>
    public async Task SendAsync(string text)
    {
        var input = await ComposerAsync();
        await input.FillAsync(text);
        await Commit.CountedAsync(Chat, "data-commits", async () =>
        {
            await input.PressAsync("Enter");
            return true;
        });
    }

    /// <summary>Chooses the files at these paths with the attach button, to go with the next message, and waits until
    /// the chat lists them. A chat without its composer or <c>AllowAttachments</c>, with <c>Disabled</c> set, or still
    /// waiting on <c>OnSend</c>, throws.</summary>
    public Task AttachAsync(params string[] paths) =>
        AttachAsync(input => input.SetInputFilesAsync(paths), paths.Select(Path.GetFileName));

    /// <summary>Chooses files made in the test, each with a <c>Name</c>, <c>MimeType</c> and <c>Buffer</c>, to go with
    /// the next message, and waits until the chat lists them. Throws as the paths overload does.</summary>
    public Task AttachAsync(params FilePayload[] files) =>
        AttachAsync(input => input.SetInputFilesAsync(files), files.Select(file => file.Name));

    private async Task AttachAsync(Func<ILocator, Task> choose, IEnumerable<string?> names)
    {
        await ComposerAsync();
        var input = Chat.Locator(":scope > [data-part=composer] [data-part=attach] input[type=file]");
        if (await input.CountAsync() == 0)
        {
            throw new InvalidOperationException("This chat doesn't take files; turn AllowAttachments on.");
        }

        if (await input.IsDisabledAsync())
        {
            throw new InvalidOperationException("This chat is still sending; attach once OnSend has run.");
        }

        await choose(input);
        foreach (var file in names)
        {
            await Assertions.Expect(Chat.Locator(":scope > [data-part=composer] [data-part=chosen]")
                .GetByText(file!, new() { Exact = true })).ToBeVisibleAsync();
        }
    }

    private async Task<ILocator> ComposerAsync()
    {
        await Chat.WaitForAsync();
        var input = Chat.Locator(":scope > [data-part=composer] [data-part=input]");
        if (await input.CountAsync() == 0)
        {
            throw new InvalidOperationException("This chat has no composer; turn ShowComposer on.");
        }

        if (await input.IsDisabledAsync())
        {
            throw new InvalidOperationException("This chat is disabled.");
        }

        // Enter sends only once the script has attached, which data-ready marks.
        await Assertions.Expect(Chat).ToHaveAttributeAsync("data-ready", "");
        return input;
    }
}
