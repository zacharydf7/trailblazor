using Microsoft.Playwright;

namespace Trailblazor.Testing.Playwright;

/// <summary>A <c>TbAiPrompt</c>, found by its <c>Label</c>, or the only one in the scope. Responses are newest first, so
/// index 0 is the latest.</summary>
public sealed class TbAiPromptLocator(ILocator scope, string? label)
{
    /// <summary>The AI prompt's region.</summary>
    public ILocator AiPrompt => Scope.ByRole(scope, AriaRole.Region, label).And(scope.Locator("section.tb-ai-prompt"));

    private ILocator Outputs => AiPrompt.Locator(":scope > [data-part=outputs] > [data-part=output]");

    /// <summary>Each response's text, newest first.</summary>
    public async Task<IReadOnlyList<string>> ResponsesAsync()
    {
        await AiPrompt.WaitForAsync();
        return await Outputs.EvaluateAllAsync<string[]>(
            "outputs => outputs.map(output => output.querySelector('[data-part=text]').textContent.trim())");
    }

    /// <summary>Each response's status, newest first.</summary>
    public async Task<IReadOnlyList<AiPromptStatus>> StatusesAsync()
    {
        await AiPrompt.WaitForAsync();
        var statuses = await Outputs.EvaluateAllAsync<string[]>("outputs => outputs.map(output => output.dataset.status)");
        return [.. statuses.Select(status => Enum.Parse<AiPromptStatus>(status, ignoreCase: true))];
    }

    /// <summary>Types the prompt in the box and presses Enter, as a user does, and waits until the response is done,
    /// stopped or failed. A disabled prompt, or one still generating, throws.</summary>
    public async Task PromptAsync(string prompt)
    {
        var input = await ComposerAsync();
        await input.FillAsync(prompt);
        await Commit.CountedAsync(AiPrompt, "data-commits", async () =>
        {
            await input.PressAsync("Enter");
            return true;
        });
    }

    /// <summary>Clicks the suggestion with this text, and waits until it's in the box. A prompt without it throws.
    /// </summary>
    public async Task ChooseSuggestionAsync(string suggestion)
    {
        var input = await ComposerAsync();
        var button = AiPrompt.Locator(":scope > [data-part=composer] [data-part=suggestion]")
            .GetByText(suggestion, new() { Exact = true });
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException($"This AI prompt has no suggestion \"{suggestion}\".");
        }

        await button.ClickAsync();
        await Assertions.Expect(input).ToHaveValueAsync(suggestion);
    }

    /// <summary>Presses Escape to stop the response that's generating, and waits until it has stopped. A prompt with
    /// nothing generating throws.</summary>
    public async Task StopAsync()
    {
        await AiPrompt.WaitForAsync();
        if (await AiPrompt.GetAttributeAsync("data-generating") is null)
        {
            throw new InvalidOperationException("Nothing is generating in this AI prompt.");
        }

        await Assertions.Expect(AiPrompt).ToHaveAttributeAsync("data-ready", "");
        await AiPrompt.Locator(":scope > [data-part=composer] [data-part=generate]").PressAsync("Escape");
        await Assertions.Expect(AiPrompt).Not.ToHaveAttributeAsync("data-generating", "");
    }

    /// <summary>Copies a response's text. A response with no text yet throws.</summary>
    public async Task CopyAsync(int index = 0) => await Commit.ClickedAsync(await ButtonAsync(index, "copy", "Copy"));

    /// <summary>Generates a response again for its prompt, and waits until it's done, stopped or failed. A prompt
    /// that's disabled or still generating throws.</summary>
    public async Task RetryAsync(int index = 0)
    {
        var retry = await ButtonAsync(index, "retry", "Retry");
        await Commit.CountedAsync(AiPrompt, "data-commits", async () =>
        {
            await retry.ClickAsync();
            return true;
        });
    }

    /// <summary>Inserts a response, which raises <c>OnInsert</c>. A prompt without <c>OnInsert</c> or that's disabled,
    /// or a response with no text yet, throws.</summary>
    public async Task InsertAsync(int index = 0) =>
        await Commit.ClickedAsync(await ButtonAsync(index, "insert", "Insert"));

    private async Task<ILocator> ComposerAsync()
    {
        await AiPrompt.WaitForAsync();
        var input = AiPrompt.Locator(":scope > [data-part=composer] [data-part=input]");
        if (await input.IsDisabledAsync())
        {
            throw new InvalidOperationException("This AI prompt is disabled.");
        }

        if (await AiPrompt.GetAttributeAsync("data-generating") is not null)
        {
            throw new InvalidOperationException("This AI prompt is still generating; stop it or let it finish.");
        }

        // Enter generates only once the script has attached, which data-ready marks.
        await Assertions.Expect(AiPrompt).ToHaveAttributeAsync("data-ready", "");
        return input;
    }

    private async Task<ILocator> ButtonAsync(int index, string part, string name)
    {
        await AiPrompt.WaitForAsync();
        var count = await Outputs.CountAsync();
        if (index < 0 || index >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index,
                $"There's no response at index {index}; this AI prompt has {count}.");
        }

        var button = Outputs.Nth(index).Locator($"[data-part={part}]");
        if (await button.CountAsync() == 0)
        {
            throw new InvalidOperationException("This AI prompt has no Insert; set OnInsert.");
        }

        if (await button.IsDisabledAsync())
        {
            throw new InvalidOperationException(
                $"{name} is off for this response: it has no text yet, or the prompt is disabled or generating.");
        }

        return button;
    }
}
